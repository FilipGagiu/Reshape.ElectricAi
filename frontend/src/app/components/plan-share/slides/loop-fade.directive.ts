import { Directive, ElementRef, OnDestroy, OnInit, inject } from '@angular/core';

const FADE_WINDOW_SECONDS = 0.8;
const DIM_FLOOR = 0.15;
const TICK_MS = 33;

@Directive({
    selector: 'video[appSlideLoopFade]',
})
export class SlideLoopFadeDirective implements OnInit, OnDestroy {
    private readonly host = inject(ElementRef<HTMLVideoElement>);
    private intervalId: ReturnType<typeof setInterval> | null = null;

    ngOnInit(): void {
        this.intervalId = setInterval(() => this.update(), TICK_MS);
    }

    ngOnDestroy(): void {
        if (this.intervalId !== null) clearInterval(this.intervalId);
    }

    private update(): void {
        const video = this.host.nativeElement;
        const duration = video.duration;
        if (!isFinite(duration) || duration <= 0) return;

        const time = video.currentTime;
        const fromStart = time / FADE_WINDOW_SECONDS;
        const fromEnd = (duration - time) / FADE_WINDOW_SECONDS;
        const proximity = Math.min(1, fromStart, fromEnd);
        const opacity = DIM_FLOOR + (1 - DIM_FLOOR) * Math.max(0, proximity);
        video.style.opacity = opacity.toFixed(3);
    }
}
