import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { API_BASE_URL, apiUrl } from './api-config';
import { Category, FeedEntryDto } from './dto/feed.dto';
import { MusicGenre } from './dto/preferences.dto';

export interface PublishFeedEntryRequest {
    readonly title: string;
    readonly body: string;
    readonly primaryCategory: Category;
    readonly isGeneral: boolean;
    readonly targetArtists: ReadonlyArray<string>;
    readonly targetGenres: ReadonlyArray<MusicGenre>;
}

@Injectable({ providedIn: 'root' })
export class FeedApi {
    private readonly http = inject(HttpClient);
    private readonly baseUrl = inject(API_BASE_URL);

    list(category?: Category): Observable<ReadonlyArray<FeedEntryDto>> {
        const params = category ? new HttpParams().set('category', category) : undefined;
        return this.http.get<ReadonlyArray<FeedEntryDto>>(
            apiUrl(this.baseUrl, '/feed'),
            params ? { params } : {},
        );
    }

    publish(payload: PublishFeedEntryRequest): Observable<FeedEntryDto> {
        return this.http.post<FeedEntryDto>(apiUrl(this.baseUrl, '/feed'), payload);
    }

    streamUrl(userId: string | null): string {
        const base = apiUrl(this.baseUrl, '/feed/stream');
        return userId ? `${base}?userId=${encodeURIComponent(userId)}` : base;
    }
}
