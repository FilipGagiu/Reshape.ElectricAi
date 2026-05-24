import { HttpErrorResponse } from '@angular/common/http';
import { Injectable, computed, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';

import { environment } from '../../../environments/environment';
import { AuthApi } from '../api/auth-api';
import { UserDto } from '../api/dto/auth.dto';
import { ErrorCode, extractErrorEnvelope } from '../api/error-envelope';
import { TokenStore } from '../api/token-store';

export type AuthUser = UserDto;

export const enum AuthError {
    EmailTaken = 'auth.error.emailTaken',
    InvalidCredentials = 'auth.error.invalidCredentials',
    NetworkError = 'auth.error.network',
    ServerError = 'auth.error.server',
}

const BYPASS_USER: UserDto = {
    id: '00000000-0000-0000-0000-000000000000',
    email: 'dev@bypass.local',
    role: 'User',
};

@Injectable({ providedIn: 'root' })
export class AuthService {
    private readonly authApi = inject(AuthApi);
    private readonly tokens = inject(TokenStore);

    readonly currentUser = this.tokens.user;
    readonly isAuthenticated = computed(() => this.tokens.hasSession());
    readonly isBypassActive = this.tokens.isBypass;
    readonly devBypassEnabled = environment.allowDevBypass;

    async register(email: string, password: string): Promise<AuthUser | AuthError> {
        const normalizedEmail = email.trim().toLowerCase();
        try {
            const response = await firstValueFrom(
                this.authApi.register({ email: normalizedEmail, password }),
            );
            this.tokens.setSession({
                accessToken: response.accessToken,
                refreshToken: response.refreshToken,
                user: response.user,
            });
            return response.user;
        } catch (err) {
            return this.toAuthError(err, AuthError.EmailTaken);
        }
    }

    async login(email: string, password: string): Promise<AuthUser | AuthError> {
        const normalizedEmail = email.trim().toLowerCase();
        try {
            const response = await firstValueFrom(
                this.authApi.login({ email: normalizedEmail, password }),
            );
            this.tokens.setSession({
                accessToken: response.accessToken,
                refreshToken: response.refreshToken,
                user: response.user,
            });
            return response.user;
        } catch (err) {
            return this.toAuthError(err, AuthError.InvalidCredentials);
        }
    }

    logout(): void {
        this.tokens.clear();
    }

    bypass(): AuthUser | null {
        if (!environment.allowDevBypass) return null;
        this.tokens.setBypassSession(BYPASS_USER);
        return BYPASS_USER;
    }

    async refreshProfile(): Promise<void> {
        if (this.tokens.isBypass() || !this.tokens.accessToken()) return;
        try {
            const user = await firstValueFrom(this.authApi.me());
            this.tokens.setUser(user);
        } catch {
            // Interceptor handles 401 → refresh-and-retry; any failure that survives means logout already happened.
        }
    }

    private toAuthError(err: unknown, conflictFallback: AuthError): AuthError {
        const envelope = extractErrorEnvelope(err);
        if (envelope.code === ErrorCode.NetworkError) return AuthError.NetworkError;
        if (err instanceof HttpErrorResponse) {
            if (err.status === 401) return AuthError.InvalidCredentials;
            if (err.status === 409) return conflictFallback;
            if (err.status === 400) return AuthError.InvalidCredentials;
        }
        return AuthError.ServerError;
    }
}
