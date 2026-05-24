import {
    ChangeDetectionStrategy,
    Component,
    DestroyRef,
    computed,
    inject,
    signal,
} from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { TranslocoModule, TranslocoService } from '@jsverse/transloco';

import { EcTopbarComponent } from '@shared/components/ec-topbar/ec-topbar.component';
import { EcModalComponent } from '@shared/components/ec-modal/ec-modal.component';
import { TokenStore } from '@shared/api/token-store';

import { PublishFeedFormComponent } from '@components/admin/publish-feed-form.component';
import { FeedEntryDto } from '@shared/api/dto/feed.dto';

import {
    CATEGORY_META,
    CategoryMeta,
    FILTER_LABEL_KEY,
    FILTER_ORDER,
    FILTER_TO_CATEGORIES,
    PRIORITY_ORDER,
} from './live-feed.config';
import {
    FeedCategory,
    FeedEntry,
    FeedFilter,
    FeedUrgency,
    PinnedKind,
} from './live-feed.model';
import { LiveFeedService } from './live-feed.service';

interface FeedItemViewModel {
    readonly entry: FeedEntry;
    readonly meta: CategoryMeta;
    readonly relativeTime: string;
    readonly categoryLabel: string;
    readonly isExpanded: boolean;
}

interface PinnedViewModel {
    readonly entryId: string;
    readonly kind: PinnedKind;
    readonly meta: CategoryMeta;
    readonly eyebrowKey: string;
    readonly title: string;
    readonly subtitle: string;
    readonly ctaLabelKey: string;
}

const URGENT_CATEGORIES: ReadonlySet<FeedCategory> = new Set([
    FeedCategory.Safety,
    FeedCategory.Weather,
]);

// Heuristic so the Transport pin only fires on time-sensitive movement updates.
// Covers EN ("shuttle", "gate", "last") + RO ("navetă/naveta", "poart...", "ultim...").
const TRANSPORT_URGENT_KEYWORDS = /shuttle|gate|last|navet[ăa]|poart|ultim/i;

const PINNED_RECENT_WINDOW_MS = 60 * 60_000;
const TICK_INTERVAL_MS = 30_000;

@Component({
    selector: 'app-live-feed',
    templateUrl: './live-feed.component.html',
    styleUrl: './live-feed.component.css',
    imports: [TranslocoModule, EcTopbarComponent, EcModalComponent, PublishFeedFormComponent],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LiveFeedComponent {
    protected readonly FeedFilter = FeedFilter;
    protected readonly FeedUrgency = FeedUrgency;
    protected readonly PinnedKind = PinnedKind;
    protected readonly filterOrder = FILTER_ORDER;
    protected readonly filterLabelKey = FILTER_LABEL_KEY;

    private readonly feedService = inject(LiveFeedService);
    private readonly transloco = inject(TranslocoService);
    private readonly destroyRef = inject(DestroyRef);
    private readonly tokens = inject(TokenStore);

    protected readonly canPublish = computed(() => {
        const user = this.tokens.user();
        const result = user?.role === 'Organizer';
        console.warn('[live-feed][canPublish]', { user, role: user?.role, canPublish: result });
        return result;
    });

    private readonly tick = signal(new Date());
    private readonly activeLang = toSignal(this.transloco.langChanges$, {
        initialValue: this.transloco.getActiveLang(),
    });

    protected readonly selectedFilter = signal<FeedFilter>(FeedFilter.All);
    protected readonly expandedIds = signal<ReadonlySet<string>>(new Set());
    protected readonly isOnline = signal(true);
    protected readonly publishOpen = signal(false);

    protected openPublish(): void {
        console.warn('[live-feed][openPublish] CLICKED — before:', this.publishOpen());
        this.publishOpen.set(true);
        console.warn('[live-feed][openPublish] AFTER set(true):', this.publishOpen());
    }

    protected closePublish(): void {
        console.warn('[live-feed][closePublish]');
        this.publishOpen.set(false);
    }

    protected onPublished(entry: FeedEntryDto): void {
        console.warn('[live-feed][onPublished]', entry);
        this.feedService.upsertEntry(entry);
        this.publishOpen.set(false);
    }

    protected readonly items = computed<ReadonlyArray<FeedItemViewModel>>(() => {
        // Subscribe to active language so all derived strings re-translate on EN ↔ RO switch.
        void this.activeLang();
        const now = this.tick();
        const expanded = this.expandedIds();
        return this.feedService.feed().map((entry) => {
            const meta = CATEGORY_META[entry.category] ?? CATEGORY_META[FeedCategory.General];
            return {
                entry,
                meta,
                relativeTime: this.formatRelative(entry.publishedAt, now),
                categoryLabel: this.transloco.translate(meta.labelKey),
                isExpanded: expanded.has(entry.id),
            };
        });
    });

    protected readonly visibleItems = computed<ReadonlyArray<FeedItemViewModel>>(() => {
        const filter = this.selectedFilter();
        const allowed = FILTER_TO_CATEGORIES[filter];
        if (allowed.length === 0) {
            return this.items();
        }
        return this.items().filter((item) => allowed.includes(item.entry.category));
    });

    protected readonly pinned = computed<PinnedViewModel | null>(() => {
        const now = this.tick();
        const list = this.items();
        if (list.length === 0) {
            return null;
        }

        const urgentRecent = list
            .filter(
                (item) =>
                    URGENT_CATEGORIES.has(item.entry.category) &&
                    now.getTime() - item.entry.publishedAt.getTime() < PINNED_RECENT_WINDOW_MS,
            )
            .sort(
                (a, b) =>
                    PRIORITY_ORDER.indexOf(a.entry.category) -
                    PRIORITY_ORDER.indexOf(b.entry.category),
            );

        if (urgentRecent.length > 0) {
            const winner = urgentRecent[0];
            return this.buildPinned(winner, this.pinnedKindForCategory(winner.entry.category));
        }

        const transportUrgent = list.find(
            (item) =>
                item.entry.category === FeedCategory.Transport &&
                TRANSPORT_URGENT_KEYWORDS.test(item.entry.title),
        );
        if (transportUrgent) {
            return this.buildPinned(transportUrgent, PinnedKind.Transport);
        }

        return null;
    });

    protected readonly emptyStateKey = computed<string | null>(() => {
        const visible = this.visibleItems();
        if (visible.length > 0) {
            return null;
        }
        if (this.feedService.feed().length === 0) {
            return 'liveFeed.empty.firstLoad';
        }
        const filter = this.selectedFilter();
        if (filter === FeedFilter.Urgent) {
            return 'liveFeed.empty.urgent';
        }
        if (filter !== FeedFilter.All) {
            return 'liveFeed.empty.filtered';
        }
        return 'liveFeed.empty.personalized';
    });

    constructor() {
        console.warn('[live-feed] component constructed; user at construct:', this.tokens.user());
        const handle = setInterval(() => this.tick.set(new Date()), TICK_INTERVAL_MS);
        this.destroyRef.onDestroy(() => clearInterval(handle));
    }

    protected selectFilter(filter: FeedFilter): void {
        this.selectedFilter.set(filter);
    }

    protected toggleExpanded(id: string): void {
        this.expandedIds.update((current) => {
            const next = new Set(current);
            if (next.has(id)) {
                next.delete(id);
            } else {
                next.add(id);
            }
            return next;
        });
    }

    protected openPinned(): void {
        const target = this.pinned();
        if (!target) {
            return;
        }
        this.selectedFilter.set(FeedFilter.All);
        this.expandedIds.update((current) => {
            const next = new Set(current);
            next.add(target.entryId);
            return next;
        });
    }

    protected trackItem(_index: number, item: FeedItemViewModel): string {
        return item.entry.id;
    }

    protected trackFilter(_index: number, filter: FeedFilter): string {
        return filter;
    }

    private buildPinned(item: FeedItemViewModel, kind: PinnedKind): PinnedViewModel {
        const eyebrowKey = `liveFeed.pinned.eyebrow.${kind}`;
        const ctaLabelKey =
            kind === PinnedKind.Transport
                ? 'liveFeed.pinned.cta.getThere'
                : 'liveFeed.pinned.cta.seeDetails';
        return {
            entryId: item.entry.id,
            kind,
            meta: item.meta,
            eyebrowKey,
            title: item.entry.title,
            subtitle: `${item.categoryLabel} · ${item.relativeTime}`,
            ctaLabelKey,
        };
    }

    private pinnedKindForCategory(category: FeedCategory): PinnedKind {
        if (category === FeedCategory.Weather) {
            return PinnedKind.Weather;
        }
        if (category === FeedCategory.Safety) {
            return PinnedKind.Safety;
        }
        return PinnedKind.Artist;
    }

    private formatRelative(target: Date, now: Date): string {
        const diffMs = Math.max(0, now.getTime() - target.getTime());
        const minutes = Math.floor(diffMs / 60_000);
        if (minutes < 1) {
            return this.transloco.translate('liveFeed.time.justNow');
        }
        if (minutes < 60) {
            return this.transloco.translate('liveFeed.time.minutesAgo', { count: minutes });
        }
        const hours = Math.floor(minutes / 60);
        if (hours < 24) {
            return this.transloco.translate('liveFeed.time.hoursAgo', { count: hours });
        }
        const days = Math.floor(hours / 24);
        return this.transloco.translate('liveFeed.time.daysAgo', { count: days });
    }
}
