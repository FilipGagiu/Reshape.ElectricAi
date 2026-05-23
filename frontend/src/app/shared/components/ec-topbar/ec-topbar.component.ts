import { ChangeDetectionStrategy, Component, inject, input } from '@angular/core';
import { TranslocoModule } from '@jsverse/transloco';

import { AuthService } from '@shared/services/auth.service';

@Component({
    selector: 'app-ec-topbar',
    imports: [TranslocoModule],
    templateUrl: './ec-topbar.component.html',
    styleUrl: './ec-topbar.component.css',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class EcTopbarComponent {
    readonly titleKey = input.required<string>();
    readonly subtitleKey = input.required<string>();

    private readonly authService = inject(AuthService);

    protected logout(): void {
        this.authService.logout();
        window.location.assign('/login');
    }
}
