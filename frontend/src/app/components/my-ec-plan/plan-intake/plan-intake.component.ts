import {
    ChangeDetectionStrategy,
    Component,
    ElementRef,
    Signal,
    computed,
    effect,
    inject,
    signal,
    viewChild,
} from '@angular/core';
import { TranslocoModule, TranslocoService } from '@jsverse/transloco';

import { ChatBubbleComponent } from '@components/questions-chat/chat-bubble.component';
import { ChatInputComponent, PrefillRequest } from '@components/questions-chat/chat-input.component';
import { ChatMessage } from '@components/questions-chat/models/question.model';

import { PlanIntakeQuestion, PlanIntakeTranscriptItem } from './models/plan-intake.model';
import { PlanIntakeService } from './services/plan-intake.service';

interface RenderedBubble {
    readonly id: string;
    readonly message: ChatMessage;
}

@Component({
    selector: 'app-plan-intake',
    imports: [TranslocoModule, ChatInputComponent, ChatBubbleComponent],
    templateUrl: './plan-intake.component.html',
    styleUrl: './plan-intake.component.css',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PlanIntakeComponent {
    private readonly transloco = inject(TranslocoService);
    protected readonly service = inject(PlanIntakeService);

    protected readonly activeLang = computed(() => this.transloco.getActiveLang());

    protected readonly bubbles: Signal<ReadonlyArray<RenderedBubble>> = computed(() => {
        const items = this.service.transcript();
        // Touch active lang so re-translation happens on lang switch.
        this.activeLang();
        return items.map((item) => ({
            id: item.id,
            message: this.toMessage(item),
        }));
    });

    protected readonly currentQuestion: Signal<PlanIntakeQuestion | null> =
        this.service.currentQuestion;

    protected readonly placeholderKey = computed(
        () => this.currentQuestion()?.placeholderKey ?? 'plan.intake.input.placeholder',
    );

    protected readonly isSubmitting = computed(() => this.service.status() === 'submitting');
    protected readonly hasError = computed(() => this.service.status() === 'error');
    protected readonly isAssistantTyping = this.service.isAssistantTyping;

    protected readonly inputDisabled = computed(() => this.service.status() !== 'collecting');

    protected readonly messagesRef = viewChild<ElementRef<HTMLDivElement>>('messages');

    private readonly prefillSignal = signal<PrefillRequest | null>(null);
    protected readonly prefill = this.prefillSignal.asReadonly();

    constructor() {
        effect(() => {
            // Touch transcript length + status so scroll fires on each new bubble.
            const _count = this.service.transcript().length;
            const _status = this.service.status();
            queueMicrotask(() => this.scrollToBottom());
        });
    }

    protected onSend(text: string): void {
        this.service.submitAnswer(text);
    }

    protected applyChip(chipKey: string): void {
        const translated = this.transloco.translate(chipKey);
        this.prefillSignal.set({ text: translated, nonce: Date.now() });
    }

    protected onRetry(): void {
        void this.service.retrySubmit();
    }

    private toMessage(item: PlanIntakeTranscriptItem): ChatMessage {
        const text =
            item.kind === 'i18n' ? this.transloco.translate(item.text) : item.text;
        return {
            id: item.id,
            role: item.role,
            text,
            createdAt: new Date(item.createdAt),
        };
    }

    private scrollToBottom(): void {
        window.scrollTo({ top: document.documentElement.scrollHeight, behavior: 'smooth' });
        const container = this.messagesRef()?.nativeElement;
        if (!container) return;
        container.scrollTop = container.scrollHeight;
    }
}
