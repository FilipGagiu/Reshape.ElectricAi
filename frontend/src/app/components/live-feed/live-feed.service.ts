import { DestroyRef, Injectable, computed, effect, inject, signal } from '@angular/core';

import { FeedApi } from '@shared/api/feed-api';
import { FeedEntryDto } from '@shared/api/dto/feed.dto';
import { TokenStore } from '@shared/api/token-store';
import { AuthService } from '@shared/services/auth.service';

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

const SSE_RECONNECT_DELAY_MS = 5000;

@Injectable({ providedIn: 'root' })
export class LiveFeedService {
    private readonly feedApi = inject(FeedApi);
    private readonly auth = inject(AuthService);
    private readonly tokens = inject(TokenStore);
    private readonly destroyRef = inject(DestroyRef);

    private readonly entriesSignal = signal<ReadonlyArray<FeedEntry>>([]);
    private readonly isUsingMockSignal = signal(false);
    private eventSource: EventSource | null = null;
    private reconnectHandle: ReturnType<typeof setTimeout> | null = null;

    readonly feed = this.entriesSignal.asReadonly();
    readonly isUsingMock = this.isUsingMockSignal.asReadonly();
    readonly isConnected = computed(() => !this.isUsingMockSignal() && this.eventSource !== null);

    /**
     * Insert (or replace) a freshly-published entry. Used to optimistically reflect
     * a successful POST /feed before the SSE broadcast lands. The SSE handler dedupes
     * by id, so any later replay is a no-op.
     */
    upsertEntry(dto: FeedEntryDto): void {
        const entry = toFeedEntry(dto);
        this.entriesSignal.update((current) =>
            [entry, ...current.filter((item) => item.id !== entry.id)].sort(byPublishedDesc),
        );
    }

    constructor() {
        // React to session changes: load fresh on login, switch to mock on bypass, clear on logout.
        effect(() => {
            const user = this.tokens.user();
            const bypass = this.auth.isBypassActive();
            this.closeStream();
            if (!user) {
                this.entriesSignal.set([]);
                this.isUsingMockSignal.set(false);
                return;
            }
            if (bypass) {
                this.useMock();
                return;
            }
            void this.loadAndStream(user.id);
        });

        this.destroyRef.onDestroy(() => this.closeStream());
    }

    private async loadAndStream(userId: string): Promise<void> {
        try {
            const entries = await this.fetchList();
            this.entriesSignal.set(entries.map(toFeedEntry).sort(byPublishedDesc));
            this.isUsingMockSignal.set(false);
            this.openStream(userId);
        } catch (err) {
            console.warn('[live-feed] fetch failed, falling back to mock', err);
            this.useMock();
        }
    }

    private async fetchList(): Promise<ReadonlyArray<FeedEntryDto>> {
        const observable = this.feedApi.list();
        return await new Promise((resolve, reject) => {
            const sub = observable.subscribe({
                next: (value) => {
                    resolve(value);
                    sub.unsubscribe();
                },
                error: (err) => {
                    reject(err);
                    sub.unsubscribe();
                },
            });
        });
    }

    private openStream(userId: string): void {
        this.closeStream();
        const url = this.feedApi.streamUrl(userId);
        const source = new EventSource(url);
        this.eventSource = source;

        source.addEventListener('feed.created', (event) => this.handleCreated(event));
        source.addEventListener('feed.updated', (event) => this.handleUpdated(event));
        source.addEventListener('feed.deleted', (event) => this.handleDeleted(event));
        source.onerror = () => this.scheduleReconnect(userId);
    }

    private handleCreated(event: MessageEvent): void {
        const dto = parseEntry(event.data);
        if (!dto) return;
        const entry = toFeedEntry(dto);
        this.entriesSignal.update((current) =>
            [entry, ...current.filter((item) => item.id !== entry.id)].sort(byPublishedDesc),
        );
    }

    private handleUpdated(event: MessageEvent): void {
        const dto = parseEntry(event.data);
        if (!dto) return;
        const entry = toFeedEntry(dto);
        this.entriesSignal.update((current) =>
            current.map((item) => (item.id === entry.id ? entry : item)).sort(byPublishedDesc),
        );
    }

    private handleDeleted(event: MessageEvent): void {
        const dto = parseEntry(event.data);
        if (!dto) return;
        this.entriesSignal.update((current) => current.filter((item) => item.id !== dto.id));
    }

    private scheduleReconnect(userId: string): void {
        if (this.reconnectHandle !== null) return;
        this.reconnectHandle = setTimeout(() => {
            this.reconnectHandle = null;
            this.openStream(userId);
        }, SSE_RECONNECT_DELAY_MS);
    }

    private closeStream(): void {
        if (this.eventSource) {
            this.eventSource.close();
            this.eventSource = null;
        }
        if (this.reconnectHandle !== null) {
            clearTimeout(this.reconnectHandle);
            this.reconnectHandle = null;
        }
    }

    private useMock(): void {
        this.entriesSignal.set([...MOCK_FEED].sort(byPublishedDesc));
        this.isUsingMockSignal.set(true);
    }
}

/** Lower-cased lookup so BE values in any casing (e.g. 'activity', 'WEATHER') map cleanly. */
const FEED_CATEGORY_BY_LOWER: ReadonlyMap<string, FeedCategory> = new Map(
    (Object.values(FeedCategory) as FeedCategory[]).map((value) => [value.toLowerCase(), value]),
);

function normalizeFeedCategory(raw: string | null | undefined, entryId: string): FeedCategory {
    const lowered = raw?.toLowerCase();
    const match = lowered ? FEED_CATEGORY_BY_LOWER.get(lowered) : undefined;
    if (match) return match;
    console.warn('[live-feed] unknown category, falling back to General', { id: entryId, category: raw });
    return FeedCategory.General;
}

function toFeedEntry(dto: FeedEntryDto): FeedEntry {
    return {
        id: dto.id,
        category: normalizeFeedCategory(dto.primaryCategory, dto.id),
        title: dto.title,
        body: dto.body,
        publishedAt: new Date(dto.publishedUtc),
        isGeneral: dto.isGeneral,
        targetArtists: dto.targetArtists,
        targetGenres: dto.targetGenres,
    };
}

function byPublishedDesc(a: FeedEntry, b: FeedEntry): number {
    return b.publishedAt.getTime() - a.publishedAt.getTime();
}

function parseEntry(raw: string): FeedEntryDto | null {
    try {
        return JSON.parse(raw) as FeedEntryDto;
    } catch {
        return null;
    }
}
