import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { TranslocoModule } from '@jsverse/transloco';
import { ButtonModule } from 'primeng/button';

@Component({
    selector: 'app-wizard-footer',
    imports: [ButtonModule, TranslocoModule],
    templateUrl: './wizard-footer.component.html',
    styleUrl: './wizard-footer.component.css',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WizardFooterComponent {
    readonly isLastStep = input<boolean>(false);
    readonly canProceed = input<boolean>(true);
    readonly selectionCount = input<number | null>(null);

    readonly proceed = output<void>();

    protected readonly hasCounter = computed(() => this.selectionCount() !== null);
    protected readonly counterKey = computed(() =>
        (this.selectionCount() ?? 0) > 0
            ? 'onboarding.action.itemsSelected'
            : 'onboarding.action.noneSelected',
    );
    protected readonly counterParams = computed(() => ({ count: this.selectionCount() ?? 0 }));
    protected readonly labelKey = computed(() =>
        this.isLastStep() ? 'onboarding.action.finish' : 'onboarding.action.continue',
    );
    protected readonly disabled = computed(() => !this.canProceed());
}
