import { ChangeDetectionStrategy, Component, inject } from '@angular/core';

import { TICKET_OPTIONS } from '../../onboarding.mock';
import { TicketType } from '../../onboarding.model';
import { OnboardingService } from '../../onboarding.service';
import { SelectableCardComponent } from '../../ui/selectable-card/selectable-card.component';

@Component({
    selector: 'app-step-ticket',
    imports: [SelectableCardComponent],
    templateUrl: './step-ticket.component.html',
    styleUrl: './step-ticket.component.css',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StepTicketComponent {
    private readonly onboarding = inject(OnboardingService);

    protected readonly options = TICKET_OPTIONS;
    protected readonly selectedTicket = this.onboarding.profile;

    protected select(value: TicketType): void {
        this.onboarding.patchProfile({ ticket: value });
    }
}
