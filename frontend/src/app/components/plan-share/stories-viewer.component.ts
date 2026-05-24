import {
    ChangeDetectionStrategy,
    Component,
    DestroyRef,
    ElementRef,
    HostListener,
    Renderer2,
    computed,
    effect,
    inject,
    signal,
    viewChild,
} from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { TranslocoModule } from '@jsverse/transloco';

import {
    HOLD_TO_PAUSE_THRESHOLD_MS,
    PlanData,
    PlanSlide,
    SLIDE_DURATION_MS,
} from './plan-share.model';
import { PlanShareService } from './plan-share.service';
import { StoryProgressComponent } from './story-progress.component';
import { ActivityVibeSlideComponent } from './slides/activity-vibe-slide.component';
import { FoodSlideComponent } from './slides/food-slide.component';
import { MusicSlideComponent } from './slides/music-slide.component';
import { ShareSlideComponent } from './slides/share-slide.component';
import { SleepSlideComponent } from './slides/sleep-slide.component';
import { TransportSlideComponent } from './slides/transport-slide.component';
import { WelcomeSlideComponent } from './slides/welcome-slide.component';

type LoadState = 'loading' | 'ready' | 'not-found';

@Component({
    selector: 'app-stories-viewer',
    imports: [
        TranslocoModule,
        StoryProgressComponent,
        WelcomeSlideComponent,
        TransportSlideComponent,
        SleepSlideComponent,
        MusicSlideComponent,
        FoodSlideComponent,
        ActivityVibeSlideComponent,
        ShareSlideComponent,
    ],
    templateUrl: './stories-viewer.component.html',
    styleUrl: './stories-viewer.component.css',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StoriesViewerComponent {
    private readonly route = inject(ActivatedRoute);
    private readonly router = inject(Router);
    private readonly planShareService = inject(PlanShareService);
    private readonly renderer = inject(Renderer2);
    private readonly destroyRef = inject(DestroyRef);

    private readonly uuid = toSignal(this.route.paramMap, { initialValue: null });

    protected readonly state = signal<LoadState>('loading');
    protected readonly plan = signal<PlanData | null>(null);
    protected readonly currentIndex = signal(0);
    protected readonly paused = signal(false);
    protected readonly progressFraction = signal(0);

    protected readonly stageHostRef = viewChild<ElementRef<HTMLDivElement>>('stage');

    protected readonly currentSlide = computed(() => {
        const planValue = this.plan();
        if (!planValue) return null;
        return planValue.slides[this.currentIndex()] ?? null;
    });

    protected readonly slideCount = computed(() => this.plan()?.slides.length ?? 0);

    protected readonly isLastSlide = computed(
        () => this.slideCount() > 0 && this.currentIndex() >= this.slideCount() - 1,
    );

    // Slide types that hold until the viewer taps / keyboards forward (no
    // auto-advance). Welcome is intentionally a "freeze frame": the first
    // moment lands and the viewer drives the next beat themselves.
    private readonly MANUAL_ADVANCE_TYPES: ReadonlySet<PlanSlide['type']> = new Set([
        'welcome',
    ]);

    protected readonly currentSlideAutoAdvances = computed(() => {
        const slide = this.currentSlide();
        return slide !== null && !this.MANUAL_ADVANCE_TYPES.has(slide.type);
    });

    private animationFrame = 0;
    private lastTickMs = 0;
    private holdTimer: ReturnType<typeof setTimeout> | null = null;
    private pointerDownAt = 0;
    private pointerDownX = 0;

    constructor() {
        effect(() => {
            const params = this.uuid();
            const uuidValue = params?.get('uuid') ?? '';
            if (uuidValue) {
                this.loadPlan(uuidValue);
            }
        });

        // Force dark mode while the stories viewer is mounted.
        const html = document.documentElement;
        const hadDark = html.classList.contains('ec-hackaton-dark');
        html.classList.add('ec-hackaton-dark');
        this.destroyRef.onDestroy(() => {
            if (!hadDark) html.classList.remove('ec-hackaton-dark');
            this.stopTimer();
            if (this.holdTimer) clearTimeout(this.holdTimer);
        });

        // Auto-advance ticker. Tracks currentIndex explicitly so the timer
        // restarts on every slide change (isLastSlide only flips at the end,
        // so relying on it alone would strand the ticker after slide 0).
        effect(() => {
            const ready = this.state() === 'ready';
            this.currentIndex();
            const isLast = this.isLastSlide();
            const isPaused = this.paused();
            const reduceMotion = this.prefersReducedMotion();
            const autoAdvances = this.currentSlideAutoAdvances();

            this.stopTimer();
            if (!ready || isPaused || isLast || reduceMotion || !autoAdvances) {
                return;
            }
            this.startTimer();
        });

        // Reset fraction when index changes. Last slide stays "complete" so
        // its progress bar reads as fully filled (no auto-advance follows).
        effect(() => {
            this.currentIndex();
            this.progressFraction.set(this.isLastSlide() ? 1 : 0);
        });
    }

    private async loadPlan(uuid: string): Promise<void> {
        this.state.set('loading');
        const data = await this.planShareService.getPlanByUuid(uuid);
        if (!data) {
            this.state.set('not-found');
            this.plan.set(null);
            return;
        }
        this.plan.set(data);
        this.currentIndex.set(0);
        this.progressFraction.set(0);
        this.paused.set(false);
        this.state.set('ready');
    }

    protected onClose(): void {
        this.router.navigateByUrl('/plan');
    }

    protected onReplay(): void {
        this.currentIndex.set(0);
        this.paused.set(false);
    }

    protected onPointerDown(event: PointerEvent): void {
        if (this.state() !== 'ready') return;
        this.pointerDownAt = performance.now();
        this.pointerDownX = event.clientX;
        this.holdTimer = setTimeout(() => {
            this.paused.set(true);
            this.holdTimer = null;
        }, HOLD_TO_PAUSE_THRESHOLD_MS);
    }

    protected onPointerUp(event: PointerEvent): void {
        if (this.state() !== 'ready') return;
        const heldMs = performance.now() - this.pointerDownAt;
        if (this.holdTimer) {
            clearTimeout(this.holdTimer);
            this.holdTimer = null;
        }
        if (this.paused()) {
            this.paused.set(false);
            return;
        }
        if (heldMs < HOLD_TO_PAUSE_THRESHOLD_MS) {
            this.handleTap(event);
        }
    }

    protected onPointerCancel(): void {
        if (this.holdTimer) {
            clearTimeout(this.holdTimer);
            this.holdTimer = null;
        }
        if (this.paused()) {
            this.paused.set(false);
        }
    }

    private handleTap(event: PointerEvent): void {
        const stage = this.stageHostRef()?.nativeElement;
        if (!stage) return;
        const rect = stage.getBoundingClientRect();
        const x = event.clientX - rect.left;
        if (x < rect.width / 3) {
            this.previousSlide();
        } else {
            this.nextSlide();
        }
    }

    protected previousSlide(): void {
        if (this.currentIndex() > 0) {
            this.currentIndex.update((i) => i - 1);
        }
    }

    protected nextSlide(): void {
        if (this.currentIndex() < this.slideCount() - 1) {
            this.currentIndex.update((i) => i + 1);
        }
    }

    @HostListener('document:keydown', ['$event'])
    protected onKeydown(event: KeyboardEvent): void {
        if (this.state() !== 'ready') return;
        switch (event.key) {
            case 'ArrowLeft':
                this.previousSlide();
                event.preventDefault();
                break;
            case 'ArrowRight':
            case ' ':
            case 'Enter':
                this.nextSlide();
                event.preventDefault();
                break;
            case 'Escape':
                this.onClose();
                event.preventDefault();
                break;
        }
    }

    private startTimer(): void {
        this.lastTickMs = performance.now();
        const tick = (now: number): void => {
            const delta = now - this.lastTickMs;
            this.lastTickMs = now;
            const next = Math.min(1, this.progressFraction() + delta / SLIDE_DURATION_MS);
            this.progressFraction.set(next);
            if (next >= 1) {
                this.nextSlide();
                return;
            }
            this.animationFrame = requestAnimationFrame(tick);
        };
        this.animationFrame = requestAnimationFrame(tick);
    }

    private stopTimer(): void {
        if (this.animationFrame) {
            cancelAnimationFrame(this.animationFrame);
            this.animationFrame = 0;
        }
    }

    private prefersReducedMotion(): boolean {
        return (
            typeof window !== 'undefined' &&
            window.matchMedia('(prefers-reduced-motion: reduce)').matches
        );
    }
}
