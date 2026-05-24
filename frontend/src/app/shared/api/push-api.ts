import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { API_BASE_URL, apiUrl } from './api-config';
import {
    SendRequest,
    SendResult,
    SubscribeRequest,
    UnsubscribeRequest,
    VapidPublicKeyResponse,
} from './dto/push.dto';

@Injectable({ providedIn: 'root' })
export class PushApi {
    private readonly http = inject(HttpClient);
    private readonly baseUrl = inject(API_BASE_URL);

    getPublicKey(): Observable<VapidPublicKeyResponse> {
        return this.http.get<VapidPublicKeyResponse>(apiUrl(this.baseUrl, '/push/public-key'));
    }

    subscribe(payload: SubscribeRequest): Observable<void> {
        return this.http.post<void>(apiUrl(this.baseUrl, '/push/subscribe'), payload);
    }

    unsubscribe(payload: UnsubscribeRequest): Observable<void> {
        return this.http.post<void>(apiUrl(this.baseUrl, '/push/unsubscribe'), payload);
    }

    send(payload: SendRequest): Observable<SendResult> {
        return this.http.post<SendResult>(apiUrl(this.baseUrl, '/push/send'), payload);
    }
}
