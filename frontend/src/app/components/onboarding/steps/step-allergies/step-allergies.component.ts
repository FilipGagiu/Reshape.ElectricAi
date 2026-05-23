import { ChangeDetectionStrategy, Component, inject } from '@angular/core';

import { ALLERGY_OPTIONS } from '../../onboarding.mock';
import { OnboardingService } from '../../onboarding.service';
import { SelectableCardComponent } from '../../ui/selectable-card/selectable-card.component';

@Component({
    selector: 'app-step-allergies',
    imports: [SelectableCardComponent],
    templateUrl: './step-allergies.component.html',
    styleUrl: './step-allergies.component.css',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StepAllergiesComponent {
    private readonly onboarding = inject(OnboardingService);

    protected readonly options = ALLERGY_OPTIONS;
    protected readonly profile = this.onboarding.profile;

    protected toggle(id: string): void {
        this.onboarding.toggleArrayMembership('allergyIds', id);
    }
}
