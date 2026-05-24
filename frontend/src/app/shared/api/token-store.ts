import { Injectable, computed, signal } from '@angular/core';

import { UserDto } from './dto/auth.dto';

const ACCESS_TOKEN_KEY = 'ec-access-token';
const REFRESH_TOKEN_KEY = 'ec-refresh-token';
const USER_KEY = 'ec-user';
const BYPASS_FLAG_KEY = 'ec-bypass';

interface PersistedSession {
    readonly accessToken: string;
    readonly refreshToken: string;
    readonly user: UserDto;
}

@Injectable({ providedIn: 'root' })
export class TokenStore {
    private readonly accessTokenSignal = signal<string | null>(this.readString(ACCESS_TOKEN_KEY));
    private readonly refreshTokenSignal = signal<string | null>(this.readString(REFRESH_TOKEN_KEY));
    private readonly userSignal = signal<UserDto | null>(this.readUser());
    private readonly bypassActiveSignal = signal<boolean>(this.readString(BYPASS_FLAG_KEY) === '1');

    readonly accessToken = this.accessTokenSignal.asReadonly();
    readonly refreshToken = this.refreshTokenSignal.asReadonly();
    readonly user = this.userSignal.asReadonly();
    readonly isBypass = this.bypassActiveSignal.asReadonly();
    readonly hasSession = computed(
        () => this.userSignal() !== null && (this.accessTokenSignal() !== null || this.bypassActiveSignal()),
    );

    setSession(session: PersistedSession): void {
        localStorage.setItem(ACCESS_TOKEN_KEY, session.accessToken);
        localStorage.setItem(REFRESH_TOKEN_KEY, session.refreshToken);
        localStorage.setItem(USER_KEY, JSON.stringify(session.user));
        localStorage.removeItem(BYPASS_FLAG_KEY);
        this.accessTokenSignal.set(session.accessToken);
        this.refreshTokenSignal.set(session.refreshToken);
        this.userSignal.set(session.user);
        this.bypassActiveSignal.set(false);
    }

    setBypassSession(user: UserDto): void {
        localStorage.setItem(USER_KEY, JSON.stringify(user));
        localStorage.setItem(BYPASS_FLAG_KEY, '1');
        localStorage.removeItem(ACCESS_TOKEN_KEY);
        localStorage.removeItem(REFRESH_TOKEN_KEY);
        this.accessTokenSignal.set(null);
        this.refreshTokenSignal.set(null);
        this.userSignal.set(user);
        this.bypassActiveSignal.set(true);
    }

    rotate(accessToken: string, refreshToken: string): void {
        localStorage.setItem(ACCESS_TOKEN_KEY, accessToken);
        localStorage.setItem(REFRESH_TOKEN_KEY, refreshToken);
        this.accessTokenSignal.set(accessToken);
        this.refreshTokenSignal.set(refreshToken);
    }

    setUser(user: UserDto): void {
        localStorage.setItem(USER_KEY, JSON.stringify(user));
        this.userSignal.set(user);
    }

    clear(): void {
        localStorage.removeItem(ACCESS_TOKEN_KEY);
        localStorage.removeItem(REFRESH_TOKEN_KEY);
        localStorage.removeItem(USER_KEY);
        localStorage.removeItem(BYPASS_FLAG_KEY);
        this.accessTokenSignal.set(null);
        this.refreshTokenSignal.set(null);
        this.userSignal.set(null);
        this.bypassActiveSignal.set(false);
    }

    private readString(key: string): string | null {
        try {
            return localStorage.getItem(key);
        } catch {
            return null;
        }
    }

    private readUser(): UserDto | null {
        const raw = this.readString(USER_KEY);
        if (!raw) return null;
        try {
            const parsed = JSON.parse(raw) as Partial<UserDto>;
            if (!parsed?.id || !parsed?.email || !parsed?.role) return null;
            return { id: parsed.id, email: parsed.email, role: parsed.role };
        } catch {
            return null;
        }
    }
}
