import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { API_BASE_URL, apiUrl } from './api-config';
import {
    PreferencesDto,
    PreferencesPatchRequest,
    PreferencesReplaceRequest,
} from './dto/preferences.dto';

@Injectable({ providedIn: 'root' })
export class PreferencesApi {
    private readonly http = inject(HttpClient);
    private readonly baseUrl = inject(API_BASE_URL);

    get(): Observable<PreferencesDto> {
        return this.http.get<PreferencesDto>(apiUrl(this.baseUrl, '/preferences'));
    }

    replace(payload: PreferencesReplaceRequest): Observable<PreferencesDto> {
        return this.http.put<PreferencesDto>(apiUrl(this.baseUrl, '/preferences'), payload);
    }

    patch(payload: PreferencesPatchRequest): Observable<PreferencesDto> {
        return this.http.patch<PreferencesDto>(apiUrl(this.baseUrl, '/preferences'), payload);
    }
}
