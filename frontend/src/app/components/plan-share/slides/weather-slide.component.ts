import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { TranslocoModule } from '@jsverse/transloco';

import { PlanWeatherSlide } from '../plan-share.model';

@Component({
    selector: 'app-weather-slide',
    imports: [TranslocoModule],
    template: `
        <div class="ec-slide">
            <span class="ec-slide__stripe" aria-hidden="true"></span>

            <h1 class="ec-slide__headline">
                {{ 'plan.story.weather.headline' | transloco }}
            </h1>
            <span class="ec-slide__accent" aria-hidden="true"></span>

            <div class="ec-slide__temp">
                <span class="ec-slide__temp-range">
                    {{ slide().tempLow }}° to {{ slide().tempHigh }}°
                </span>
                <i class="pi pi-sun ec-slide__icon" aria-hidden="true"></i>
            </div>

            <ul class="ec-slide__tips">
                @for (tipKey of slide().tipKeys; track tipKey) {
                    <li class="ec-slide__tip">
                        {{ 'plan.story.weather.tip.' + tipKey | transloco }}
                    </li>
                }
            </ul>
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
        }
        .ec-slide__stripe {
            position: absolute;
            top: 0;
            left: 0;
            right: 0;
            height: 12px;
            background-color: var(--ec-pass-youth);
        }
        .ec-slide__headline {
            margin: 0;
            font-size: 28px;
            font-weight: 800;
            text-transform: uppercase;
            line-height: 1.1;
            color: var(--ec-white);
            max-width: 320px;
        }
        .ec-slide__accent {
            display: block;
            width: 64px;
            height: 2px;
            background-color: var(--ec-red);
        }
        .ec-slide__temp {
            display: flex;
            align-items: center;
            gap: var(--space-3);
            margin-top: var(--space-4);
        }
        .ec-slide__temp-range {
            font-size: 44px;
            font-weight: 900;
            color: var(--ec-yellow);
            font-variant-numeric: tabular-nums;
        }
        .ec-slide__icon {
            font-size: 36px;
            color: var(--ec-yellow);
        }
        .ec-slide__tips {
            list-style: none;
            padding: 0;
            margin: var(--space-4) 0 0;
            display: flex;
            flex-direction: column;
            gap: var(--space-2);
            max-width: 320px;
            width: 100%;
        }
        .ec-slide__tip {
            font-size: 14px;
            color: rgba(255, 255, 255, 0.85);
            text-align: left;
            padding-left: var(--space-4);
            position: relative;
            line-height: 1.5;
        }
        .ec-slide__tip::before {
            content: '';
            position: absolute;
            left: 0;
            top: 0.55em;
            width: 6px;
            height: 6px;
            background-color: var(--ec-yellow);
        }
    `,
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WeatherSlideComponent {
    readonly slide = input.required<PlanWeatherSlide>();
}
