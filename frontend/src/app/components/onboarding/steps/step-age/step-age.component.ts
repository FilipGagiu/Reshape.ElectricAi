import { ChangeDetectionStrategy, Component, inject } from '@angular/core';

import { AGE_GROUP_OPTIONS } from '../../onboarding.mock';
import { AgeGroup } from '../../onboarding.model';
import { OnboardingService } from '../../onboarding.service';
import { SelectableCardComponent } from '../../ui/selectable-card/selectable-card.component';

@Component({
    selector: 'app-step-age',
    imports: [SelectableCardComponent],
    templateUrl: './step-age.component.html',
    styleUrl: './step-age.component.css',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StepAgeComponent {
    private readonly onboarding = inject(OnboardingService);

    protected readonly options = AGE_GROUP_OPTIONS;
    protected readonly profile = this.onboarding.profile;

    protected select(value: AgeGroup): void {
        this.onboarding.patchProfile({ ageGroup: value });
    }
}
