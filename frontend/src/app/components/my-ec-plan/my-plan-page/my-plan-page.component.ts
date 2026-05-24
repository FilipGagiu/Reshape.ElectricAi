import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';

import { AuthService } from '@shared/services/auth.service';
import { PlanOnboardingService } from '@shared/services/plan-onboarding.service';

import { PlanIntakeService } from '../plan-intake/services/plan-intake.service';
import { PlanResultsComponent } from '../plan-results/plan-results.component';
import { PlanStepsComponent } from '../plan-steps/plan-steps.component';

@Component({
    selector: 'app-my-plan-page',
    imports: [PlanStepsComponent, PlanResultsComponent],
    template: `
        @if (showResults()) {
            <app-plan-results />
        } @else {
            <app-plan-steps />
        }
    `,
    styles: [
        `
            :host {
                display: flex;
                flex-direction: column;
                flex: 1;
                min-height: 100%;
            }
        `,
    ],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MyPlanPageComponent {
    private readonly auth = inject(AuthService);
    private readonly planOnboarding = inject(PlanOnboardingService);
    private readonly planIntake = inject(PlanIntakeService);

    protected readonly showResults = computed(() => {
        if (this.planOnboarding.isCompleted(this.auth.currentUser()?.email)) return true;
        // Route the user to the results view on submit failure so they see a
        // clear retry path instead of being stuck inside the wizard.
        return this.planIntake.status() === 'error';
    });
}
