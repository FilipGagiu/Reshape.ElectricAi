import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { AlertLevel, FeedItem, FeedItemKind } from './live-feed.model';
import { LiveFeedService } from './live-feed.service';
import { formatClock, formatRelativeFuture, formatRelativePast } from './relative-time';
import { NotificationService } from '@shared/services/notification.service';

@Component({
    selector: 'app-live-feed',
    templateUrl: './live-feed.component.html',
    styleUrl: './live-feed.component.css',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LiveFeedComponent {
    protected readonly FeedItemKind = FeedItemKind;
    protected readonly AlertLevel = AlertLevel;

    private readonly feedService = inject(LiveFeedService);
    private readonly notifications = inject(NotificationService);
    private readonly tick = signal(new Date());

    protected readonly feed = this.feedService.feed;
    protected readonly now = computed(() => this.tick());
    protected readonly notificationPermission = this.notifications.permission;
    protected readonly demoArmed = signal(false);

    constructor() {
        // Re-render relative times every 30s. Cheap because the feed signal doesn't change.
        setInterval(() => this.tick.set(new Date()), 30_000);
    }

    protected pastLabel(date: Date): string {
        return formatRelativePast(date, this.now());
    }

    protected futureLabel(date: Date): string {
        return formatRelativeFuture(date, this.now());
    }

    protected clock(date: Date): string {
        return formatClock(date);
    }

    protected trackById(_index: number, item: FeedItem): string {
        return item.id;
    }

    protected async triggerDemoNotification(): Promise<void> {
        this.demoArmed.set(true);
        await this.notifications.showDemo({
            title: 'Boris Brejcha is on Main Stage now',
            body: 'You marked this set as a favorite. Head over before it ends at 23:30.',
            delayMs: 5_000,
        });
        setTimeout(() => this.demoArmed.set(false), 6_000);
    }
}
