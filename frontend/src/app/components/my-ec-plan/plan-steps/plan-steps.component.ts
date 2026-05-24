import {
    ChangeDetectionStrategy,
    Component,
    ElementRef,
    Signal,
    computed,
    effect,
    inject,
    signal,
    untracked,
    viewChild,
} from '@angular/core';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { TranslocoModule, TranslocoService } from '@jsverse/transloco';

import { EcTopbarComponent } from '@shared/components/ec-topbar/ec-topbar.component';
import { AuthService } from '@shared/services/auth.service';
import { PlanOnboardingService } from '@shared/services/plan-onboarding.service';

import {
    WizardProgressComponent,
    WizardProgressStep,
} from '@components/my-ec-plan/ui/wizard-progress/wizard-progress.component';

import { PlanIntakeQuestion } from '../plan-intake/models/plan-intake.model';
import { PlanIntakeService } from '../plan-intake/services/plan-intake.service';

interface LoaderStep {
    readonly iconClass: string;
    readonly copyKey: string;
}

const LOADER_STEPS: ReadonlyArray<LoaderStep> = [
    { iconClass: 'pi-headphones', copyKey: 'plan.intake.loader.curating' },
    { iconClass: 'pi-map-marker', copyKey: 'plan.intake.loader.mapping' },
    { iconClass: 'pi-ticket', copyKey: 'plan.intake.loader.matching' },
    { iconClass: 'pi-shopping-bag', copyKey: 'plan.intake.loader.tasting' },
    { iconClass: 'pi-sun', copyKey: 'plan.intake.loader.timing' },
    { iconClass: 'pi-star', copyKey: 'plan.intake.loader.polishing' },
];
const LOADER_STEP_INTERVAL_MS = 1500;

@Component({
    selector: 'app-plan-steps',
    imports: [
        TranslocoModule,
        ReactiveFormsModule,
        EcTopbarComponent,
        WizardProgressComponent,
    ],
    templateUrl: './plan-steps.component.html',
    styleUrl: './plan-steps.component.css',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PlanStepsComponent {
    private readonly transloco = inject(TranslocoService);
    private readonly router = inject(Router);
    private readonly auth = inject(AuthService);
    private readonly planOnboarding = inject(PlanOnboardingService);
    protected readonly service = inject(PlanIntakeService);

    protected readonly textControl = new FormControl<string>('', { nonNullable: true });

    private readonly textValueSignal = toSignal(this.textControl.valueChanges, {
        initialValue: '',
    });

    protected readonly currentQuestion: Signal<PlanIntakeQuestion | null> =
        this.service.currentQuestion;
    protected readonly status = this.service.status;
    protected readonly answeredCount = this.service.answeredCount;
    protected readonly totalQuestions = this.service.totalQuestions;
    protected readonly isCompleted = this.service.isCompleted;
    protected readonly currentIndex = this.service.currentIndex;
    protected readonly answers = this.service.answers;

    protected readonly isSubmitting = computed(() => this.status() === 'submitting');
    protected readonly hasError = computed(() => this.status() === 'error');
    protected readonly isCollecting = computed(() => this.status() === 'collecting');

    protected readonly isLastQuestion = computed(
        () => this.currentIndex() === this.totalQuestions() - 1,
    );
    protected readonly isFirstQuestion = computed(() => this.currentIndex() === 0);
    protected readonly canSkip = computed(() => !this.isFirstQuestion() && this.isCollecting());
    protected readonly canGoForward = computed(
        () => this.currentIndex() < this.answers().length && !this.isLastQuestion(),
    );

    protected readonly counterParams = computed(() => ({
        current: Math.min(this.currentIndex() + 1, this.totalQuestions()),
        total: this.totalQuestions(),
    }));

    protected readonly canContinue = computed(
        () => this.isCollecting() && this.textValueSignal().trim().length > 0,
    );

    protected readonly continueLabelKey = computed(() =>
        this.isLastQuestion() ? 'plan.steps.actions.finish' : 'plan.steps.actions.continue',
    );

    protected readonly progressSteps = computed<ReadonlyArray<WizardProgressStep>>(() => {
        const total = this.totalQuestions();
        const active = this.currentIndex();
        const reached = Math.max(active, this.answers().length);
        return Array.from({ length: total }, (_, index) => ({
            index,
            active: index === active,
            reachable: index <= reached,
        }));
    });

    protected readonly promptKey = computed(
        () => this.currentQuestion()?.promptKey ?? '',
    );
    protected readonly placeholderKey = computed(
        () => this.currentQuestion()?.placeholderKey ?? 'plan.intake.input.placeholder',
    );
    protected readonly descriptionKey = computed(
        () => this.currentQuestion()?.descriptionKey ?? null,
    );
    protected readonly suggestionKeys = computed<ReadonlyArray<string>>(
        () => this.currentQuestion()?.suggestionKeys ?? [],
    );

    private readonly textareaRef = viewChild<ElementRef<HTMLTextAreaElement>>('textarea');

    private readonly loaderStepIndex = signal(0);
    protected readonly loaderStep = computed<LoaderStep>(
        () => LOADER_STEPS[this.loaderStepIndex() % LOADER_STEPS.length],
    );

    constructor() {
        effect(() => {
            const question = this.currentQuestion();
            const index = this.currentIndex();
            if (!question) return;
            untracked(() => {
                const existing = this.service.answers()[index];
                const prefill = existing && !existing.skipped ? existing.text : '';
                this.textControl.setValue(prefill, { emitEvent: false });
                queueMicrotask(() => {
                    this.textareaRef()?.nativeElement.focus();
                    this.resizeTextarea();
                });
            });
        });

        effect((onCleanup) => {
            const running = this.isSubmitting();
            if (!running) {
                this.loaderStepIndex.set(0);
                return;
            }
            const timerId = setInterval(
                () => this.loaderStepIndex.update((value) => value + 1),
                LOADER_STEP_INTERVAL_MS,
            );
            onCleanup(() => clearInterval(timerId));
        });

        this.textControl.valueChanges
            .pipe(takeUntilDestroyed())
            .subscribe(() => queueMicrotask(() => this.resizeTextarea()));
    }

    protected applyChipKey(chipKey: string): void {
        const translated = this.transloco.translate(chipKey);
        this.textControl.setValue(translated);
        queueMicrotask(() => {
            this.textareaRef()?.nativeElement.focus();
            this.resizeTextarea();
        });
    }

    protected submit(): void {
        if (!this.canContinue()) return;
        const text = this.textControl.value.trim();
        if (!text) return;
        this.service.submitAnswer(text);
    }

    protected onKeydown(event: KeyboardEvent): void {
        if (event.key === 'Enter' && !event.shiftKey) {
            event.preventDefault();
            this.submit();
        }
    }

    protected retry(): void {
        void this.service.retrySubmit();
    }

    protected back(): void {
        if (this.isFirstQuestion()) {
            this.router.navigateByUrl('/');
            return;
        }
        this.service.previous();
    }

    protected forward(): void {
        if (!this.canGoForward()) return;
        this.service.goForward();
    }

    protected skip(): void {
        if (!this.canSkip()) return;
        this.service.skipCurrent();
    }

    protected skipOnboarding(): void {
        this.planOnboarding.markCompleted(this.auth.currentUser()?.email);
        void this.router.navigateByUrl('/');
    }

    private resizeTextarea(): void {
        const textarea = this.textareaRef()?.nativeElement;
        if (!textarea) return;
        textarea.style.height = 'auto';
        const lineHeight = 24;
        const maxLines = 5;
        const maxHeight = lineHeight * maxLines;
        const next = Math.min(textarea.scrollHeight, maxHeight);
        textarea.style.height = `${next}px`;
        textarea.style.overflowY = textarea.scrollHeight > maxHeight ? 'auto' : 'hidden';
    }
}
