import { Injectable, computed, signal } from '@angular/core';

import { ItineraryResponse } from './dto/itinerary.dto';

const STORAGE_KEY = 'ec-itinerary';

@Injectable({ providedIn: 'root' })
export class ItineraryStore {
    private readonly itinerarySignal = signal<ItineraryResponse | null>(this.load());

    readonly itinerary = this.itinerarySignal.asReadonly();
    readonly preferences = computed(() => this.itinerarySignal()?.preferences ?? null);
    readonly sections = computed(() => this.itinerarySignal()?.itinerary.sections ?? []);
    readonly hasItinerary = computed(() => this.itinerarySignal() !== null);

    set(response: ItineraryResponse): void {
        this.itinerarySignal.set(response);
        try {
            localStorage.setItem(STORAGE_KEY, JSON.stringify(response));
        } catch {
            // localStorage may be unavailable (private mode); swallow.
        }
    }

    clear(): void {
        this.itinerarySignal.set(null);
        try {
            localStorage.removeItem(STORAGE_KEY);
        } catch {
            // swallow.
        }
    }

    private load(): ItineraryResponse | null {
        try {
            const raw = localStorage.getItem(STORAGE_KEY);
            if (!raw) return null;
            const parsed = JSON.parse(raw) as ItineraryResponse;
            if (!parsed?.preferences || !parsed?.itinerary) return null;
            return parsed;
        } catch {
            return null;
        }
    }
}
