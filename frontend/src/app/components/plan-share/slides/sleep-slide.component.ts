import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { TranslocoModule } from '@jsverse/transloco';

import { SlideLoopFadeDirective } from './loop-fade.directive';

import { AccommodationKind, PlanSleepSlide } from '../plan-share.model';

const ICON_BY_KIND: Record<AccommodationKind, string> = {
    festivalCamping: 'pi-home',
    bontida: 'pi-map-marker',
    cluj: 'pi-building',
};

@Component({
    selector: 'app-sleep-slide',
    imports: [TranslocoModule, SlideLoopFadeDirective],
    template: `
        <div class="ec-slide">
            <video
                class="ec-slide__video" appSlideLoopFade
                src="/media/sliders/slide_3_bg.mp4"
                autoplay
                muted
                loop
                playsinline
                disablepictureinpicture
                aria-hidden="true"
            ></video>
            <span class="ec-slide__scrim" aria-hidden="true"></span>
            <h1 class="ec-slide__headline">
                {{ 'plan.story.sleep.headline' | transloco }}
            </h1>
            <span class="ec-slide__accent" aria-hidden="true"></span>

            <i [class]="'pi ' + iconClass() + ' ec-slide__icon'" aria-hidden="true"></i>
            <p class="ec-slide__location">
                {{ 'plan.story.sleep.accommodation.' + slide().accommodation | transloco }}
            </p>

            <p class="ec-slide__body">
                {{ 'plan.story.sleep.tip.' + slide().accommodation | transloco }}
            </p>
        </div>
    `,
    styles: `
        :host {
            display: contents;
        }
        .ec-slide {
            position: absolute;
            inset: 0;
            background-color: var(--ec-dark-navy);
            display: flex;
            flex-direction: column;
            align-items: center;
            justify-content: center;
            padding: var(--space-12) var(--space-6);
            gap: var(--space-4);
            text-align: center;
            overflow: hidden;
            isolation: isolate;
        }
        .ec-slide__video {
            position: absolute;
            inset: 0;
            width: 100%;
            height: 100%;
            object-fit: cover;
            filter: blur(3px);
            transform: scale(1.04);
            z-index: 0;
            pointer-events: none;
        }
        .ec-slide__scrim {
            position: absolute;
            inset: 0;
            background-color: rgb(32 115 12 / 60%);
            z-index: 1;
            pointer-events: none;
            filter: contrast(5);
        }
        .ec-slide__headline,
        .ec-slide__accent,
        .ec-slide__icon,
        .ec-slide__location,
        .ec-slide__body {
            position: relative;
            z-index: 2;
        }
        .ec-slide__headline {
            margin: 0;
            font-size: 36px;
            font-weight: 800;
            text-transform: uppercase;
            line-height: 1.1;
            color: var(--ec-white);
            animation: ec-slide-fade-up var(--duration-deliberate) var(--ease-emphasized) both;
        }
        .ec-slide__accent {
            display: block;
            width: 64px;
            height: 2px;
            background-color: var(--ec-red);
        }
        .ec-slide__icon {
            font-size: 56px;
            color: var(--ec-white);
            margin-top: var(--space-4);
            animation: ec-slide-fade-up var(--duration-deliberate) var(--ease-emphasized) 100ms both;
        }
        .ec-slide__location {
            margin: 0;
            font-size: 28px;
            font-weight: 700;
            text-transform: uppercase;
            color: var(--ec-yellow);
            letter-spacing: 0.04em;
            animation: ec-slide-fade-up var(--duration-deliberate) var(--ease-emphasized) 150ms both;
        }
        .ec-slide__body {
            margin: var(--space-3) 0 0;
            font-size: 14px;
            line-height: 1.5;
            color: rgba(255, 255, 255, 0.78);
            max-width: 320px;
            animation: ec-slide-fade-up var(--duration-deliberate) var(--ease-emphasized) 220ms both;
        }
        @keyframes ec-slide-fade-up {
            from {
                opacity: 0;
                transform: translateY(16px);
            }
            to {
                opacity: 1;
                transform: translateY(0);
            }
        }
        @media (prefers-reduced-motion: reduce) {
            .ec-slide__headline,
            .ec-slide__icon,
            .ec-slide__location,
            .ec-slide__body {
                animation: none;
            }
        }
    `,
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SleepSlideComponent {
    readonly slide = input.required<PlanSleepSlide>();

    protected readonly iconClass = computed(() => ICON_BY_KIND[this.slide().accommodation]);
}
