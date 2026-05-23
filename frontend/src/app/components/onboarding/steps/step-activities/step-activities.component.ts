import { ChangeDetectionStrategy, Component, inject } from '@angular/core';

import { ACTIVITY_OPTIONS } from '../../onboarding.mock';
import { OnboardingService } from '../../onboarding.service';
import { SelectableCardComponent } from '../../ui/selectable-card/selectable-card.component';

@Component({
    selector: 'app-step-activities',
    imports: [SelectableCardComponent],
    templateUrl: './step-activities.component.html',
    styleUrl: './step-activities.component.css',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StepActivitiesComponent {
    private readonly onboarding = inject(OnboardingService);

    protected readonly options = ACTIVITY_OPTIONS;
    protected readonly profile = this.onboarding.profile;

    protected toggle(id: string): void {
        this.onboarding.toggleArrayMembership('activityIds', id);
    }
}
