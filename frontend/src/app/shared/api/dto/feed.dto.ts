import { MusicGenre } from '../enums';

export const CATEGORY_VALUES = [
    'General', 'Transport', 'Accommodation', 'Food', 'Music',
    'Lineup', 'Activity', 'Weather', 'Rules', 'Ticket',
    'Safety', 'Health',
] as const;
export type Category = (typeof CATEGORY_VALUES)[number];

export interface FeedEntryDto {
    readonly id: string;
    readonly title: string;
    readonly body: string;
    readonly primaryCategory: Category;
    readonly isGeneral: boolean;
    readonly targetArtists: ReadonlyArray<string>;
    readonly targetGenres: ReadonlyArray<MusicGenre>;
    readonly publishedUtc: string;
    readonly updatedUtc: string | null;
}

export type FeedSseKind = 'created' | 'updated' | 'deleted';

export interface FeedSseEvent {
    readonly kind: FeedSseKind;
    readonly eventId: string;
    readonly entry: FeedEntryDto;
}
