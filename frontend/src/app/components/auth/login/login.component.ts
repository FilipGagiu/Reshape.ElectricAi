import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { TranslocoModule } from '@jsverse/transloco';

import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { MessageModule } from 'primeng/message';
import { PasswordModule } from 'primeng/password';

import { AuthService } from '@shared/services/auth.service';

interface LoginFormControls {
    email: FormControl<string>;
    password: FormControl<string>;
}

@Component({
    selector: 'app-login',
    imports: [
        ReactiveFormsModule,
        RouterLink,
        TranslocoModule,
        ButtonModule,
        InputTextModule,
        MessageModule,
        PasswordModule,
    ],
    templateUrl: './login.component.html',
    styleUrl: './login.component.css',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LoginComponent {
    private readonly authService = inject(AuthService);
    private readonly router = inject(Router);

    protected readonly errorKey = signal<string | null>(null);
    protected readonly submitting = signal(false);

    protected readonly form = new FormGroup<LoginFormControls>({
        email: new FormControl('', {
            nonNullable: true,
            validators: [Validators.required, Validators.email],
        }),
        password: new FormControl('', {
            nonNullable: true,
            validators: [Validators.required],
        }),
    });

    protected async submit(): Promise<void> {
        if (this.form.invalid || this.submitting()) {
            this.form.markAllAsTouched();
            return;
        }

        this.submitting.set(true);
        this.errorKey.set(null);

        const { email, password } = this.form.getRawValue();
        const result = await this.authService.login(email, password);

        if (typeof result === 'string') {
            this.errorKey.set(result);
            this.submitting.set(false);
            return;
        }

        this.submitting.set(false);
        void this.router.navigateByUrl('/');
    }

    protected bypass(): void {
        this.authService.bypass();
        this.router.navigateByUrl('/');
    }
}
