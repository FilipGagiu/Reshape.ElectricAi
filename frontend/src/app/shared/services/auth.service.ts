import { computed, Injectable, signal } from '@angular/core';

export interface AuthUser {
    readonly email: string;
    readonly onboardingCompleted: boolean;
}

interface StoredUser {
    readonly email: string;
    readonly passwordHash: string;
}

export const enum AuthError {
    EmailTaken = 'auth.error.emailTaken',
    InvalidCredentials = 'auth.error.invalidCredentials',
}

const USERS_STORAGE_KEY = 'ec-hackaton-users';
const SESSION_STORAGE_KEY = 'ec-hackaton-session';
const ONBOARDING_FLAGS_STORAGE_KEY = 'ec-hackaton-onboarding-flags';
const BYPASS_EMAIL = 'dev@bypass.local';

@Injectable({ providedIn: 'root' })
export class AuthService {
    private readonly currentUserSignal = signal<AuthUser | null>(this.loadSession());

    readonly currentUser = this.currentUserSignal.asReadonly();
    readonly isAuthenticated = computed(() => this.currentUserSignal() !== null);

    register(email: string, password: string): AuthUser | AuthError {
        const normalizedEmail = email.trim().toLowerCase();
        const users = this.loadUsers();

        if (users.some((user) => user.email === normalizedEmail)) {
            return AuthError.EmailTaken;
        }

        const newUser: StoredUser = { email: normalizedEmail, passwordHash: this.hash(password) };
        this.saveUsers([...users, newUser]);
        this.writeOnboardingFlag(normalizedEmail, false);

        const session: AuthUser = { email: normalizedEmail, onboardingCompleted: false };
        this.persistSession(session);
        this.currentUserSignal.set(session);
        return session;
    }

    login(email: string, password: string): AuthUser | AuthError {
        const normalizedEmail = email.trim().toLowerCase();
        const passwordHash = this.hash(password);
        const match = this.loadUsers().find(
            (user) => user.email === normalizedEmail && user.passwordHash === passwordHash,
        );

        if (!match) {
            return AuthError.InvalidCredentials;
        }

        const session: AuthUser = {
            email: match.email,
            onboardingCompleted: this.readOnboardingFlag(match.email),
        };
        this.persistSession(session);
        this.currentUserSignal.set(session);
        return session;
    }

    logout(): void {
        localStorage.removeItem(SESSION_STORAGE_KEY);
        this.currentUserSignal.set(null);
    }

    bypass(): AuthUser {
        const session: AuthUser = {
            email: BYPASS_EMAIL,
            onboardingCompleted: this.readOnboardingFlag(BYPASS_EMAIL),
        };
        this.persistSession(session);
        this.currentUserSignal.set(session);
        return session;
    }

    markOnboardingCompleted(): void {
        const current = this.currentUserSignal();
        if (!current) {
            return;
        }
        this.writeOnboardingFlag(current.email, true);
        const updated: AuthUser = { ...current, onboardingCompleted: true };
        this.persistSession(updated);
        this.currentUserSignal.set(updated);
    }

    resetOnboardingForCurrentUser(): void {
        const current = this.currentUserSignal();
        if (!current) {
            return;
        }
        this.writeOnboardingFlag(current.email, false);
        const updated: AuthUser = { ...current, onboardingCompleted: false };
        this.persistSession(updated);
        this.currentUserSignal.set(updated);
    }

    private loadUsers(): ReadonlyArray<StoredUser> {
        const raw = localStorage.getItem(USERS_STORAGE_KEY);
        if (!raw) {
            return [];
        }
        try {
            const parsed = JSON.parse(raw) as unknown;
            return Array.isArray(parsed) ? (parsed as StoredUser[]) : [];
        } catch {
            return [];
        }
    }

    private saveUsers(users: ReadonlyArray<StoredUser>): void {
        localStorage.setItem(USERS_STORAGE_KEY, JSON.stringify(users));
    }

    private loadSession(): AuthUser | null {
        const raw = localStorage.getItem(SESSION_STORAGE_KEY);
        if (!raw) {
            return null;
        }
        try {
            const parsed = JSON.parse(raw) as Partial<AuthUser>;
            if (!parsed?.email) {
                return null;
            }
            return {
                email: parsed.email,
                onboardingCompleted:
                    typeof parsed.onboardingCompleted === 'boolean'
                        ? parsed.onboardingCompleted
                        : this.readOnboardingFlag(parsed.email),
            };
        } catch {
            return null;
        }
    }

    private persistSession(user: AuthUser): void {
        localStorage.setItem(SESSION_STORAGE_KEY, JSON.stringify(user));
    }

    private readOnboardingFlag(email: string): boolean {
        const flags = this.loadOnboardingFlags();
        return flags[email] === true;
    }

    private writeOnboardingFlag(email: string, completed: boolean): void {
        const flags = { ...this.loadOnboardingFlags(), [email]: completed };
        localStorage.setItem(ONBOARDING_FLAGS_STORAGE_KEY, JSON.stringify(flags));
    }

    private loadOnboardingFlags(): Record<string, boolean> {
        const raw = localStorage.getItem(ONBOARDING_FLAGS_STORAGE_KEY);
        if (!raw) {
            return {};
        }
        try {
            const parsed = JSON.parse(raw) as unknown;
            return parsed && typeof parsed === 'object' ? (parsed as Record<string, boolean>) : {};
        } catch {
            return {};
        }
    }

    private hash(password: string): string {
        return btoa(unescape(encodeURIComponent(password)));
    }
}
