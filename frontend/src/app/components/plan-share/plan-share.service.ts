import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';

import { ItineraryApi } from '@shared/api/itinerary-api';
import { ItineraryStore } from '@shared/api/itinerary-store';

import { itineraryToPlanData } from './itinerary-to-plan.mapper';
import { PlanData } from './plan-share.model';

/**
 * Fetches the personalised plan tied to the current JWT.
 *
 * Backend currently exposes `GET /api/v1/Itinerary` with no id parameter —
 * the plan is resolved from the bearer token. Once per-plan share UUIDs land
 * we add a `getById(uuid)` overload; for now `getCurrent()` covers both the
 * static plan view and the share/story view.
 */
@Injectable({ providedIn: 'root' })
export class PlanShareService {
    private readonly itineraryApi = inject(ItineraryApi);
    private readonly itineraryStore = inject(ItineraryStore);

    async getCurrent(): Promise<PlanData | null> {
        try {
            const response = await firstValueFrom(this.itineraryApi.getCurrent());
            if (!response) return null;
            this.itineraryStore.set(response);
            return itineraryToPlanData(response);
        } catch {
            return itineraryToPlanData(this.itineraryStore.itinerary());
        }
    }

    async getById(id: string): Promise<PlanData | null> {
        try {
            const response = await firstValueFrom(this.itineraryApi.getById(id));
            return itineraryToPlanData(response);
        } catch {
            return null;
        }
    }
}
