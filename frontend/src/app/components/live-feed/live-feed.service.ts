import { Injectable, signal } from '@angular/core';
import { AlertLevel, FeedItem, FeedItemKind } from './live-feed.model';

const minutesAgo = (minutes: number): Date => new Date(Date.now() - minutes * 60_000);
const minutesAhead = (minutes: number): Date => new Date(Date.now() + minutes * 60_000);

const MOCK_FEED: ReadonlyArray<FeedItem> = [
    {
        id: 'now-1',
        kind: FeedItemKind.NowPlaying,
        artist: 'Bring Me The Horizon',
        stage: 'Main Stage',
        startsAt: minutesAgo(20),
        endsAt: minutesAhead(70),
        publishedAt: minutesAgo(20),
    },
    {
        id: 'alert-1',
        kind: FeedItemKind.Alert,
        level: AlertLevel.Warning,
        title: 'Rain expected ~23:30',
        body: 'Storm clouds over Bonțida. Covered areas: Hangar, Booha tent, Camping Hub.',
        icon: 'pi-cloud',
        publishedAt: minutesAgo(8),
    },
    {
        id: 'change-1',
        kind: FeedItemKind.ScheduleChange,
        artist: 'Apashe',
        newStage: 'Dance Arena',
        newTime: minutesAhead(180),
        reason: 'Stage reshuffle',
        publishedAt: minutesAgo(15),
    },
    {
        id: 'next-1',
        kind: FeedItemKind.UpNext,
        artist: 'Skepta',
        stage: 'Booha Stage',
        startsAt: minutesAhead(30),
        publishedAt: minutesAgo(45),
    },
    {
        id: 'photo-1',
        kind: FeedItemKind.Photo,
        author: 'EC Official',
        caption: 'Sunset crowd at The Garden 🌅',
        accent: 'from-orange-400 to-pink-500',
        publishedAt: minutesAgo(35),
    },
    {
        id: 'info-1',
        kind: FeedItemKind.Info,
        title: 'Free water refill points',
        body: 'New refill station opened next to Forest Stage. Bring your reusable bottle.',
        icon: 'pi-info-circle',
        publishedAt: minutesAgo(55),
    },
    {
        id: 'social-1',
        kind: FeedItemKind.Social,
        primaryUser: 'Maria',
        othersCount: 4,
        location: 'Hangar Stage',
        publishedAt: minutesAgo(12),
    },
    {
        id: 'alert-2',
        kind: FeedItemKind.Alert,
        level: AlertLevel.Info,
        title: 'Last shuttle to Cluj',
        body: 'Final shuttle bus leaves the main gate at 04:30. Schedule in the menu.',
        icon: 'pi-truck',
        publishedAt: minutesAgo(70),
    },
    {
        id: 'next-2',
        kind: FeedItemKind.UpNext,
        artist: 'Sub Focus',
        stage: 'Dance Arena',
        startsAt: minutesAhead(90),
        publishedAt: minutesAgo(60),
    },
    {
        id: 'photo-2',
        kind: FeedItemKind.Photo,
        author: 'Andrei R.',
        caption: 'Fireworks over the castle 🔥',
        accent: 'from-red-500 to-purple-700',
        publishedAt: minutesAgo(95),
    },
];

@Injectable({
    providedIn: 'root',
})
export class LiveFeedService {
    private readonly items = signal<ReadonlyArray<FeedItem>>(
        [...MOCK_FEED].sort((a, b) => b.publishedAt.getTime() - a.publishedAt.getTime()),
    );

    readonly feed = this.items.asReadonly();
}
