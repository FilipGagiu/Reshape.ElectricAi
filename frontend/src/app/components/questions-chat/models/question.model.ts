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
    /** Stable view-id for the lifetime of the conversation. */
    readonly id: string;
    /** BE-assigned id; absent until the conversation is persisted server-side. */
    readonly beId?: string;
    readonly firstQuestion: string;
    readonly messages: readonly ChatMessage[];
    readonly updatedAt: Date;
}
