import { ItineraryResponse } from '@shared/api/dto/itinerary.dto';
import { Accommodation, TransportMode } from '@shared/api/dto/preferences.dto';

import {
    AccommodationKind,
    MOCK_PLAN_UUID,
    PlanData,
    PlanSlide,
    TransportMethod,
} from './plan-share.model';

const TRANSPORT_FALLBACK: TransportMethod = 'car';
const ACCOMMODATION_FALLBACK: AccommodationKind = 'cluj';
const DEFAULT_NAME = 'friend';
const DEFAULT_ORIGIN = 'your city';
const DEFAULT_VIBE_KEY = 'curiousAndChill';

const TRANSPORT_MAP: Readonly<Record<TransportMode, TransportMethod>> = {
    RideShare: 'car',
    Car: 'car',
    EcTrain: 'train',
    EcBus: 'bus',
    Helicopter: 'car',
};

const ACCOMMODATION_MAP: Readonly<Record<Accommodation, AccommodationKind>> = {
    VillageRental: 'bontida',
    Camping: 'festivalCamping',
    CarCamping: 'festivalCamping',
    RvCamping: 'festivalCamping',
    Glamping: 'festivalCamping',
};

export function itineraryToPlanData(response: ItineraryResponse | null): PlanData | null {
    if (!response?.preferences) return null;

    const prefs = response.preferences;
    const name = prefs.name?.trim() || DEFAULT_NAME;
    const transportMode = prefs.suggestedTransport?.mode;
    const accommodationType = prefs.suggestedAccommodation?.type;
    const cleanedArtists = (prefs.mustSeeArtists ?? []).filter(
        (entry): entry is string => !!entry?.trim(),
    );
    const cleanedVibes = (prefs.vibeTags ?? []).filter(
        (entry): entry is string => !!entry?.trim(),
    );

    const slides: PlanSlide[] = [
        { type: 'welcome', name },
        {
            type: 'transport',
            method: transportMode ? TRANSPORT_MAP[transportMode] ?? TRANSPORT_FALLBACK : TRANSPORT_FALLBACK,
            from: prefs.origin?.trim() || DEFAULT_ORIGIN,
        },
        {
            type: 'sleep',
            accommodation: accommodationType
                ? ACCOMMODATION_MAP[accommodationType] ?? ACCOMMODATION_FALLBACK
                : ACCOMMODATION_FALLBACK,
        },
        {
            type: 'music',
            artists: cleanedArtists,
            genres: prefs.musicGenres ?? [],
        },
        {
            type: 'food',
            cuisines: prefs.cuisines ?? [],
            allergies: prefs.foodRestrictions ?? [],
        },
        {
            type: 'activityVibe',
            activityKeys: prefs.activityInterests ?? [],
            vibeKey: cleanedVibes[0] ?? DEFAULT_VIBE_KEY,
        },
        { type: 'share', uuid: MOCK_PLAN_UUID },
    ];

    return {
        uuid: MOCK_PLAN_UUID,
        ownerName: name,
        slides,
        createdAt: response.itinerary?.generatedUtc ?? new Date().toISOString(),
    };
}
