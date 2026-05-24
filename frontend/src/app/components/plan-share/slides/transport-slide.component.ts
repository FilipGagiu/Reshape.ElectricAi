import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { TranslocoModule } from '@jsverse/transloco';

import { SlideLoopFadeDirective } from './loop-fade.directive';

import { PlanTransportSlide, TransportMethod } from '../plan-share.model';

const ICON_BY_METHOD: Record<TransportMethod, string> = {
    car: 'pi-car',
    train: 'pi-arrow-right',
    bus: 'pi-truck',
    bike: 'pi-circle',
    onFoot: 'pi-walking',
};

@Component({
    selector: 'app-transport-slide',
    imports: [TranslocoModule, SlideLoopFadeDirective],
    template: `
        <div class="ec-slide">
            <video
                class="ec-slide__video" appSlideLoopFade
                src="/media/sliders/slide_2_bg.mp4"
                autoplay
                muted
                loop
                playsinline
                disablepictureinpicture
                aria-hidden="true"
            ></video>
            <span class="ec-slide__scrim" aria-hidden="true"></span>
            <h1 class="ec-slide__headline">
                {{ 'plan.story.transport.headline' | transloco }}
            </h1>
            <span class="ec-slide__accent" aria-hidden="true"></span>

            <div class="ec-slide__route">
                <span class="ec-slide__city">{{ slide().from }}</span>
                <span class="ec-slide__dots" aria-hidden="true">· · · · · ·</span>
                <i [class]="'pi ' + iconClass() + ' ec-slide__icon'" aria-hidden="true"></i>
                <span class="ec-slide__dots" aria-hidden="true">· · · · · ·</span>
                <span class="ec-slide__city">BONȚIDA</span>
            </div>

            <p class="ec-slide__body">
                {{
                    'plan.story.transport.method.' + slide().method | transloco
                }}
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
            background-color: rgb(128 129 10 / 55%);
            z-index: 1;
            pointer-events: none;
            filter: contrast(5);
        }
        .ec-slide__headline,
        .ec-slide__accent,
        .ec-slide__route,
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
        .ec-slide__route {
            display: flex;
            flex-direction: column;
            align-items: center;
            gap: var(--space-2);
            margin-top: var(--space-6);
            animation: ec-slide-fade-up var(--duration-deliberate) var(--ease-emphasized) 100ms both;
        }
        .ec-slide__city {
            font-size: 22px;
            font-weight: 700;
            color: var(--ec-yellow);
            letter-spacing: 0.04em;
        }
        .ec-slide__dots {
            color: rgba(255, 255, 255, 0.5);
            font-size: 18px;
            letter-spacing: 0.3em;
        }
        .ec-slide__icon {
            font-size: 36px;
            color: var(--ec-white);
        }
        .ec-slide__body {
            margin: var(--space-4) 0 0;
            font-size: 22px;
            font-weight: 700;
            line-height: 1.45;
            color: var(--ec-white);
            max-width: 340px;
            animation: ec-slide-fade-up var(--duration-deliberate) var(--ease-emphasized) 200ms both;
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
            .ec-slide__route,
            .ec-slide__body {
                animation: none;
            }
        }
    `,
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TransportSlideComponent {
    readonly slide = input.required<PlanTransportSlide>();

    protected readonly iconClass = computed(() => ICON_BY_METHOD[this.slide().method]);
}
