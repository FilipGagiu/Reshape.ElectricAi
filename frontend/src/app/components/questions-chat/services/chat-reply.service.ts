import { Injectable } from '@angular/core';

import { ChatMessage } from '../models/question.model';

/**
 * Mocked backend reply. Resolves after ~2 seconds with a placeholder response.
 *
 * The real implementation will call the chat API and stream tokens.
 * The signature stays identical so the swap is mechanical.
 */
@Injectable({ providedIn: 'root' })
export class ChatReplyService {
    private readonly MOCK_DELAY_MS = 2000;

    generateReply(_conversationId: string, _messages: readonly ChatMessage[]): Promise<string> {
        return new Promise((resolve) => {
            setTimeout(() => {
                resolve(
                    "Mock reply. The chat backend isn't connected yet, " +
                        'so this is a placeholder so the UI can be tested end to end.',
                );
            }, this.MOCK_DELAY_MS);
        });
    }
}
