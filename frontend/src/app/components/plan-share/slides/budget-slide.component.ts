import {
    ChangeDetectionStrategy,
    Component,
    DestroyRef,
    effect,
    inject,
    input,
    signal,
} from '@angular/core';
import { TranslocoModule } from '@jsverse/transloco';

import { PlanBudgetSlide } from '../plan-share.model';

/**
 * Budget slide: huge yellow count-up to the target amount, then breakdown.
 * Spec: 80 px Bold-900 yellow number, count-up 1200 ms ease-out.
 * prefers-reduced-motion: skip count-up, show final immediately.
 */
@Component({
    selector: 'app-budget-slide',
    imports: [TranslocoModule],
    template: `
        <div class="ec-slide">
            <h1 class="ec-slide__headline">
                {{ 'plan.story.budget.headline' | transloco }}
            </h1>
            <span class="ec-slide__accent" aria-hidden="true"></span>

            <div class="ec-slide__amount" aria-live="polite">
                <span class="ec-slide__amount-number">{{ displayAmount() }}</span>
                <span class="ec-slide__amount-unit">RON</span>
            </div>

            <p class="ec-slide__sub">
                {{ 'plan.story.budget.suffix' | transloco }}
            </p>

            <ul class="ec-slide__breakdown">
                @for (line of slide().breakdown; track line.key) {
                    <li class="ec-slide__line">
                        <span>{{ 'plan.story.budget.line.' + line.key | transloco }}</span>
                        <span>{{ line.ron }} RON</span>
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
            gap: var(--space-2);
            text-align: center;
        }
        .ec-slide__headline {
            margin: 0;
            font-size: 30px;
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
            margin-bottom: var(--space-4);
        }
        .ec-slide__amount {
            display: flex;
            align-items: baseline;
            gap: var(--space-3);
        }
        .ec-slide__amount-number {
            font-size: 80px;
            font-weight: 900;
            line-height: 1;
            color: var(--ec-yellow);
            font-variant-numeric: tabular-nums;
        }
        .ec-slide__amount-unit {
            font-size: 22px;
            font-weight: 700;
            color: var(--ec-white);
            letter-spacing: 0.08em;
        }
        .ec-slide__sub {
            margin: 0;
            font-size: 13px;
            color: rgba(255, 255, 255, 0.7);
            text-transform: uppercase;
            letter-spacing: 0.08em;
        }
        .ec-slide__breakdown {
            list-style: none;
            padding: 0;
            margin: var(--space-5) 0 0;
            display: flex;
            flex-direction: column;
            gap: var(--space-2);
            width: 100%;
            max-width: 320px;
            border-top: 1px solid rgba(255, 230, 0, 0.4);
            padding-top: var(--space-4);
        }
        .ec-slide__line {
            display: flex;
            justify-content: space-between;
            font-size: 13px;
            color: rgba(255, 255, 255, 0.85);
        }
    `,
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class BudgetSlideComponent {
    private readonly destroyRef = inject(DestroyRef);

    readonly slide = input.required<PlanBudgetSlide>();

    private readonly currentAmount = signal(0);
    private animationFrame = 0;

    protected displayAmount(): number {
        return this.currentAmount();
    }

    constructor() {
        effect(() => {
            const target = this.slide().amountRon;
            this.startCountUp(target);
        });

        this.destroyRef.onDestroy(() => {
            cancelAnimationFrame(this.animationFrame);
        });
    }

    private startCountUp(target: number): void {
        const reduceMotion =
            typeof window !== 'undefined' &&
            window.matchMedia('(prefers-reduced-motion: reduce)').matches;
        if (reduceMotion) {
            this.currentAmount.set(target);
            return;
        }

        const duration = 1200;
        const start = performance.now();
        const easeOut = (t: number): number => 1 - Math.pow(1 - t, 3);

        const tick = (now: number): void => {
            const elapsed = now - start;
            const t = Math.min(1, elapsed / duration);
            const eased = easeOut(t);
            this.currentAmount.set(Math.round(target * eased));
            if (t < 1) {
                this.animationFrame = requestAnimationFrame(tick);
            }
        };
        cancelAnimationFrame(this.animationFrame);
        this.animationFrame = requestAnimationFrame(tick);
    }
}
