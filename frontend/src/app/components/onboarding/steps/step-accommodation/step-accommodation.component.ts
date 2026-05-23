import { ChangeDetectionStrategy, Component, inject } from '@angular/core';

import { ACCOMMODATION_OPTIONS } from '../../onboarding.mock';
import { Accommodation } from '../../onboarding.model';
import { OnboardingService } from '../../onboarding.service';
import { SelectableCardComponent } from '../../ui/selectable-card/selectable-card.component';

@Component({
    selector: 'app-step-accommodation',
    imports: [SelectableCardComponent],
    templateUrl: './step-accommodation.component.html',
    styleUrl: './step-accommodation.component.css',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StepAccommodationComponent {
    private readonly onboarding = inject(OnboardingService);

    protected readonly options = ACCOMMODATION_OPTIONS;
    protected readonly profile = this.onboarding.profile;

    protected select(value: Accommodation): void {
        this.onboarding.patchProfile({ accommodation: value });
    }
}
