import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { API_BASE_URL, apiUrl } from './api-config';
import { ItineraryGenerationRequest, ItineraryResponse } from './dto/itinerary.dto';

@Injectable({ providedIn: 'root' })
export class ItineraryApi {
    private readonly http = inject(HttpClient);
    private readonly baseUrl = inject(API_BASE_URL);

    generate(payload: ItineraryGenerationRequest): Observable<ItineraryResponse> {
        return this.http.post<ItineraryResponse>(
            apiUrl(this.baseUrl, '/itinerary/generate'),
            payload,
        );
    }

    getCurrent(): Observable<ItineraryResponse> {
        return this.http.get<ItineraryResponse>(apiUrl(this.baseUrl, '/itinerary'));
    }
}
