import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { TranslocoModule } from '@jsverse/transloco';

import { LanguageSwitcherComponent } from '@i18n/language-switcher.component';

@Component({
    selector: 'app-ec-topbar',
    imports: [TranslocoModule, LanguageSwitcherComponent],
    templateUrl: './ec-topbar.component.html',
    styleUrl: './ec-topbar.component.css',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class EcTopbarComponent {
    readonly titleKey = input.required<string>();
    readonly subtitleKey = input.required<string>();
}
