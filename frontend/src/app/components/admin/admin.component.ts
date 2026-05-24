import { ChangeDetectionStrategy, Component } from '@angular/core';
import { TranslocoModule } from '@jsverse/transloco';

import { EcTopbarComponent } from '@shared/components/ec-topbar/ec-topbar.component';

import { PublishFeedFormComponent } from './publish-feed-form.component';

@Component({
    selector: 'app-admin',
    imports: [TranslocoModule, EcTopbarComponent, PublishFeedFormComponent],
    template: `
        <app-ec-topbar titleKey="admin.title" subtitleKey="admin.subtitle" />
        <section class="ec-admin" *transloco="let t">
            <h2 class="ec-admin__heading">{{ t('admin.publish.heading') }}</h2>
            <p class="ec-admin__hint">{{ t('admin.publish.hint') }}</p>
            <app-publish-feed-form />
        </section>
    `,
    styles: [
        `
            :host {
                display: block;
            }
            .ec-admin {
                padding: 16px;
                display: flex;
                flex-direction: column;
                gap: 12px;
            }
            .ec-admin__heading {
                font-size: 20px;
                font-weight: 700;
                margin: 0;
            }
            .ec-admin__hint {
                font-size: 13px;
                opacity: 0.6;
                margin: 0 0 8px;
            }
        `,
    ],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdminComponent {}
