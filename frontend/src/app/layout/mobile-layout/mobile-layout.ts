import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { TranslocoModule } from '@jsverse/transloco';

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

    protected readonly navItems: ReadonlyArray<BottomNavItem> = [
        { icon: 'pi-bolt', labelKey: 'nav.liveFeed', route: '/' },
        { icon: 'pi-comments', labelKey: 'nav.questions', route: '/questions' },
        { icon: 'pi-list-check', labelKey: 'nav.plan', route: '/plan' },
    ];

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
