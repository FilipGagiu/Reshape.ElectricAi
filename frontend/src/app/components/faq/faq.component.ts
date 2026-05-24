import { ChangeDetectionStrategy, Component } from '@angular/core';
import { TranslocoModule } from '@jsverse/transloco';

import { EcTopbarComponent } from '@shared/components/ec-topbar/ec-topbar.component';

@Component({
    selector: 'app-faq',
    imports: [TranslocoModule, EcTopbarComponent],
    template: `
        <app-ec-topbar titleKey="faq.title" subtitleKey="faq.subtitle" />
        <iframe
            class="ec-faq-frame"
            src="https://electriccastle.ro/faq?noHeader=true&noFooter=true"
            [title]="'faq.title' | transloco"
            loading="lazy"
            referrerpolicy="no-referrer"
        ></iframe>
    `,
    styles: [
        `
            :host {
                display: flex;
                flex-direction: column;
                flex: 1;
                min-height: 0;
            }
            .ec-faq-frame {
                flex: 1;
                border: 0;
                width: 100%;
                background: var(--ec-page-bg, #ffffff);
            }
        `,
    ],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FaqComponent {}
