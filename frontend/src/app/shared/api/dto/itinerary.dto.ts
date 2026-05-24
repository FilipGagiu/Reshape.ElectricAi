import { PreferencesDto } from './preferences.dto';

export interface WizardAnswer {
    readonly question: string;
    readonly answer: string | null;
    readonly answeredAt: string | null;
}

export interface ItineraryGenerationRequest {
    readonly version: number;
    readonly locale: string | null;
    readonly submittedAt: string;
    readonly answers: ReadonlyArray<WizardAnswer>;
    readonly freeText: string | null;
}

export interface ItineraryRefineRequest {
    readonly locale: string | null;
    readonly itineraryId: string;
    readonly freeText: string;
}

export interface ItinerarySectionDto {
    readonly key: string;
    readonly data: unknown;
    readonly diagnostic: string | null;
}

export interface ItineraryDto {
    readonly id: string;
    readonly generatedUtc: string;
    readonly sections: ReadonlyArray<ItinerarySectionDto>;
}

export interface ItineraryResponse {
    readonly preferences: PreferencesDto;
    readonly itinerary: ItineraryDto;
}

export interface RetrievedItem {
    readonly id: string;
    readonly score: number;
    readonly title: string;
    readonly snippet?: string;
    readonly eventUtc?: string;
}

export interface GreetingSectionData {
    readonly crew?: {
        readonly kind: string | null;
        readonly size: number | null;
    } | null;
    readonly name?: string | null;
    readonly origin?: string | null;
}

export interface TransportSectionData {
    readonly mode: string | null;
    readonly note: string | null;
}

export interface VibeActivitiesSectionData {
    readonly vibeTags: ReadonlyArray<string>;
    readonly topActivities: ReadonlyArray<RetrievedItem>;
}

export interface FoodSectionData {
    readonly restrictions: ReadonlyArray<string>;
    readonly topRestaurants: ReadonlyArray<RetrievedItem>;
    readonly preferredCuisines: ReadonlyArray<string>;
}

export interface DayArtists {
    readonly date: string;
    readonly artists: ReadonlyArray<RetrievedItem>;
}

export interface TopArtistsSectionData {
    readonly byDay: ReadonlyArray<DayArtists>;
    readonly topOverall: ReadonlyArray<RetrievedItem>;
}

export interface AccommodationSectionData {
    readonly type: string | null;
    readonly note: string | null;
}
