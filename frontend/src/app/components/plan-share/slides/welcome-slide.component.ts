import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { TranslocoModule } from '@jsverse/transloco';

import { PlanWelcomeSlide } from '../plan-share.model';

@Component({
    selector: 'app-welcome-slide',
    imports: [TranslocoModule],
    template: `
        <div class="ec-slide">
            <span class="ec-slide__monogram">
                <span class="ec-slide__monogram-letters">EC</span>
            </span>
            <h1 class="ec-slide__headline">
                {{ 'plan.story.welcome.headline' | transloco }}
            </h1>
            <p class="ec-slide__body">
                {{ 'plan.story.welcome.body' | transloco: { name: slide().name } }}
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
            background-color: var(--ec-red);
            display: flex;
            flex-direction: column;
            align-items: center;
            justify-content: center;
            padding: var(--space-12) var(--space-6);
            gap: var(--space-6);
            text-align: center;
        }
        .ec-slide__monogram {
            display: inline-flex;
            align-items: center;
            justify-content: center;
            width: 96px;
            height: 72px;
            background-color: var(--ec-red);
            border: 3px solid var(--ec-white);
            border-radius: var(--radius-sm);
            box-shadow: var(--shadow-glow-red);
            animation: ec-slide-fade-up var(--duration-deliberate) var(--ease-emphasized) both;
        }
        .ec-slide__monogram-letters {
            color: var(--ec-white);
            font-weight: 900;
            font-size: 36px;
            font-style: italic;
            letter-spacing: 0.02em;
            line-height: 1;
        }
        .ec-slide__headline {
            margin: 0;
            font-size: 44px;
            font-weight: 800;
            text-transform: uppercase;
            line-height: 1.05;
            color: var(--ec-yellow);
            animation: ec-slide-fade-up var(--duration-deliberate) var(--ease-emphasized) 100ms both;
        }
        .ec-slide__body {
            margin: 0;
            font-size: 16px;
            line-height: 1.55;
            color: rgba(255, 255, 255, 0.92);
            max-width: 360px;
            animation: ec-slide-fade-up var(--duration-deliberate) var(--ease-emphasized) 200ms both;
        }
        @keyframes ec-slide-fade-up {
            from {
                opacity: 0;
                transform: translateY(24px);
            }
            to {
                opacity: 1;
                transform: translateY(0);
            }
        }
        @media (prefers-reduced-motion: reduce) {
            .ec-slide__monogram,
            .ec-slide__headline,
            .ec-slide__body {
                animation: none;
            }
        }
    `,
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WelcomeSlideComponent {
    readonly slide = input.required<PlanWelcomeSlide>();
}
