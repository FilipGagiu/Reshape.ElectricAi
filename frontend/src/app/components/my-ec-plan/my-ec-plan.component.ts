import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { TranslocoModule } from '@jsverse/transloco';

import { EcTopbarComponent } from '@shared/components/ec-topbar/ec-topbar.component';

import { PlanIntakeComponent } from './plan-intake/plan-intake.component';
import { PlanIntakeService } from './plan-intake/services/plan-intake.service';

@Component({
    selector: 'app-my-ec-plan',
    imports: [TranslocoModule, EcTopbarComponent, PlanIntakeComponent],
    templateUrl: './my-ec-plan.component.html',
    styleUrl: './my-ec-plan.component.css',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MyEcPlanComponent {
    protected readonly intake = inject(PlanIntakeService);
}
