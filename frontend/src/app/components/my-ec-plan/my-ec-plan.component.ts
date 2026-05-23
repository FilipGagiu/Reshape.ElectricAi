import { ChangeDetectionStrategy, Component } from '@angular/core';
import { TranslocoModule } from '@jsverse/transloco';

@Component({
    selector: 'app-my-ec-plan',
    imports: [TranslocoModule],
    template: `
        <section class="flex flex-col gap-4 p-4">
            <h1 class="text-surface-900 dark:text-surface-0 text-2xl font-bold tracking-tight">
                {{ 'plan.title' | transloco }}
            </h1>
            <p class="text-surface-600 dark:text-surface-300">{{ 'plan.placeholder' | transloco }}</p>
        </section>
    `,
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MyEcPlanComponent {}
