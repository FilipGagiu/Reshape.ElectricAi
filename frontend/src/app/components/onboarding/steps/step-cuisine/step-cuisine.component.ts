import { ChangeDetectionStrategy, Component, inject } from '@angular/core';

import { CUISINE_OPTIONS } from '../../onboarding.mock';
import { OnboardingService } from '../../onboarding.service';
import { SelectableCardComponent } from '../../ui/selectable-card/selectable-card.component';

@Component({
    selector: 'app-step-cuisine',
    imports: [SelectableCardComponent],
    templateUrl: './step-cuisine.component.html',
    styleUrl: './step-cuisine.component.css',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StepCuisineComponent {
    private readonly onboarding = inject(OnboardingService);

    protected readonly options = CUISINE_OPTIONS;
    protected readonly profile = this.onboarding.profile;

    protected toggle(id: string): void {
        this.onboarding.toggleArrayMembership('cuisineIds', id);
    }
}
