import { ChangeDetectionStrategy, Component, inject } from '@angular/core';

import { TRANSPORTATION_OPTIONS } from '../../onboarding.mock';
import { OnboardingService } from '../../onboarding.service';
import { SelectableCardComponent } from '../../ui/selectable-card/selectable-card.component';

@Component({
    selector: 'app-step-transport',
    imports: [SelectableCardComponent],
    templateUrl: './step-transport.component.html',
    styleUrl: './step-transport.component.css',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StepTransportComponent {
    private readonly onboarding = inject(OnboardingService);

    protected readonly options = TRANSPORTATION_OPTIONS;
    protected readonly profile = this.onboarding.profile;

    protected toggle(id: string): void {
        this.onboarding.toggleArrayMembership('transportationIds', id);
    }
}
