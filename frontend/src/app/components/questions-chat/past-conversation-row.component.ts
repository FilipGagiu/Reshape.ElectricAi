import { ChangeDetectionStrategy, Component, computed, inject, input, output } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { TranslocoModule, TranslocoService } from '@jsverse/transloco';

import { Conversation } from './models/question.model';

/**
 * Past conversation card. Same visual chrome as the hot question card
 * (light card bg, --shadow-1, square corners, chevron-right) with an
 * always-visible 5px Youth Pass Blue left stripe so it reads as a
 * sibling pattern that's clearly distinct from hot questions.
 *
 * Visual spec: visual-design-language.md §05 + §10 + plan §"Past convo card".
 */
@Component({
    selector: 'app-past-conversation-row',
    imports: [TranslocoModule],
    template: `
        <button
            type="button"
            class="ec-convo-row"
            (click)="open.emit(conversation())"
        >
            <span class="ec-convo-row__body">
                <span class="ec-convo-row__title">{{ conversation().firstQuestion }}</span>
                <span class="ec-convo-row__meta">{{ relativeTime() }}</span>
            </span>
            <i class="pi pi-chevron-right ec-convo-row__chevron" aria-hidden="true"></i>
        </button>
    `,
    styles: `
        :host {
            display: block;
        }

        .ec-convo-row {
            display: flex;
            align-items: center;
            justify-content: space-between;
            gap: var(--space-3);
            width: 100%;
            padding: var(--space-3);
            background-color: var(--ec-auth-card-bg);
            color: var(--ec-auth-card-text);
            border: 0;
            border-left: 5px solid var(--ec-pass-youth);
            border-radius: var(--radius-none);
            text-align: left;
            cursor: pointer;
            box-shadow: var(--shadow-1);
            transition:
                box-shadow var(--duration-base) var(--ease-out),
                transform var(--duration-base) var(--ease-out);
        }

        .ec-convo-row:hover {
            box-shadow: var(--shadow-2);
        }

        .ec-convo-row:focus-visible {
            outline: none;
            box-shadow: var(--shadow-2), 0 0 0 3px var(--focus-ring-color);
        }

        .ec-convo-row:active {
            transform: translateY(1px);
        }

        .ec-convo-row__body {
            display: flex;
            flex-direction: column;
            gap: var(--space-1);
            min-width: 0;
            flex: 1;
        }

        .ec-convo-row__title {
            font-size: 15px;
            font-weight: 700;
            line-height: 1.3;
            color: var(--ec-auth-card-text);
            display: -webkit-box;
            -webkit-line-clamp: 2;
            -webkit-box-orient: vertical;
            overflow: hidden;
        }

        .ec-convo-row__meta {
            font-size: 12px;
            color: var(--ec-auth-card-muted);
        }

        .ec-convo-row__chevron {
            color: var(--ec-gray-mid);
            font-size: 16px;
            flex-shrink: 0;
            transition: color var(--duration-fast) var(--ease-out);
        }

        .ec-convo-row:hover .ec-convo-row__chevron,
        .ec-convo-row:focus-visible .ec-convo-row__chevron {
            color: var(--ec-pass-youth);
        }
    `,
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PastConversationRowComponent {
    private readonly transloco = inject(TranslocoService);
    private readonly activeLang = toSignal(this.transloco.langChanges$, {
        initialValue: this.transloco.getActiveLang(),
    });

    readonly conversation = input.required<Conversation>();
    readonly open = output<Conversation>();

    protected readonly relativeTime = computed(() => {
        const updated = this.conversation().updatedAt;
        const lang = this.activeLang();
        return this.formatRelative(updated, lang);
    });

    private formatRelative(from: Date, lang: string): string {
        const diffMs = Date.now() - from.getTime();
        const minutes = Math.floor(diffMs / 60_000);
        const hours = Math.floor(minutes / 60);
        const days = Math.floor(hours / 24);

        if (minutes < 1) {
            return this.transloco.translate('questions.time.justNow');
        }
        if (minutes < 60) {
            return this.transloco.translate('questions.time.minutesAgo', { count: minutes });
        }
        if (hours < 24) {
            return this.transloco.translate('questions.time.hoursAgo', { count: hours });
        }
        if (days < 7) {
            return this.transloco.translate('questions.time.daysAgo', { count: days });
        }

        return new Intl.DateTimeFormat(lang === 'ro' ? 'ro-RO' : 'en-GB', {
            day: 'numeric',
            month: 'short',
        }).format(from);
    }
}
