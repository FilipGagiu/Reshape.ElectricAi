/**
 * Domain models for the Questions page.
 *
 * Backend not implemented yet. These shapes are the contract we'll honour
 * when the HTTP layer lands. See `services/mock-data.ts` for fixtures.
 */

export type ChatRole = 'user' | 'assistant';

export interface HotQuestion {
    readonly id: string;
    readonly text: string;
    readonly askedCount: number;
    readonly curatedAnswer: string;
    /** Position-1 freshest card receives the EC Red top border. */
    readonly isFreshest?: boolean;
}

export interface ChatMessage {
    readonly id: string;
    readonly role: ChatRole;
    readonly text: string;
    readonly createdAt: Date;
}

export interface Conversation {
    readonly id: string;
    readonly firstQuestion: string;
    readonly messages: readonly ChatMessage[];
    readonly updatedAt: Date;
}
