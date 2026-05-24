import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { TranslocoModule } from '@jsverse/transloco';

type BottomNavItem = {
    icon: string;
    labelKey: string;
    route: string;
};

@Component({
    selector: 'app-mobile-layout',
    imports: [RouterOutlet, RouterLink, RouterLinkActive, TranslocoModule],
    templateUrl: './mobile-layout.html',
    styleUrl: './mobile-layout.css',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MobileLayoutComponent {
    protected readonly navItems: ReadonlyArray<BottomNavItem> = [
        { icon: 'pi-bolt', labelKey: 'nav.liveFeed', route: '/' },
        { icon: 'pi-comments', labelKey: 'nav.questions', route: '/questions' },
        { icon: 'pi-list-check', labelKey: 'nav.plan', route: '/plan' },
    ];
}
