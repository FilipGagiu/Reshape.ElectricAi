import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';

export interface WizardProgressStep {
    readonly index: number;
    readonly active: boolean;
    readonly reachable: boolean;
}

@Component({
    selector: 'app-wizard-progress',
    templateUrl: './wizard-progress.component.html',
    styleUrl: './wizard-progress.component.css',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WizardProgressComponent {
    readonly steps = input.required<ReadonlyArray<WizardProgressStep>>();
    readonly select = output<number>();

    protected handleSelect(step: WizardProgressStep): void {
        if (!step.reachable) {
            return;
        }
        this.select.emit(step.index);
    }
}
