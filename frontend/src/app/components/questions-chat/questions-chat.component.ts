import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { TranslocoModule } from '@jsverse/transloco';

import { EcTopbarComponent } from '@shared/components/ec-topbar/ec-topbar.component';

import { ChatInputComponent } from './chat-input.component';
import { ConversationModalComponent } from './conversation-modal.component';
import { HotQuestionCardComponent } from './hot-question-card.component';
import { Conversation, HotQuestion } from './models/question.model';
import { PastConversationRowComponent } from './past-conversation-row.component';
import { QuestionsService } from './services/questions.service';

/**
 * Questions page: top hot questions, middle past conversations, sticky chat
 * input at the bottom. Hot-question tap = start a new conversation with the
 * question text. Conversation modal rendered at page level via signal toggle.
 */
@Component({
    selector: 'app-questions-chat',
    imports: [
        TranslocoModule,
        EcTopbarComponent,
        HotQuestionCardComponent,
        PastConversationRowComponent,
        ChatInputComponent,
        ConversationModalComponent,
    ],
    templateUrl: './questions-chat.component.html',
    styleUrl: './questions-chat.component.css',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class QuestionsChatComponent {
    private readonly questionsService = inject(QuestionsService);

    protected readonly hotQuestions = this.questionsService.hotQuestions;
    protected readonly conversations = this.questionsService.conversations;
    protected readonly hotQuestionsState = this.questionsService.hotQuestionsState;
    protected readonly conversationsState = this.questionsService.conversationsState;

    private readonly activeConversationId = signal<string | null>(null);

    protected readonly activeConversation = computed<Conversation | null>(() => {
        const id = this.activeConversationId();
        if (!id) return null;
        return this.conversations().find((c) => c.id === id) ?? null;
    });

    protected readonly conversationModalOpen = computed(() => this.activeConversation() !== null);

    protected async openHotQuestion(question: HotQuestion): Promise<void> {
        // If the user already started a conversation with this exact question,
        // re-open it instead of spawning a duplicate. Match is on the first
        // user message of past conversations (BE preserves it verbatim), not
        // the BE-generated title.
        const existingId = await this.questionsService.findConversationByFirstMessage(
            question.text,
        );
        if (existingId) {
            this.activeConversationId.set(existingId);
            return;
        }
        // Seed a local-only conversation with the canned Q+A — no BE call.
        // The conversation persists on BE only once the user sends a follow-up.
        const localId = this.questionsService.startHotQuestionConversation(question);
        this.activeConversationId.set(localId);
    }

    protected openConversation(conversation: Conversation): void {
        this.activeConversationId.set(conversation.id);
    }

    protected closeConversation(): void {
        this.activeConversationId.set(null);
    }

    protected onStickyInputSend(text: string): void {
        this.startConversation(text);
    }

    private startConversation(text: string): void {
        const newConversationId = this.questionsService.startNewConversation(text);
        if (newConversationId) {
            this.activeConversationId.set(newConversationId);
        }
    }
}
