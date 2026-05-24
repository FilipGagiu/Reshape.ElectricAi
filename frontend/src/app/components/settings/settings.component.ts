import { ChangeDetectionStrategy, Component, signal } from '@angular/core';
import { TranslocoModule } from '@jsverse/transloco';

import { EcTopbarComponent } from '@shared/components/ec-topbar/ec-topbar.component';

@Component({
    selector: 'app-settings',
    imports: [TranslocoModule, EcTopbarComponent],
    templateUrl: './settings.component.html',
    styleUrl: './settings.component.css',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SettingsComponent {
    protected readonly pushEnabled = signal<boolean>(true);

    protected togglePush(): void {
        this.pushEnabled.update((current) => !current);
    }

    protected openPrivacy(): void {
        // Privacy policy page not implemented yet.
    }
}
