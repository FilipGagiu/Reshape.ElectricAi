import { ChangeDetectionStrategy, Component, inject } from '@angular/core';

import { GENRE_OPTIONS } from '../../onboarding.mock';
import { OnboardingService } from '../../onboarding.service';
import { SelectableCardComponent } from '../../ui/selectable-card/selectable-card.component';

@Component({
    selector: 'app-step-music',
    imports: [SelectableCardComponent],
    templateUrl: './step-music.component.html',
    styleUrl: './step-music.component.css',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StepMusicComponent {
    private readonly onboarding = inject(OnboardingService);

    protected readonly options = GENRE_OPTIONS;
    protected readonly profile = this.onboarding.profile;

    protected toggle(id: string): void {
        this.onboarding.toggleArrayMembership('genreIds', id);
    }
}
