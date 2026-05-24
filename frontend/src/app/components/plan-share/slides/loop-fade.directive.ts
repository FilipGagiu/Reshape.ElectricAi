import { Directive, ElementRef, OnDestroy, OnInit, inject } from '@angular/core';

const FADE_WINDOW_SECONDS = 0.8;
const DIM_FLOOR = 0.15;
const TICK_MS = 33;
const SCRIM_SELECTOR = '.ec-slide__scrim';
const SCRIM_FILTER_LOADING = 'contrast(5) saturate(0.1) brightness(0.5)';
const SCRIM_FILTER_READY = 'contrast(5) saturate(1) brightness(1)';
const SCRIM_TRANSITION = 'filter 200ms ease-out';
const SLIDE_BG_LOADING = 'var(--ec-dark-navy)';
const SLIDE_TRANSITION = 'background-color 200ms ease-out';

@Directive({
    selector: 'video[appSlideLoopFade]',
})
export class SlideLoopFadeDirective implements OnInit, OnDestroy {
    private readonly host = inject(ElementRef<HTMLVideoElement>);
    private intervalId: ReturnType<typeof setInterval> | null = null;
    private hasLooped = false;
    private prevTime = -1;
    private scrim: HTMLElement | null = null;
    private slide: HTMLElement | null = null;
    private readonly onVideoReady = (): void => {
        if (this.scrim) this.scrim.style.filter = SCRIM_FILTER_READY;
        // Clearing the inline background-color lets the CSS-defined colour
        // (var(--ec-red) on welcome/share, dark-navy elsewhere) take over,
        // animated by the inline transition we set in ngOnInit.
        if (this.slide) this.slide.style.backgroundColor = '';
    };

    ngOnInit(): void {
        const video = this.host.nativeElement;
        const parent = video.parentElement;
        this.slide = parent instanceof HTMLElement ? parent : null;
        const found = parent?.querySelector(SCRIM_SELECTOR) ?? null;
        this.scrim = found instanceof HTMLElement ? found : null;

        if (this.slide) {
            // Open every slide on the neutral dark-navy wash. Final colour
            // (e.g. ec-red on the welcome slide) flips in when the video has
            // data, transitioning over 200ms — no bright tint before the bg.
            this.slide.style.transition = SLIDE_TRANSITION;
            this.slide.style.backgroundColor = SLIDE_BG_LOADING;
        }
        if (this.scrim) {
            this.scrim.style.filter = SCRIM_FILTER_LOADING;
            this.scrim.style.transition = SCRIM_TRANSITION;
        }
        video.addEventListener('loadeddata', this.onVideoReady);
        if (video.readyState >= 2) this.onVideoReady();
        this.intervalId = setInterval(() => this.update(), TICK_MS);
    }

    ngOnDestroy(): void {
        if (this.intervalId !== null) clearInterval(this.intervalId);
        this.host.nativeElement.removeEventListener('loadeddata', this.onVideoReady);
    }

    private update(): void {
        const video = this.host.nativeElement;
        const duration = video.duration;
        if (!isFinite(duration) || duration <= 0) return;

        const time = video.currentTime;
        if (this.prevTime >= 0 && time < this.prevTime) this.hasLooped = true;
        this.prevTime = time;

        // First playthrough — keep the video at full opacity so it appears in
        // sync with the scrim instead of flashing a flat tint over a dim frame.
        if (!this.hasLooped) {
            video.style.opacity = '1';
            return;
        }

        const fromStart = time / FADE_WINDOW_SECONDS;
        const fromEnd = (duration - time) / FADE_WINDOW_SECONDS;
        const proximity = Math.min(1, fromStart, fromEnd);
        const opacity = DIM_FLOOR + (1 - DIM_FLOOR) * Math.max(0, proximity);
        video.style.opacity = opacity.toFixed(3);
    }
}
