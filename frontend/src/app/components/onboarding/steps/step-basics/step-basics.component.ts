import { ChangeDetectionStrategy, Component, effect, inject, untracked } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { TranslocoModule } from '@jsverse/transloco';
import { InputTextModule } from 'primeng/inputtext';
import { auditTime } from 'rxjs';

import { OnboardingService } from '../../onboarding.service';

interface BasicsControls {
    name: FormControl<string>;
}

const PATCH_DEBOUNCE_MS = 200;

@Component({
    selector: 'app-step-basics',
    imports: [ReactiveFormsModule, TranslocoModule, InputTextModule],
    templateUrl: './step-basics.component.html',
    styleUrl: './step-basics.component.css',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StepBasicsComponent {
    private readonly onboarding = inject(OnboardingService);

    protected readonly form = new FormGroup<BasicsControls>({
        name: new FormControl('', {
            nonNullable: true,
            validators: [Validators.required],
        }),
    });

    constructor() {
        effect(() => {
            const profile = this.onboarding.profile();
            untracked(() => {
                this.form.setValue({ name: profile.name }, { emitEvent: false });
            });
        });

        this.form.valueChanges
            .pipe(auditTime(PATCH_DEBOUNCE_MS), takeUntilDestroyed())
            .subscribe(() => {
                const raw = this.form.getRawValue();
                this.onboarding.patchProfile({ name: raw.name });
            });
    }
}
