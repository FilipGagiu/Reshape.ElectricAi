import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { TranslocoModule } from '@jsverse/transloco';

import { SlideLoopFadeDirective } from './loop-fade.directive';

import { PlanFoodSlide } from '../plan-share.model';

@Component({
    selector: 'app-food-slide',
    imports: [TranslocoModule, SlideLoopFadeDirective],
    template: `
        <div class="ec-slide">
            <video
                class="ec-slide__video" appSlideLoopFade
                src="/media/sliders/slide_5_bg.mp4"
                autoplay
                muted
                loop
                playsinline
                disablepictureinpicture
                aria-hidden="true"
            ></video>
            <span class="ec-slide__scrim" aria-hidden="true"></span>
            <h1 class="ec-slide__headline">
                {{ 'plan.story.food.headline' | transloco }}
            </h1>
            <span class="ec-slide__accent" aria-hidden="true"></span>

            @if (slide().cuisines.length > 0) {
                <div class="ec-slide__chips">
                    @for (cuisine of slide().cuisines; track cuisine) {
                        <span class="ec-slide__chip">
                            {{ 'plan.story.food.cuisine.' + cuisine | transloco }}
                        </span>
                    }
                </div>
                <p class="ec-slide__body">
                    {{ 'plan.story.food.body' | transloco }}
                </p>
            } @else {
                <p class="ec-slide__body">
                    {{ 'plan.story.food.fallback' | transloco }}
                </p>
            }

            @if (slide().allergies.length > 0) {
                <p class="ec-slide__note">
                    {{ 'plan.story.food.allergyNote' | transloco }}
                </p>
            }
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
            background-color: rgb(130 3 98 / 55%);
            z-index: 1;
            pointer-events: none;
            filter: contrast(5);
        }
        .ec-slide__headline,
        .ec-slide__accent,
        .ec-slide__chips,
        .ec-slide__body,
        .ec-slide__note {
            position: relative;
            z-index: 2;
        }
        .ec-slide__headline {
            margin: 0;
            font-size: 32px;
            font-weight: 800;
            text-transform: uppercase;
            line-height: 1.1;
            color: var(--ec-white);
        }
        .ec-slide__accent {
            display: block;
            width: 64px;
            height: 2px;
            background-color: var(--ec-red);
        }
        .ec-slide__chips {
            display: flex;
            flex-wrap: wrap;
            justify-content: center;
            gap: var(--space-2);
            margin-top: var(--space-4);
        }
        .ec-slide__chip {
            padding: var(--space-2) var(--space-4);
            background-color: transparent;
            color: var(--ec-yellow);
            border: 1px solid var(--ec-yellow);
            border-radius: var(--radius-none);
            font-size: 14px;
            font-weight: 700;
            text-transform: uppercase;
            letter-spacing: 0.04em;
        }
        .ec-slide__body {
            margin: var(--space-4) 0 0;
            font-size: 22px;
            font-weight: 700;
            line-height: 1.45;
            color: var(--ec-white);
            max-width: 360px;
        }
        .ec-slide__note {
            margin: var(--space-3) 0 0;
            font-size: 12px;
            color: rgba(255, 255, 255, 0.6);
            text-transform: uppercase;
            letter-spacing: 0.06em;
        }
    `,
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FoodSlideComponent {
    readonly slide = input.required<PlanFoodSlide>();
}
