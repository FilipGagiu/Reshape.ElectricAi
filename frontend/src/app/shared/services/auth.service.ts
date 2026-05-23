import { computed, Injectable, signal } from '@angular/core';

export interface AuthUser {
    readonly email: string;
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

        const session: AuthUser = { email: normalizedEmail };
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

        const session: AuthUser = { email: match.email };
        this.persistSession(session);
        this.currentUserSignal.set(session);
        return session;
    }

    logout(): void {
        localStorage.removeItem(SESSION_STORAGE_KEY);
        this.currentUserSignal.set(null);
    }

    bypass(): AuthUser {
        const session: AuthUser = { email: BYPASS_EMAIL };
        this.persistSession(session);
        this.currentUserSignal.set(session);
        return session;
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
            return { email: parsed.email };
        } catch {
            return null;
        }
    }

    private persistSession(user: AuthUser): void {
        localStorage.setItem(SESSION_STORAGE_KEY, JSON.stringify(user));
    }

    private hash(password: string): string {
        return btoa(unescape(encodeURIComponent(password)));
    }
}
