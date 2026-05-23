import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

import { ChatMessage } from './models/question.model';

/**
 * Single chat bubble. Square corners, role-based alignment + colors.
 * Visual spec: visual-design-language.md §00 tokens + plan §"Chat bubbles".
 */
@Component({
    selector: 'app-chat-bubble',
    imports: [],
    template: `
        <div
            class="ec-bubble"
            [class.ec-bubble--user]="isUser()"
            [class.ec-bubble--assistant]="!isUser()"
        >
            <span class="ec-bubble__text">{{ message().text }}</span>
        </div>
    `,
    styles: `
        :host {
            display: flex;
            width: 100%;
        }

        :host(.ec-bubble-host--user) {
            justify-content: flex-end;
        }

        :host(.ec-bubble-host--assistant) {
            justify-content: flex-start;
        }

        .ec-bubble {
            max-width: 75%;
            padding: var(--space-2) var(--space-3);
            border-radius: var(--radius-none);
            font-size: 14px;
            line-height: 1.5;
            word-wrap: break-word;
            overflow-wrap: anywhere;
        }

        .ec-bubble--user {
            background-color: var(--ec-dark-navy);
            color: var(--ec-white);
            margin-left: auto;
        }

        .ec-bubble--assistant {
            background-color: var(--ec-gray-light);
            color: var(--ec-dark-navy);
            margin-right: auto;
        }

        .ec-bubble__text {
            white-space: pre-wrap;
        }
    `,
    host: {
        '[class.ec-bubble-host--user]': 'isUser()',
        '[class.ec-bubble-host--assistant]': '!isUser()',
    },
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ChatBubbleComponent {
    readonly message = input.required<ChatMessage>();

    protected readonly isUser = computed(() => this.message().role === 'user');
}
