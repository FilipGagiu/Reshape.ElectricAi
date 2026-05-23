import { ChangeDetectionStrategy, Component, computed, inject, input } from '@angular/core';
import { Router } from '@angular/router';
import { TranslocoModule } from '@jsverse/transloco';
import { ButtonModule } from 'primeng/button';

import { OnboardingStepId } from './onboarding.model';
import { OnboardingService } from './onboarding.service';
import { StepAccommodationComponent } from './steps/step-accommodation/step-accommodation.component';
import { StepActivitiesComponent } from './steps/step-activities/step-activities.component';
import { StepAllergiesComponent } from './steps/step-allergies/step-allergies.component';
import { StepArtistsComponent } from './steps/step-artists/step-artists.component';
import { StepBasicsComponent } from './steps/step-basics/step-basics.component';
import { StepCuisineComponent } from './steps/step-cuisine/step-cuisine.component';
import { StepMusicComponent } from './steps/step-music/step-music.component';
import { StepTicketComponent } from './steps/step-ticket/step-ticket.component';
import { StepTransportComponent } from './steps/step-transport/step-transport.component';
import {
    WizardProgressComponent,
    WizardProgressStep,
} from './ui/wizard-progress/wizard-progress.component';
import { WizardFooterComponent } from './ui/wizard-footer/wizard-footer.component';

@Component({
    selector: 'app-onboarding-shell',
    imports: [
        TranslocoModule,
        ButtonModule,
        WizardProgressComponent,
        WizardFooterComponent,
        StepBasicsComponent,
        StepTicketComponent,
        StepAccommodationComponent,
        StepTransportComponent,
        StepArtistsComponent,
        StepMusicComponent,
        StepCuisineComponent,
        StepAllergiesComponent,
        StepActivitiesComponent,
    ],
    templateUrl: './onboarding-shell.component.html',
    styleUrl: './onboarding-shell.component.css',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class OnboardingShellComponent {
    private readonly onboarding = inject(OnboardingService);
    private readonly router = inject(Router);

    readonly embedded = input<boolean>(false);

    protected readonly StepId = OnboardingStepId;
    protected readonly currentStep = this.onboarding.currentStep;
    protected readonly isFirstStep = this.onboarding.isFirstStep;
    protected readonly isLastStep = this.onboarding.isLastStep;
    protected readonly canProceed = this.onboarding.canProceed;
    protected readonly progress = this.onboarding.progress;
    protected readonly selectionCount = this.onboarding.currentSelectionCount;
    protected readonly canSkip = computed(() => !this.embedded());

    protected readonly progressSteps = computed<ReadonlyArray<WizardProgressStep>>(() => {
        const steps = this.onboarding.steps();
        const activeIndex = this.progress().index;
        return steps.map((_, index) => ({
            index,
            active: index === activeIndex,
            reachable: index <= activeIndex,
        }));
    });

    protected readonly stepTitleKey = computed(
        () => 'onboarding.step.' + this.currentStep() + '.title',
    );
    protected readonly stepSubtitleKey = computed(
        () => 'onboarding.step.' + this.currentStep() + '.subtitle',
    );

    protected jumpTo(index: number): void {
        const steps = this.onboarding.steps();
        if (index < 0 || index >= steps.length) {
            return;
        }
        this.onboarding.goTo(steps[index]);
    }

    protected back(): void {
        this.onboarding.previous();
    }

    protected proceed(): void {
        if (!this.canProceed()) {
            return;
        }
        if (this.isLastStep()) {
            this.onboarding.complete();
            if (!this.embedded()) {
                this.router.navigateByUrl('/');
            }
            return;
        }
        this.onboarding.next();
    }

    protected skip(): void {
        this.router.navigateByUrl('/');
    }

    protected resetOnboarding(): void {
        this.onboarding.reset();
    }
}
