export type MusicGenre =
    | 'HipHop' | 'House' | 'Balkan' | 'Rock' | 'Folk'
    | 'Techno' | 'Pop' | 'Electronic' | 'Jazz' | 'Metal' | 'Other';

export type FoodRestriction =
    | 'Vegan' | 'Vegetarian' | 'NoPeanuts' | 'NoMeat' | 'NoPork'
    | 'NoDairy' | 'NoGluten' | 'NoShellfish' | 'NoEggs' | 'Halal' | 'Kosher';

export type ActivityType =
    | 'Relax' | 'Energetic' | 'Adrenaline' | 'Social'
    | 'Creative' | 'Wellness' | 'Discovery';

export type Cuisine =
    | 'American' | 'Italian' | 'Romanian' | 'Mexican' | 'Chinese'
    | 'Japanese' | 'Indian' | 'Thai' | 'French' | 'Greek'
    | 'Mediterranean' | 'MiddleEastern' | 'Bbq' | 'StreetFood' | 'Other';

export type TicketType = 'Standard' | 'Vip' | 'UltraVip' | 'Black';

export type Accommodation =
    | 'VillageRental' | 'Camping' | 'CarCamping' | 'RvCamping' | 'Glamping';

export type TransportMode = 'RideShare' | 'Car' | 'EcTrain' | 'EcBus' | 'Helicopter';

export type AgeGroup =
    | 'Under18' | 'Adult18To24' | 'Adult25To34' | 'Adult35To44' | 'Adult45Plus';

export type CrewKind = 'Solo' | 'WithGroup';

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
