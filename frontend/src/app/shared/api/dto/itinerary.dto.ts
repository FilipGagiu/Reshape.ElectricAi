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

export interface ItinerarySectionDto {
    readonly key: string;
    readonly data: unknown;
    readonly diagnostic: string | null;
}

export interface ItineraryDto {
    readonly generatedUtc: string;
    readonly sections: ReadonlyArray<ItinerarySectionDto>;
}

export interface ItineraryResponse {
    readonly preferences: PreferencesDto;
    readonly itinerary: ItineraryDto;
}
