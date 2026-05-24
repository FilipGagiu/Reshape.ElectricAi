import { Directive, ElementRef, OnDestroy, OnInit, inject } from '@angular/core';

const FADE_WINDOW_SECONDS = 0.8;
const DIM_FLOOR = 0.15;
const TICK_MS = 33;
const SCRIM_SELECTOR = '.ec-slide__scrim';
const SCRIM_TRANSITION = 'opacity 240ms ease-out';

@Directive({
    selector: 'video[appSlideLoopFade]',
})
export class SlideLoopFadeDirective implements OnInit, OnDestroy {
    private readonly host = inject(ElementRef<HTMLVideoElement>);
    private intervalId: ReturnType<typeof setInterval> | null = null;
    private hasLooped = false;
    private prevTime = -1;
    private scrim: HTMLElement | null = null;
    private readonly onVideoReady = (): void => {
        if (this.scrim) this.scrim.style.opacity = '1';
    };

    ngOnInit(): void {
        const video = this.host.nativeElement;
        const found = video.parentElement?.querySelector(SCRIM_SELECTOR) ?? null;
        this.scrim = found instanceof HTMLElement ? found : null;
        if (this.scrim) {
            this.scrim.style.opacity = '0';
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
