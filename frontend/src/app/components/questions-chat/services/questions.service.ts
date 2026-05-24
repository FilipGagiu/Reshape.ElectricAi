import { Injectable, computed, effect, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';

import { ConversationsApi } from '@shared/api/conversations-api';
import {
    ConversationActor,
    ConversationListItemDto,
    ConversationReplyDto,
} from '@shared/api/dto/conversations.dto';
import { extractErrorEnvelope } from '@shared/api/error-envelope';
import { TokenStore } from '@shared/api/token-store';
import { AuthService } from '@shared/services/auth.service';

import { ChatMessage, ChatRole, Conversation, HotQuestion } from '../models/question.model';

export type LoadState = 'loading' | 'ready' | 'empty';

@Injectable({ providedIn: 'root' })
export class QuestionsService {
    private readonly conversationsApi = inject(ConversationsApi);
    private readonly auth = inject(AuthService);
    private readonly tokens = inject(TokenStore);

    private readonly hotQuestionsSignal = signal<readonly HotQuestion[]>([]);
    private readonly conversationsSignal = signal<readonly Conversation[]>([]);
    private readonly hydratedIds = signal<ReadonlySet<string>>(new Set());
    private readonly pendingConversationIdsSignal = signal<ReadonlySet<string>>(new Set());
    private readonly listLoaded = signal(false);

    readonly hotQuestions = computed(() => this.hotQuestionsSignal());
    readonly conversations = computed(() => this.conversationsSignal());

    readonly hotQuestionsState = computed<LoadState>(() =>
        this.hotQuestionsSignal().length ? 'ready' : 'empty',
    );

    readonly conversationsState = computed<LoadState>(() =>
        this.conversationsSignal().length ? 'ready' : 'empty',
    );

    constructor() {
        effect(() => {
            const user = this.tokens.user();
            const bypass = this.auth.isBypassActive();
            if (!user) {
                this.conversationsSignal.set([]);
                this.hydratedIds.set(new Set());
                this.listLoaded.set(false);
                return;
            }
            if (bypass) {
                this.listLoaded.set(true);
                return;
            }
            void this.fetchList();
        });
    }

    isPending(conversationId: string): boolean {
        return this.pendingConversationIdsSignal().has(conversationId);
    }

    getConversation(id: string): Conversation | undefined {
        return this.conversationsSignal().find((conv) => conv.id === id);
    }

    /**
     * Opening a conversation lazy-hydrates its replies from the BE on first view.
     * Bypass-mode conversations are local-only — no fetch.
     */
    async ensureHydrated(conversationId: string): Promise<void> {
        if (this.auth.isBypassActive()) return;
        if (this.hydratedIds().has(conversationId)) return;

        const beId = this.getConversation(conversationId)?.beId;
        if (!beId) return;

        try {
            const detail = await firstValueFrom(this.conversationsApi.get(beId));
            const messages = detail.replies.map((r) => toChatMessage(r));
            this.conversationsSignal.update((list) =>
                list.map((conv) =>
                    conv.id === conversationId
                        ? { ...conv, firstQuestion: detail.title, messages, updatedAt: latestDate(messages, conv.updatedAt) }
                        : conv,
                ),
            );
            this.hydratedIds.update((set) => new Set(set).add(conversationId));
        } catch (err) {
            console.warn('[questions] hydrate failed', extractErrorEnvelope(err));
        }
    }

    /**
     * Append a user message to an existing conversation and request a bot reply.
     * Bypass mode: append locally only, no BE call (and no bot reply).
     */
    async addUserMessage(conversationId: string, text: string): Promise<void> {
        const trimmed = text.trim();
        if (!trimmed) return;

        const userMessage: ChatMessage = makeMessage('user', trimmed);
        this.applyMessageToConversation(conversationId, userMessage);
        this.markPending(conversationId, true);

        if (this.auth.isBypassActive()) {
            this.markPending(conversationId, false);
            return;
        }

        const beId = this.getConversation(conversationId)?.beId;
        if (!beId) {
            this.markPending(conversationId, false);
            return;
        }

        try {
            const response = await firstValueFrom(
                this.conversationsApi.continue(beId, { message: trimmed }),
            );
            const botMessage = toChatMessage(response.reply);
            this.applyMessageToConversation(conversationId, botMessage);
        } catch (err) {
            const envelope = extractErrorEnvelope(err);
            const fallback: ChatMessage = makeMessage(
                'assistant',
                fallbackErrorReply(envelope.code),
            );
            this.applyMessageToConversation(conversationId, fallback);
        } finally {
            this.markPending(conversationId, false);
        }
    }

    /**
     * Create a new conversation. Returns the id synchronously so the modal opens
     * immediately; the bot reply is fetched in the background and appended.
     */
    startNewConversation(text: string): string | null {
        const trimmed = text.trim();
        if (!trimmed) return null;

        const localId = makeId('conv');
        const userMessage: ChatMessage = makeMessage('user', trimmed);
        const placeholder: Conversation = {
            id: localId,
            firstQuestion: trimmed,
            updatedAt: new Date(),
            messages: [userMessage],
        };

        this.conversationsSignal.update((list) => [placeholder, ...list]);
        this.markPending(localId, true);
        this.hydratedIds.update((set) => new Set(set).add(localId));

        if (this.auth.isBypassActive()) {
            this.markPending(localId, false);
            return localId;
        }

        void this.createOnServer(localId, trimmed);
        return localId;
    }

    private async createOnServer(localId: string, text: string): Promise<void> {
        try {
            const response = await firstValueFrom(
                this.conversationsApi.create({ message: text }),
            );
            this.assignBeId(localId, response.id, response.title);
            const botMessage = toChatMessage(response.reply);
            this.applyMessageToConversation(localId, botMessage);
        } catch (err) {
            const envelope = extractErrorEnvelope(err);
            const fallback: ChatMessage = makeMessage(
                'assistant',
                fallbackErrorReply(envelope.code),
            );
            this.applyMessageToConversation(localId, fallback);
        } finally {
            this.markPending(localId, false);
        }
    }

    private async fetchList(): Promise<void> {
        try {
            const list = await firstValueFrom(this.conversationsApi.list());
            this.conversationsSignal.set(list.map(toConversationSummary));
            this.hydratedIds.set(new Set());
            this.listLoaded.set(true);
        } catch (err) {
            console.warn('[questions] list failed', extractErrorEnvelope(err));
            this.conversationsSignal.set([]);
            this.listLoaded.set(true);
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

    private assignBeId(localId: string, beId: string, title: string): void {
        this.conversationsSignal.update((list) =>
            list.map((conv) =>
                conv.id === localId
                    ? { ...conv, beId, firstQuestion: title }
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
}

function toConversationSummary(item: ConversationListItemDto): Conversation {
    return {
        id: item.id,
        beId: item.id,
        firstQuestion: item.title,
        messages: [],
        updatedAt: new Date(item.lastMessageUtc),
    };
}

function toChatMessage(reply: ConversationReplyDto): ChatMessage {
    return {
        id: makeId('msg'),
        role: actorToRole(reply.actor),
        text: reply.message,
        createdAt: new Date(reply.createdUtc),
    };
}

function actorToRole(actor: ConversationActor): ChatRole {
    return actor === 'Bot' ? 'assistant' : 'user';
}

function makeMessage(role: ChatRole, text: string): ChatMessage {
    return {
        id: makeId('msg'),
        role,
        text,
        createdAt: new Date(),
    };
}

function makeId(prefix: string): string {
    return `${prefix}-${Date.now().toString(36)}-${Math.random().toString(36).slice(2, 8)}`;
}

function latestDate(messages: ReadonlyArray<ChatMessage>, fallback: Date): Date {
    if (messages.length === 0) return fallback;
    return messages.reduce((acc, m) => (m.createdAt > acc ? m.createdAt : acc), fallback);
}

function fallbackErrorReply(code: string): string {
    if (code === 'conversation-full') {
        return "We've hit the limit on this thread. Start a new conversation and we'll keep going.";
    }
    if (code === 'conversation-busy') {
        return "Still thinking about your last message. Try again in a moment.";
    }
    return `Sorry, I can't answer right now (${code}). Try again in a moment.`;
}
