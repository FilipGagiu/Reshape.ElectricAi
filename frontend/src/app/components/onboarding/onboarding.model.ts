export enum TicketType {
    NoTicket = 'noTicket',
    FullPass = 'fullPass',
    DayTicket = 'dayTicket',
    Vip = 'vip',
    UltraVip = 'ultraVip',
    BlackTicket = 'blackTicket',
}

export enum Accommodation {
    FestivalCamping = 'festivalCamping',
    Bontida = 'bontida',
    Cluj = 'cluj',
}

export enum Transportation {
    Car = 'car',
    Train = 'train',
    Bus = 'bus',
    Bike = 'bike',
    OnFoot = 'onFoot',
}

export enum AgeGroup {
    Under18 = 'under18',
    Adult18To24 = 'adult18To24',
    Adult25To34 = 'adult25To34',
    Adult35To44 = 'adult35To44',
    Adult45Plus = 'adult45Plus',
}

export enum OnboardingMode {
    Individual = 'individual',
    Group = 'group',
}

export enum OnboardingStepId {
    Basics = 'basics',
    AgeGroup = 'ageGroup',
    Ticket = 'ticket',
    Accommodation = 'accommodation',
    Transport = 'transport',
    Artists = 'artists',
    Music = 'music',
    Cuisine = 'cuisine',
    Allergies = 'allergies',
    Activities = 'activities',
}

export interface OnboardingProfile {
    readonly name: string;
    readonly ageGroup: AgeGroup | null;
    readonly ticket: TicketType | null;
    readonly accommodation: Accommodation | null;
    readonly transportationIds: ReadonlyArray<string>;
    readonly artistIds: ReadonlyArray<string>;
    readonly genreIds: ReadonlyArray<string>;
    readonly cuisineIds: ReadonlyArray<string>;
    readonly allergyIds: ReadonlyArray<string>;
    readonly activityIds: ReadonlyArray<string>;
}

export interface OnboardingState {
    readonly mode: OnboardingMode;
    readonly profile: OnboardingProfile;
    readonly currentStep: OnboardingStepId;
    readonly completed: boolean;
    readonly updatedAt: string;
    readonly version: number;
}

export interface SelectOption {
    readonly id: string;
    readonly labelKey: string;
    readonly icon?: string;
}

export interface TypedSelectOption<TValue extends string> extends SelectOption {
    readonly value: TValue;
}

export const ONBOARDING_STATE_VERSION = 1;

export const EMPTY_ONBOARDING_PROFILE: OnboardingProfile = {
    name: '',
    ageGroup: null,
    ticket: null,
    accommodation: null,
    transportationIds: [],
    artistIds: [],
    genreIds: [],
    cuisineIds: [],
    allergyIds: [],
    activityIds: [],
};
