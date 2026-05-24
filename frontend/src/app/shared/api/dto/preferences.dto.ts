export type {
    Accommodation,
    ActivityType,
    AgeGroup,
    CrewKind,
    Cuisine,
    FoodRestriction,
    MusicGenre,
    TicketType,
    TransportMode,
} from '../enums';

import type {
    Accommodation,
    ActivityType,
    AgeGroup,
    CrewKind,
    Cuisine,
    FoodRestriction,
    MusicGenre,
    TicketType,
    TransportMode,
} from '../enums';

export interface CrewDto {
    readonly kind: CrewKind;
    readonly estimatedSize: number | null;
}

export interface TransportSuggestionDto {
    readonly mode: TransportMode;
    readonly note: string | null;
}

export interface AccommodationSuggestionDto {
    readonly type: Accommodation;
    readonly note: string | null;
}

export interface PreferencesDto {
    readonly name: string | null;
    readonly origin: string | null;
    readonly crew: CrewDto | null;
    readonly vibeTags: ReadonlyArray<string>;
    readonly musicGenres: ReadonlyArray<MusicGenre>;
    readonly mustSeeArtists: ReadonlyArray<string>;
    readonly foodRestrictions: ReadonlyArray<FoodRestriction>;
    readonly cuisines: ReadonlyArray<Cuisine>;
    readonly activityInterests: ReadonlyArray<ActivityType>;
    readonly suggestedTransport: TransportSuggestionDto | null;
    readonly suggestedAccommodation: AccommodationSuggestionDto | null;
    readonly ticketType: TicketType | null;
    readonly ageGroup: AgeGroup | null;
    readonly completionPercent: number;
    readonly updatedUtc: string;
}

export interface PreferencesReplaceRequest {
    readonly name?: string | null;
    readonly origin?: string | null;
    readonly crew?: CrewDto | null;
    readonly vibeTags?: ReadonlyArray<string> | null;
    readonly musicGenres?: ReadonlyArray<MusicGenre> | null;
    readonly mustSeeArtists?: ReadonlyArray<string> | null;
    readonly foodRestrictions?: ReadonlyArray<FoodRestriction> | null;
    readonly cuisines?: ReadonlyArray<Cuisine> | null;
    readonly activityInterests?: ReadonlyArray<ActivityType> | null;
    readonly suggestedTransport?: TransportSuggestionDto | null;
    readonly suggestedAccommodation?: AccommodationSuggestionDto | null;
    readonly ticketType?: TicketType | null;
    readonly ageGroup?: AgeGroup | null;
}

export type PreferencesPatchRequest = PreferencesReplaceRequest;
