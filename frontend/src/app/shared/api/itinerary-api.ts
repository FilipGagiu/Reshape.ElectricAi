import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, of } from 'rxjs';

import { AuthService } from '@shared/services/auth.service';

import { API_BASE_URL, apiUrl } from './api-config';
import { ItineraryGenerationRequest, ItineraryResponse } from './dto/itinerary.dto';
import { MOCK_ITINERARY_RESPONSE } from './mock-itinerary';

@Injectable({ providedIn: 'root' })
export class ItineraryApi {
    private readonly http = inject(HttpClient);
    private readonly baseUrl = inject(API_BASE_URL);
    private readonly auth = inject(AuthService);

    generate(payload: ItineraryGenerationRequest): Observable<ItineraryResponse> {
        if (this.auth.isBypassActive()) {
            console.info('[itinerary-api] bypass mode — returning mock response for generate', payload);
            return of(this.mockResponse());
        }
        return this.http.post<ItineraryResponse>(
            apiUrl(this.baseUrl, '/Itinerary/generate'),
            payload,
        );
    }

    getCurrent(): Observable<ItineraryResponse> {
        if (this.auth.isBypassActive()) {
            return of(this.mockResponse());
        }
        return this.http.get<ItineraryResponse>(apiUrl(this.baseUrl, '/Itinerary'));
    }

    private mockResponse(): ItineraryResponse {
        return {
            ...MOCK_ITINERARY_RESPONSE,
            itinerary: {
                ...MOCK_ITINERARY_RESPONSE.itinerary,
                generatedUtc: new Date().toISOString(),
            },
            preferences: {
                ...MOCK_ITINERARY_RESPONSE.preferences,
                updatedUtc: new Date().toISOString(),
            },
        };
    }
}
