import { Injectable, computed, inject, signal } from '@angular/core';

import { ChatMessage, Conversation, HotQuestion } from '../models/question.model';
import { ChatReplyService } from './chat-reply.service';
import { MOCK_CONVERSATIONS, MOCK_HOT_QUESTIONS } from './mock-data';

export type LoadState = 'loading' | 'ready' | 'empty';

/**
 * State container for the Questions page. Signals-only, OnPush-friendly.
 *
 * Hot questions are seeded from mock fixtures; future BE swap replaces the
 * loader with HttpClient. Past conversations live in memory only for v1;
 * a follow-up will add localStorage persistence.
 */
@Injectable({ providedIn: 'root' })
export class QuestionsService {
    private readonly chatReply = inject(ChatReplyService);

    private readonly hotQuestionsSignal = signal<readonly HotQuestion[]>(MOCK_HOT_QUESTIONS);
    private readonly conversationsSignal = signal<readonly Conversation[]>(MOCK_CONVERSATIONS);
    private readonly pendingConversationIdsSignal = signal<ReadonlySet<string>>(new Set());

    readonly hotQuestions = computed(() => this.hotQuestionsSignal());
    readonly conversations = computed(() => this.conversationsSignal());

    readonly hotQuestionsState = computed<LoadState>(() =>
        this.hotQuestionsSignal().length ? 'ready' : 'empty',
    );

    readonly conversationsState = computed<LoadState>(() =>
        this.conversationsSignal().length ? 'ready' : 'empty',
    );

    /** Returns true while a given conversation is waiting on a mocked reply. */
    isPending(conversationId: string): boolean {
        return this.pendingConversationIdsSignal().has(conversationId);
    }

    getConversation(id: string): Conversation | undefined {
        return this.conversationsSignal().find((conv) => conv.id === id);
    }

    /** Append a user message and trigger the mocked assistant reply. */
    async addUserMessage(conversationId: string, text: string): Promise<void> {
        const trimmed = text.trim();
        if (!trimmed) return;

        const userMessage: ChatMessage = {
            id: this.makeId('msg'),
            role: 'user',
            text: trimmed,
            createdAt: new Date(),
        };

        this.applyMessageToConversation(conversationId, userMessage);
        this.markPending(conversationId, true);

        const conversation = this.getConversation(conversationId);
        const replyText = await this.chatReply.generateReply(
            conversationId,
            conversation?.messages ?? [],
        );

        const assistantMessage: ChatMessage = {
            id: this.makeId('msg'),
            role: 'assistant',
            text: replyText,
            createdAt: new Date(),
        };

        this.applyMessageToConversation(conversationId, assistantMessage);
        this.markPending(conversationId, false);
    }

    /** Create a brand-new conversation from the sticky chat input. */
    async startNewConversation(text: string): Promise<string | null> {
        const trimmed = text.trim();
        if (!trimmed) return null;

        const newId = this.makeId('conv');
        const now = new Date();
        const firstMessage: ChatMessage = {
            id: this.makeId('msg'),
            role: 'user',
            text: trimmed,
            createdAt: now,
        };

        const newConversation: Conversation = {
            id: newId,
            firstQuestion: trimmed,
            updatedAt: now,
            messages: [firstMessage],
        };

        this.conversationsSignal.update((list) => [newConversation, ...list]);
        this.markPending(newId, true);

        const replyText = await this.chatReply.generateReply(newId, [firstMessage]);

        const assistantMessage: ChatMessage = {
            id: this.makeId('msg'),
            role: 'assistant',
            text: replyText,
            createdAt: new Date(),
        };

        this.applyMessageToConversation(newId, assistantMessage);
        this.markPending(newId, false);

        return newId;
    }

    private applyMessageToConversation(conversationId: string, message: ChatMessage): void {
        this.conversationsSignal.update((list) =>
            list.map((conv) =>
                conv.id === conversationId
                    ? {
                          ...conv,
                          messages: [...conv.messages, message],
                          updatedAt: new Date(),
                      }
                    : conv,
            ),
        );
    }

    private markPending(conversationId: string, pending: boolean): void {
        this.pendingConversationIdsSignal.update((set) => {
            const next = new Set(set);
            if (pending) next.add(conversationId);
            else next.delete(conversationId);
            return next;
        });
    }

    private makeId(prefix: string): string {
        return `${prefix}-${Date.now().toString(36)}-${Math.random().toString(36).slice(2, 8)}`;
    }
}
