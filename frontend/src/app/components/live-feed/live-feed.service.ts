import { Injectable, signal } from '@angular/core';
import { FeedCategory, FeedEntry } from './live-feed.model';

const minutesAgo = (minutes: number): Date => new Date(Date.now() - minutes * 60_000);

const MOCK_FEED: ReadonlyArray<FeedEntry> = [
    {
        id: 'music-bmth-now',
        category: FeedCategory.Music,
        title: 'Bring Me The Horizon on Main Stage',
        body: 'Set running 22:00 to 23:30. Head over before it fills up.',
        publishedAt: minutesAgo(3),
        isGeneral: false,
        targetArtists: ['Bring Me The Horizon'],
        targetGenres: ['Rock', 'Metal'],
    },
    {
        id: 'activity-silent-disco',
        category: FeedCategory.Activity,
        title: 'Silent disco at EC Village from midnight',
        body: 'Headphones at the gate. No queue if you arrive before 23:45.',
        publishedAt: minutesAgo(11),
        isGeneral: true,
        targetArtists: [],
        targetGenres: [],
    },
    {
        id: 'lineup-apashe-move',
        category: FeedCategory.Lineup,
        title: 'Apashe moved to Dance Arena',
        body: 'New time: 02:00. Stage reshuffle after a sound check delay.',
        publishedAt: minutesAgo(18),
        isGeneral: false,
        targetArtists: ['Apashe'],
        targetGenres: ['Electronic'],
    },
    {
        id: 'weather-rain-23-30',
        category: FeedCategory.Weather,
        title: 'Rain expected from 23:30',
        body: 'Storm clouds over Bonțida. Covered stages: Hangar, Booha, Camping Hub.',
        publishedAt: minutesAgo(24),
        isGeneral: true,
        targetArtists: [],
        targetGenres: [],
    },
    {
        id: 'food-vegan-stall',
        category: FeedCategory.Food,
        title: 'New vegan stall near the Hangar',
        body: 'Plant-based wraps and bowls. Open until 03:00. Worth the walk.',
        publishedAt: minutesAgo(35),
        isGeneral: true,
        targetArtists: [],
        targetGenres: [],
    },
    {
        id: 'health-water-refill',
        category: FeedCategory.Health,
        title: 'Free water refill points',
        body: 'New station at Forest Stage. Also at Camping Hub B. Stay hydrated.',
        publishedAt: minutesAgo(50),
        isGeneral: true,
        targetArtists: [],
        targetGenres: [],
    },
    {
        id: 'transport-shuttle-cluj',
        category: FeedCategory.Transport,
        title: 'Last shuttle to Cluj at 04:30',
        body: 'Leaves from the main gate. No shuttle after that until 07:00.',
        publishedAt: minutesAgo(65),
        isGeneral: true,
        targetArtists: [],
        targetGenres: [],
    },
    {
        id: 'safety-gate-3',
        category: FeedCategory.Safety,
        title: 'Gate 3 is closed until 01:00',
        body: 'Crowd management. Use Gate 1 or Gate 5 instead. Medical tent at the east gate stays open.',
        publishedAt: minutesAgo(85),
        isGeneral: true,
        targetArtists: [],
        targetGenres: [],
    },
    {
        id: 'ticket-rfid-topup',
        category: FeedCategory.Ticket,
        title: 'RFID top-up accepts card now',
        body: 'Top-up points near Main Stage and EC Village now accept card and cash.',
        publishedAt: minutesAgo(110),
        isGeneral: true,
        targetArtists: [],
        targetGenres: [],
    },
    {
        id: 'rules-no-glass',
        category: FeedCategory.Rules,
        title: 'No glass beyond the main arena',
        body: 'Plastic cups at every bar. Keep the magic safe.',
        publishedAt: minutesAgo(160),
        isGeneral: true,
        targetArtists: [],
        targetGenres: [],
    },
];

@Injectable({
    providedIn: 'root',
})
export class LiveFeedService {
    private readonly entries = signal<ReadonlyArray<FeedEntry>>(
        [...MOCK_FEED].sort((a, b) => b.publishedAt.getTime() - a.publishedAt.getTime()),
    );

    readonly feed = this.entries.asReadonly();
}
