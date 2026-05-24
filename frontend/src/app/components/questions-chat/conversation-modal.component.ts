import {
    ChangeDetectionStrategy,
    Component,
    ElementRef,
    computed,
    effect,
    inject,
    input,
    output,
    signal,
    viewChild,
} from '@angular/core';
import { TranslocoModule } from '@jsverse/transloco';

import { EcModalComponent } from '@shared/components/ec-modal/ec-modal.component';

import { ChatBubbleComponent } from './chat-bubble.component';
import { ChatInputComponent } from './chat-input.component';
import { Conversation } from './models/question.model';
import { QuestionsService } from './services/questions.service';

/**
 * Full conversation view. Renders chat bubbles + a footer that flips between
 * the "Keep going" CTA and an inline chat input.
 *
 * Visual spec: visual-design-language.md §18 Modal + plan §"Conversation modal".
 */
@Component({
    selector: 'app-conversation-modal',
    imports: [EcModalComponent, ChatBubbleComponent, ChatInputComponent, TranslocoModule],
    templateUrl: './conversation-modal.component.html',
    styleUrl: './conversation-modal.component.css',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ConversationModalComponent {
    private readonly questionsService = inject(QuestionsService);

    readonly conversation = input<Conversation | null>(null);
    readonly open = input<boolean>(false);

    readonly close = output<void>();

    protected readonly titleId = 'conversation-modal-title';
    protected readonly inputRevealed = signal(false);

    protected readonly messagesRef = viewChild<ElementRef<HTMLDivElement>>('messages');

    protected readonly isPending = computed(() => {
        const conv = this.conversation();
        return conv ? this.questionsService.isPending(conv.id) : false;
    });

    constructor() {
        // Reset the input-revealed flag + hydrate replies whenever the modal opens
        // with a different conversation. Hydration is a no-op if already loaded
        // or if we're in bypass mode (local-only conversations).
        effect(() => {
            const conv = this.conversation();
            const opened = this.open();
            if (opened && conv) {
                this.inputRevealed.set(false);
                void this.questionsService.ensureHydrated(conv.id);
            }
        });

        // Auto-scroll to bottom on new message or when pending state changes.
        effect(() => {
            const conv = this.conversation();
            const _pending = this.isPending();
            if (!conv) return;
            // Touch messages length so the effect re-fires when bubbles arrive.
            const _count = conv.messages.length;
            queueMicrotask(() => this.scrollToBottom());
        });
    }

    protected onKeepGoing(): void {
        this.inputRevealed.set(true);
        queueMicrotask(() => this.scrollToBottom());
    }

    protected async onSend(text: string): Promise<void> {
        const conv = this.conversation();
        if (!conv) return;
        await this.questionsService.addUserMessage(conv.id, text);
    }

    private scrollToBottom(): void {
        const container = this.messagesRef()?.nativeElement;
        if (!container) return;
        container.scrollTop = container.scrollHeight;
    }
}
