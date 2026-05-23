import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { TranslocoModule } from '@jsverse/transloco';

import { OnboardingShellComponent } from '@components/onboarding/onboarding-shell.component';
import { OnboardingService } from '@components/onboarding/onboarding.service';

@Component({
    selector: 'app-my-ec-plan',
    imports: [TranslocoModule, OnboardingShellComponent],
    templateUrl: './my-ec-plan.component.html',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MyEcPlanComponent {
    protected readonly onboarding = inject(OnboardingService);
}
