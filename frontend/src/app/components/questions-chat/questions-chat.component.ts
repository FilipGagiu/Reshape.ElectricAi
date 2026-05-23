import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { TranslocoModule } from '@jsverse/transloco';

import { EcTopbarComponent } from '@shared/components/ec-topbar/ec-topbar.component';

import { ChatInputComponent } from './chat-input.component';
import { ConversationModalComponent } from './conversation-modal.component';
import { HotQuestionCardComponent } from './hot-question-card.component';
import { HotQuestionModalComponent } from './hot-question-modal.component';
import { Conversation, HotQuestion } from './models/question.model';
import { PastConversationRowComponent } from './past-conversation-row.component';
import { QuestionsService } from './services/questions.service';

/**
 * Questions page: top hot questions, middle past conversations, sticky chat
 * input at the bottom. Modals rendered at page level via signal toggles.
 *
 * Plan: .claude/plans/in-the-folder-frontend-fluttering-sunbeam.md
 */
@Component({
    selector: 'app-questions-chat',
    imports: [
        TranslocoModule,
        EcTopbarComponent,
        HotQuestionCardComponent,
        PastConversationRowComponent,
        ChatInputComponent,
        HotQuestionModalComponent,
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

    private readonly activeHotQuestionId = signal<string | null>(null);
    private readonly activeConversationId = signal<string | null>(null);

    protected readonly activeHotQuestion = computed<HotQuestion | null>(() => {
        const id = this.activeHotQuestionId();
        if (!id) return null;
        return this.hotQuestions().find((q) => q.id === id) ?? null;
    });

    protected readonly activeConversation = computed<Conversation | null>(() => {
        const id = this.activeConversationId();
        if (!id) return null;
        return this.conversations().find((c) => c.id === id) ?? null;
    });

    protected readonly hotModalOpen = computed(() => this.activeHotQuestion() !== null);
    protected readonly conversationModalOpen = computed(() => this.activeConversation() !== null);

    protected openHotQuestion(question: HotQuestion): void {
        this.activeHotQuestionId.set(question.id);
    }

    protected closeHotQuestion(): void {
        this.activeHotQuestionId.set(null);
    }

    protected openConversation(conversation: Conversation): void {
        this.activeConversationId.set(conversation.id);
    }

    protected closeConversation(): void {
        this.activeConversationId.set(null);
    }

    protected onAskFollowUp(_question: HotQuestion): void {
        // Close the modal. The sticky input is already visible at the bottom of
        // the page; the user picks up there. Prefill is intentionally out of v1.
        this.closeHotQuestion();
    }

    protected async onStickyInputSend(text: string): Promise<void> {
        const newConversationId = await this.questionsService.startNewConversation(text);
        if (newConversationId) {
            this.activeConversationId.set(newConversationId);
        }
    }
}
