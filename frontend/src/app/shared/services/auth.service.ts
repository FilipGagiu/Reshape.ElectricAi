import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { computed, inject, Injectable, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';

import { API_BASE_URL } from '@shared/tokens/api-base-url.token';

export type UserRole = 'User' | 'Organizer';

export interface AuthUser {
    readonly id: string;
    readonly email: string;
    readonly role: UserRole;
}

export const enum AuthError {
    EmailTaken = 'auth.error.emailTaken',
    InvalidCredentials = 'auth.error.invalidCredentials',
    Network = 'auth.error.network',
}

interface AuthResponse {
    readonly accessToken: string;
    readonly refreshToken: string;
    readonly expiresIn: number;
    readonly user: AuthUser;
}

interface BackendErrorBody {
    readonly error?: { readonly code?: string; readonly message?: string };
}

const ACCESS_STORAGE_KEY = 'ec-hackaton-access';
const REFRESH_STORAGE_KEY = 'ec-hackaton-refresh';
const USER_STORAGE_KEY = 'ec-hackaton-user';

const BYPASS_USER: AuthUser = {
    id: '00000000-0000-0000-0000-000000000000',
    email: 'dev@bypass.local',
    role: 'User',
};

@Injectable({ providedIn: 'root' })
export class AuthService {
    private readonly http = inject(HttpClient);
    private readonly apiBaseUrl = inject(API_BASE_URL);

    private readonly accessTokenSignal = signal<string | null>(
        localStorage.getItem(ACCESS_STORAGE_KEY),
    );
    private readonly refreshTokenSignal = signal<string | null>(
        localStorage.getItem(REFRESH_STORAGE_KEY),
    );
    private readonly currentUserSignal = signal<AuthUser | null>(this.loadStoredUser());

    private inFlightRefresh: Promise<boolean> | null = null;

    readonly currentUser = this.currentUserSignal.asReadonly();
    readonly isAuthenticated = computed(() => this.currentUserSignal() !== null);

    constructor() {
        if (this.refreshTokenSignal() !== null && this.currentUserSignal() === null) {
            void this.bootstrapSession();
        }
    }

    accessToken(): string | null {
        return this.accessTokenSignal();
    }

    async register(email: string, password: string): Promise<AuthUser | AuthError> {
        return this.runAuthRequest('register', { email: email.trim().toLowerCase(), password });
    }

    async login(email: string, password: string): Promise<AuthUser | AuthError> {
        return this.runAuthRequest('login', { email: email.trim().toLowerCase(), password });
    }

    logout(): void {
        localStorage.removeItem(ACCESS_STORAGE_KEY);
        localStorage.removeItem(REFRESH_STORAGE_KEY);
        localStorage.removeItem(USER_STORAGE_KEY);
        this.accessTokenSignal.set(null);
        this.refreshTokenSignal.set(null);
        this.currentUserSignal.set(null);
        this.inFlightRefresh = null;
    }

    bypass(): AuthUser {
        this.persistUser(BYPASS_USER);
        this.currentUserSignal.set(BYPASS_USER);
        return BYPASS_USER;
    }

    async refresh(): Promise<boolean> {
        if (this.inFlightRefresh !== null) {
            return this.inFlightRefresh;
        }
        const refreshToken = this.refreshTokenSignal();
        if (refreshToken === null) {
            return false;
        }
        this.inFlightRefresh = this.performRefresh(refreshToken).finally(() => {
            this.inFlightRefresh = null;
        });
        return this.inFlightRefresh;
    }

    private async performRefresh(refreshToken: string): Promise<boolean> {
        try {
            const response = await firstValueFrom(
                this.http.post<AuthResponse>(`${this.apiBaseUrl}/api/v1/auth/refresh`, {
                    refreshToken,
                }),
            );
            this.applySession(response);
            return true;
        } catch {
            this.logout();
            return false;
        }
    }

    private async runAuthRequest(
        endpoint: 'login' | 'register',
        body: { email: string; password: string },
    ): Promise<AuthUser | AuthError> {
        try {
            const response = await firstValueFrom(
                this.http.post<AuthResponse>(`${this.apiBaseUrl}/api/v1/auth/${endpoint}`, body),
            );
            this.applySession(response);
            return response.user;
        } catch (error) {
            return this.mapError(error);
        }
    }

    private async bootstrapSession(): Promise<void> {
        const ok = await this.fetchCurrentUser();
        if (ok) {
            return;
        }
        const refreshed = await this.refresh();
        if (!refreshed) {
            return;
        }
        await this.fetchCurrentUser();
    }

    private async fetchCurrentUser(): Promise<boolean> {
        if (this.accessTokenSignal() === null) {
            return false;
        }
        try {
            const user = await firstValueFrom(
                this.http.get<AuthUser>(`${this.apiBaseUrl}/api/v1/auth/me`),
            );
            this.currentUserSignal.set(user);
            this.persistUser(user);
            return true;
        } catch {
            return false;
        }
    }

    private applySession(response: AuthResponse): void {
        localStorage.setItem(ACCESS_STORAGE_KEY, response.accessToken);
        localStorage.setItem(REFRESH_STORAGE_KEY, response.refreshToken);
        this.persistUser(response.user);
        this.accessTokenSignal.set(response.accessToken);
        this.refreshTokenSignal.set(response.refreshToken);
        this.currentUserSignal.set(response.user);
    }

    private persistUser(user: AuthUser): void {
        localStorage.setItem(USER_STORAGE_KEY, JSON.stringify(user));
    }

    private loadStoredUser(): AuthUser | null {
        const raw = localStorage.getItem(USER_STORAGE_KEY);
        if (raw === null) {
            return null;
        }
        try {
            const parsed = JSON.parse(raw) as Partial<AuthUser>;
            if (
                typeof parsed.id !== 'string' ||
                typeof parsed.email !== 'string' ||
                (parsed.role !== 'User' && parsed.role !== 'Organizer')
            ) {
                return null;
            }
            return { id: parsed.id, email: parsed.email, role: parsed.role };
        } catch {
            return null;
        }
    }

    private mapError(error: unknown): AuthError {
        if (!(error instanceof HttpErrorResponse)) {
            return AuthError.Network;
        }
        if (error.status === 0) {
            return AuthError.Network;
        }
        const code = (error.error as BackendErrorBody | null)?.error?.code;
        if (code === 'email-in-use') {
            return AuthError.EmailTaken;
        }
        if (code === 'invalid-credentials') {
            return AuthError.InvalidCredentials;
        }
        if (error.status === 409) {
            return AuthError.EmailTaken;
        }
        if (error.status === 401) {
            return AuthError.InvalidCredentials;
        }
        return AuthError.Network;
    }
}
