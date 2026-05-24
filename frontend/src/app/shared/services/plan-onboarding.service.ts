import { Injectable, Signal, signal } from '@angular/core';

const STORAGE_PREFIX = 'ec-plan-onboarding-completed-v1-';
const ANON_STORAGE_KEY = `${STORAGE_PREFIX}anonymous`;
const COMPLETED_VALUE = '1';

@Injectable({ providedIn: 'root' })
export class PlanOnboardingService {
    private readonly completedKeysSignal = signal<ReadonlySet<string>>(this.loadCompletedKeys());

    readonly completedKeys: Signal<ReadonlySet<string>> = this.completedKeysSignal.asReadonly();

    isCompleted(email: string | null | undefined): boolean {
        return this.completedKeysSignal().has(this.keyFor(email));
    }

    markCompleted(email: string | null | undefined): void {
        const key = this.keyFor(email);
        try {
            localStorage.setItem(key, COMPLETED_VALUE);
        } catch {
            // localStorage may be unavailable (private mode); swallow.
        }
        this.completedKeysSignal.update((current) => {
            if (current.has(key)) return current;
            const next = new Set(current);
            next.add(key);
            return next;
        });
    }

    clearCompleted(email: string | null | undefined): void {
        const key = this.keyFor(email);
        try {
            localStorage.removeItem(key);
        } catch {
            // swallow.
        }
        this.completedKeysSignal.update((current) => {
            if (!current.has(key)) return current;
            const next = new Set(current);
            next.delete(key);
            return next;
        });
    }

    private keyFor(email: string | null | undefined): string {
        return email ? `${STORAGE_PREFIX}${email}` : ANON_STORAGE_KEY;
    }

    private loadCompletedKeys(): ReadonlySet<string> {
        const keys = new Set<string>();
        try {
            for (let index = 0; index < localStorage.length; index += 1) {
                const key = localStorage.key(index);
                if (key && key.startsWith(STORAGE_PREFIX) && localStorage.getItem(key) === COMPLETED_VALUE) {
                    keys.add(key);
                }
            }
        } catch {
            // swallow.
        }
        return keys;
    }
}
