import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, of } from 'rxjs';
import { map } from 'rxjs/operators';

import { AuthService } from '@shared/services/auth.service';

import { API_BASE_URL, apiUrl } from './api-config';
import { TopArtistsResponse } from './dto/top-artists.dto';

@Injectable({ providedIn: 'root' })
export class TopArtistsApi {
    private readonly http = inject(HttpClient);
    private readonly baseUrl = inject(API_BASE_URL);
    private readonly auth = inject(AuthService);

    getTopArtists(): Observable<ReadonlyArray<string>> {
        if (this.auth.isBypassActive()) {
            return of([]);
        }
        return this.http
            .get<TopArtistsResponse>(apiUrl(this.baseUrl, '/top-artists'))
            .pipe(map((response) => response?.artists ?? []));
    }
}
