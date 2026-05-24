import { Injectable, computed, inject, signal } from '@angular/core';

import { ChatMessage, Conversation, HotQuestion } from '../models/question.model';
import { ChatReplyService } from './chat-reply.service';

export type LoadState = 'loading' | 'ready' | 'empty';

@Injectable({ providedIn: 'root' })
export class QuestionsService {
    private readonly chatReply = inject(ChatReplyService);

    private readonly hotQuestionsSignal = signal<readonly HotQuestion[]>([]);
    private readonly conversationsSignal = signal<readonly Conversation[]>([]);
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

    /**
     * Create a brand-new conversation synchronously. Returns the id immediately
     * so the caller can open the conversation UI without waiting for the AI
     * reply. The assistant reply is fetched in the background and appended
     * when it lands. `isPending(id)` is true while the reply is in flight.
     */
    startNewConversation(text: string): string | null {
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
        void this.requestReply(newId, [firstMessage]);

        return newId;
    }

    private async requestReply(
        conversationId: string,
        messages: readonly ChatMessage[],
    ): Promise<void> {
        try {
            const replyText = await this.chatReply.generateReply(conversationId, messages);
            const assistantMessage: ChatMessage = {
                id: this.makeId('msg'),
                role: 'assistant',
                text: replyText,
                createdAt: new Date(),
            };
            this.applyMessageToConversation(conversationId, assistantMessage);
        } finally {
            this.markPending(conversationId, false);
        }
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
