import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { API_BASE_URL, apiUrl } from './api-config';
import {
    ContinueConversationRequest,
    ContinueConversationResponse,
    ConversationDetailDto,
    ConversationListItemDto,
    CreateConversationRequest,
    CreateConversationResponse,
    HotQuestionDto,
} from './dto/conversations.dto';

@Injectable({ providedIn: 'root' })
export class ConversationsApi {
    private readonly http = inject(HttpClient);
    private readonly baseUrl = inject(API_BASE_URL);

    list(): Observable<ReadonlyArray<ConversationListItemDto>> {
        return this.http.get<ReadonlyArray<ConversationListItemDto>>(
            apiUrl(this.baseUrl, '/conversations'),
        );
    }

    hotQuestions(): Observable<ReadonlyArray<HotQuestionDto>> {
        return this.http.get<ReadonlyArray<HotQuestionDto>>(
            apiUrl(this.baseUrl, '/conversations/hot-questions'),
        );
    }

    get(id: string): Observable<ConversationDetailDto> {
        return this.http.get<ConversationDetailDto>(
            apiUrl(this.baseUrl, `/conversations/${encodeURIComponent(id)}`),
        );
    }

    create(payload: CreateConversationRequest): Observable<CreateConversationResponse> {
        return this.http.post<CreateConversationResponse>(
            apiUrl(this.baseUrl, '/conversations'),
            payload,
        );
    }

    continue(id: string, payload: ContinueConversationRequest): Observable<ContinueConversationResponse> {
        return this.http.post<ContinueConversationResponse>(
            apiUrl(this.baseUrl, `/conversations/${encodeURIComponent(id)}`),
            payload,
        );
    }
}
