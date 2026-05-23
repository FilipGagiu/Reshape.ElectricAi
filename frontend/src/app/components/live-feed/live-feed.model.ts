export enum FeedCategory {
    General = 'General',
    Transport = 'Transport',
    Accommodation = 'Accommodation',
    Food = 'Food',
    Music = 'Music',
    Lineup = 'Lineup',
    Activity = 'Activity',
    Weather = 'Weather',
    Rules = 'Rules',
    Ticket = 'Ticket',
    Safety = 'Safety',
    Health = 'Health',
}

export enum FeedUrgency {
    Low = 'low',
    Medium = 'medium',
    High = 'high',
}

export interface FeedEntry {
    readonly id: string;
    readonly category: FeedCategory;
    readonly title: string;
    readonly body: string;
    readonly publishedAt: Date;
    readonly isGeneral: boolean;
    readonly targetArtists: ReadonlyArray<string>;
    readonly targetGenres: ReadonlyArray<string>;
}

export enum FeedFilter {
    All = 'all',
    Urgent = 'urgent',
    Schedule = 'schedule',
    GettingAround = 'gettingAround',
    General = 'general',
}

export enum PinnedKind {
    Safety = 'safety',
    Weather = 'weather',
    Transport = 'transport',
    Artist = 'artist',
}

export interface PinnedCapsule {
    readonly entryId: string;
    readonly kind: PinnedKind;
    readonly eyebrowKey: string;
    readonly title: string;
    readonly meta: string;
    readonly ctaLabelKey: string;
}

export interface FeedViewItem {
    readonly entry: FeedEntry;
    readonly relativeTime: string;
}
