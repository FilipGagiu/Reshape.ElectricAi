import { Category } from './feed.dto';

export type UserContext = Partial<Record<Category, ReadonlyArray<string>>>;

export type ConversationActor = 'User' | 'Bot';

export interface ConversationListItemDto {
    readonly id: string;
    readonly title: string;
    readonly createdUtc: string;
    readonly lastMessageUtc: string;
    readonly userMessageCount: number;
}

export interface ConversationReplyDto {
    readonly message: string;
    readonly actor: ConversationActor;
    readonly createdUtc: string;
}

export interface ConversationDetailDto {
    readonly id: string;
    readonly title: string;
    readonly createdUtc: string;
    readonly replies: ReadonlyArray<ConversationReplyDto>;
}

export interface CreateConversationRequest {
    readonly message: string;
    readonly userContext?: UserContext | null;
}

export interface CreateConversationResponse {
    readonly id: string;
    readonly title: string;
    readonly reply: ConversationReplyDto;
}

export interface ContinueConversationRequest {
    readonly message: string;
    readonly userContext?: UserContext | null;
}

export interface ContinueConversationResponse {
    readonly reply: ConversationReplyDto;
}
