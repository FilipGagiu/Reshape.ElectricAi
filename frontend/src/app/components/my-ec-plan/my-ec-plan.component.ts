import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslocoModule } from '@jsverse/transloco';

import { OnboardingShellComponent } from '@components/onboarding/onboarding-shell.component';
import { OnboardingService } from '@components/onboarding/onboarding.service';
import { MOCK_PLAN_UUID } from '@components/plan-share/plan-share.model';

@Component({
    selector: 'app-my-ec-plan',
    imports: [TranslocoModule, RouterLink, OnboardingShellComponent],
    templateUrl: './my-ec-plan.component.html',
    styleUrl: './my-ec-plan.component.css',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MyEcPlanComponent {
    protected readonly onboarding = inject(OnboardingService);
    protected readonly recapPlanUrl = `/p/${MOCK_PLAN_UUID}`;
}
