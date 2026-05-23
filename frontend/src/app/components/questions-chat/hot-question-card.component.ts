import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';

import { HotQuestion } from './models/question.model';

/**
 * Hot question card. Tap-the-whole-card to expand. Always-visible 5px EC Red
 * left stripe per [visual-design-language.md §05 + §10] (5–6px solid stripe).
 *
 * Refinement turn: smaller padding, smaller title, no "people asked" meta,
 * stripe carries the brand identity instead of the hover/focus toggle.
 */
@Component({
    selector: 'app-hot-question-card',
    imports: [],
    template: `
        <button
            type="button"
            class="ec-hot-card"
            (click)="expand.emit(question())"
        >
            <span class="ec-hot-card__text">{{ question().text }}</span>
            <i class="pi pi-chevron-right ec-hot-card__chevron" aria-hidden="true"></i>
        </button>
    `,
    styles: `
        :host {
            display: block;
        }

        .ec-hot-card {
            display: flex;
            align-items: center;
            justify-content: space-between;
            gap: var(--space-3);
            width: 100%;
            padding: var(--space-3);
            background-color: var(--ec-auth-card-bg);
            color: var(--ec-auth-card-text);
            border: 0;
            border-left: 5px solid var(--ec-red);
            border-radius: var(--radius-none);
            text-align: left;
            cursor: pointer;
            box-shadow: var(--shadow-1);
            transition:
                box-shadow var(--duration-base) var(--ease-out),
                transform var(--duration-base) var(--ease-out);
        }

        .ec-hot-card:hover {
            box-shadow: var(--shadow-2);
        }

        .ec-hot-card:focus-visible {
            outline: none;
            box-shadow: var(--shadow-2), 0 0 0 3px var(--focus-ring-color);
        }

        .ec-hot-card:active {
            transform: translateY(1px);
        }

        .ec-hot-card__text {
            font-size: 15px;
            font-weight: 700;
            line-height: 1.3;
            color: var(--ec-auth-card-text);
            display: -webkit-box;
            -webkit-line-clamp: 2;
            -webkit-box-orient: vertical;
            overflow: hidden;
            flex: 1;
        }

        .ec-hot-card__chevron {
            color: var(--ec-gray-mid);
            font-size: 16px;
            flex-shrink: 0;
            transition: color var(--duration-fast) var(--ease-out);
        }

        .ec-hot-card:hover .ec-hot-card__chevron,
        .ec-hot-card:focus-visible .ec-hot-card__chevron {
            color: var(--ec-red);
        }
    `,
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class HotQuestionCardComponent {
    readonly question = input.required<HotQuestion>();
    readonly expand = output<HotQuestion>();
}
