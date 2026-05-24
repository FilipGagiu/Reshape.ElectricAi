import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import {
    AbstractControl,
    FormControl,
    FormGroup,
    ReactiveFormsModule,
    ValidationErrors,
    Validators,
} from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { TranslocoModule } from '@jsverse/transloco';

import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { MessageModule } from 'primeng/message';
import { PasswordModule } from 'primeng/password';

import { AuthError, AuthService } from '@shared/services/auth.service';

interface RegisterFormControls {
    email: FormControl<string>;
    password: FormControl<string>;
    confirmPassword: FormControl<string>;
}

const PASSWORD_MIN_LENGTH = 8;

function passwordsMatchValidator(group: AbstractControl): ValidationErrors | null {
    const password = group.get('password')?.value;
    const confirm = group.get('confirmPassword')?.value;
    return password === confirm ? null : { passwordsMismatch: true };
}

@Component({
    selector: 'app-register',
    imports: [
        ReactiveFormsModule,
        RouterLink,
        TranslocoModule,
        ButtonModule,
        InputTextModule,
        MessageModule,
        PasswordModule,
    ],
    templateUrl: './register.component.html',
    styleUrl: './register.component.css',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RegisterComponent {
    private readonly authService = inject(AuthService);
    private readonly router = inject(Router);

    protected readonly errorKey = signal<string | null>(null);
    protected readonly submitting = signal(false);
    protected readonly passwordMinLength = PASSWORD_MIN_LENGTH;

    protected readonly form = new FormGroup<RegisterFormControls>(
        {
            email: new FormControl('', {
                nonNullable: true,
                validators: [Validators.required, Validators.email],
            }),
            password: new FormControl('', {
                nonNullable: true,
                validators: [Validators.required, Validators.minLength(PASSWORD_MIN_LENGTH)],
            }),
            confirmPassword: new FormControl('', {
                nonNullable: true,
                validators: [Validators.required],
            }),
        },
        { validators: passwordsMatchValidator },
    );

    protected async submit(): Promise<void> {
        if (this.form.invalid || this.submitting()) {
            this.form.markAllAsTouched();
            return;
        }

        this.submitting.set(true);
        this.errorKey.set(null);

        try {
            const { email, password } = this.form.getRawValue();
            const result = await this.authService.register(email, password);

            if (this.isAuthErrorValue(result)) {
                this.errorKey.set(result);
                return;
            }

            await this.router.navigateByUrl('/');
        } finally {
            this.submitting.set(false);
        }
    }

    private isAuthErrorValue(value: unknown): value is AuthError {
        return (
            value === AuthError.EmailTaken ||
            value === AuthError.InvalidCredentials ||
            value === AuthError.NetworkError ||
            value === AuthError.ServerError
        );
    }
}
