import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';

import { ConversationApi } from '@shared/api/conversation-api';
import { extractErrorEnvelope } from '@shared/api/error-envelope';

import { ChatMessage } from '../models/question.model';

@Injectable({ providedIn: 'root' })
export class ChatReplyService {
    private readonly conversationApi = inject(ConversationApi);

    async generateReply(
        _conversationId: string,
        messages: readonly ChatMessage[],
    ): Promise<string> {
        const lastUserMessage = [...messages].reverse().find((m) => m.role === 'user');
        const questionText = lastUserMessage?.text?.trim() ?? '';

        if (!questionText) {
            return 'Please type a question.';
        }

        try {
            const response = await firstValueFrom(this.conversationApi.ask({ questionText }));
            return response.answer;
        } catch (err) {
            const envelope = extractErrorEnvelope(err);
            console.warn('[chat-reply] ask failed', envelope);
            return `Sorry, I can't answer right now (${envelope.code}). Try again in a moment.`;
        }
    }
}
