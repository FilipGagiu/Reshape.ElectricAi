import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { NavigationEnd, Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { TranslocoModule } from '@jsverse/transloco';
import { filter, map, startWith } from 'rxjs';

import { AuthService } from '@shared/services/auth.service';

type BottomNavItem = {
    icon: string;
    labelKey: string;
    route: string;
};

type MoreMenuItem = {
    icon: string;
    labelKey: string;
    action: () => void;
};

const ALL_NAV_ITEMS: ReadonlyArray<BottomNavItem> = [
    { icon: 'pi-bolt', labelKey: 'nav.liveFeed', route: '/' },
    { icon: 'pi-comments', labelKey: 'nav.questions', route: '/questions' },
    { icon: 'pi-list-check', labelKey: 'nav.plan', route: '/plan' },
];

@Component({
    selector: 'app-mobile-layout',
    imports: [RouterOutlet, RouterLink, RouterLinkActive, TranslocoModule],
    templateUrl: './mobile-layout.html',
    styleUrl: './mobile-layout.css',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MobileLayoutComponent {
    private readonly router = inject(Router);
    private readonly authService = inject(AuthService);

    constructor() {
        // Refresh the cached user profile (and role) from BE on every authed-layout
        // mount so role changes made server-side propagate without a full re-login.
        void this.authService.refreshProfile();
    }

    private readonly currentUrl = toSignal(
        this.router.events.pipe(
            filter((event): event is NavigationEnd => event instanceof NavigationEnd),
            map((event) => event.urlAfterRedirects),
            startWith(this.router.url),
        ),
        { initialValue: this.router.url },
    );

    protected readonly navItems = computed<ReadonlyArray<BottomNavItem>>(() => {
        const onAdmin = this.currentUrl().startsWith('/admin');
        return onAdmin
            ? ALL_NAV_ITEMS.filter((item) => item.route !== '/plan')
            : ALL_NAV_ITEMS;
    });

    protected readonly moreOpen = signal(false);

    protected readonly moreItems: ReadonlyArray<MoreMenuItem> = [
        {
            icon: 'pi-question-circle',
            labelKey: 'more.faq',
            action: () => this.navigate('/faq'),
        },
        {
            icon: 'pi-cog',
            labelKey: 'more.settings',
            action: () => this.navigate('/settings'),
        },
        {
            icon: 'pi-sign-out',
            labelKey: 'more.logout',
            action: () => this.logout(),
        },
    ];

    protected toggleMore(): void {
        this.moreOpen.update((open) => !open);
    }

    protected closeMore(): void {
        this.moreOpen.set(false);
    }

    protected runMoreItem(item: MoreMenuItem): void {
        this.closeMore();
        item.action();
    }

    private navigate(route: string): void {
        void this.router.navigateByUrl(route);
    }

    private logout(): void {
        this.authService.logout();
        window.location.assign('/login');
    }
}
