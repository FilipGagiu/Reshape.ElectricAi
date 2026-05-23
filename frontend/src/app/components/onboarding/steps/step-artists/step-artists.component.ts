import { ChangeDetectionStrategy, Component, inject } from '@angular/core';

import { ARTIST_OPTIONS } from '../../onboarding.mock';
import { OnboardingService } from '../../onboarding.service';
import { SelectableCardComponent } from '../../ui/selectable-card/selectable-card.component';

@Component({
    selector: 'app-step-artists',
    imports: [SelectableCardComponent],
    templateUrl: './step-artists.component.html',
    styleUrl: './step-artists.component.css',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StepArtistsComponent {
    private readonly onboarding = inject(OnboardingService);

    protected readonly options = ARTIST_OPTIONS;
    protected readonly profile = this.onboarding.profile;

    protected toggle(id: string): void {
        this.onboarding.toggleArrayMembership('artistIds', id);
    }
}
