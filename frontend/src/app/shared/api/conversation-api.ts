import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { API_BASE_URL, apiUrl } from './api-config';
import { ConversationRequest, ConversationResponse } from './dto/conversation.dto';

@Injectable({ providedIn: 'root' })
export class ConversationApi {
    private readonly http = inject(HttpClient);
    private readonly baseUrl = inject(API_BASE_URL);

    ask(payload: ConversationRequest): Observable<ConversationResponse> {
        return this.http.post<ConversationResponse>(
            apiUrl(this.baseUrl, '/conversation'),
            payload,
        );
    }
}
