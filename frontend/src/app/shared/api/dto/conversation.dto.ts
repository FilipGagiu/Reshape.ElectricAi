import { Category } from './feed.dto';

export type UserContext = Partial<Record<Category, ReadonlyArray<string>>>;

export interface ConversationRequest {
    readonly questionText: string;
    readonly userContext?: UserContext | null;
}

export interface ConversationResponse {
    readonly answer: string;
}
