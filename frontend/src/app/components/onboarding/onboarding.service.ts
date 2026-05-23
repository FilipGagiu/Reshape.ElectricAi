import { computed, effect, inject, Injectable, signal, Signal } from '@angular/core';

import { AuthService } from '@shared/services/auth.service';

import {
    EMPTY_ONBOARDING_PROFILE,
    OnboardingMode,
    OnboardingProfile,
    OnboardingState,
    OnboardingStepId,
    ONBOARDING_STATE_VERSION,
} from './onboarding.model';
import { STEP_REGISTRY_BY_MODE } from './onboarding.tokens';

const STATE_STORAGE_PREFIX = 'ec-hackaton-onboarding-';

interface ProgressSnapshot {
    readonly index: number;
    readonly total: number;
    readonly ratio: number;
}

@Injectable({ providedIn: 'root' })
export class OnboardingService {
    private readonly authService = inject(AuthService);

    private readonly stateSignal = signal<OnboardingState>(this.loadInitialState());

    readonly state: Signal<OnboardingState> = this.stateSignal.asReadonly();
    readonly profile: Signal<OnboardingProfile> = computed(() => this.stateSignal().profile);
    readonly currentStep: Signal<OnboardingStepId> = computed(() => this.stateSignal().currentStep);
    readonly isCompleted: Signal<boolean> = computed(() => this.stateSignal().completed);
    readonly steps: Signal<ReadonlyArray<OnboardingStepId>> = computed(
        () => STEP_REGISTRY_BY_MODE[this.stateSignal().mode],
    );
    readonly progress: Signal<ProgressSnapshot> = computed(() => {
        const steps = this.steps();
        const index = steps.indexOf(this.stateSignal().currentStep);
        const safeIndex = index < 0 ? 0 : index;
        return {
            index: safeIndex,
            total: steps.length,
            ratio: steps.length === 0 ? 0 : (safeIndex + 1) / steps.length,
        };
    });
    readonly isFirstStep: Signal<boolean> = computed(() => this.progress().index === 0);
    readonly isLastStep: Signal<boolean> = computed(
        () => this.progress().index === this.progress().total - 1,
    );
    readonly canProceed: Signal<boolean> = computed(() => {
        const step = this.currentStep();
        const profile = this.profile();
        if (step === OnboardingStepId.Basics) {
            return profile.name.trim().length > 0;
        }
        return true;
    });
    readonly currentSelectionCount: Signal<number | null> = computed(() => {
        const step = this.currentStep();
        const profile = this.profile();
        switch (step) {
            case OnboardingStepId.Transport:
                return profile.transportationIds.length;
            case OnboardingStepId.Artists:
                return profile.artistIds.length;
            case OnboardingStepId.Music:
                return profile.genreIds.length;
            case OnboardingStepId.Cuisine:
                return profile.cuisineIds.length;
            case OnboardingStepId.Allergies:
                return profile.allergyIds.length;
            case OnboardingStepId.Activities:
                return profile.activityIds.length;
            default:
                return null;
        }
    });

    constructor() {
        effect(() => {
            const user = this.authService.currentUser();
            if (!user) {
                return;
            }
            this.stateSignal.set(this.loadStateForEmail(user.email));
        });

        effect(() => {
            const user = this.authService.currentUser();
            if (!user) {
                return;
            }
            this.persist(user.email, this.stateSignal());
        });
    }

    patchProfile(patch: Partial<OnboardingProfile>): void {
        this.stateSignal.update((current) => ({
            ...current,
            profile: { ...current.profile, ...patch },
            updatedAt: new Date().toISOString(),
        }));
    }

    toggleArrayMembership(key: ArrayProfileKey, id: string): void {
        const current = this.stateSignal().profile[key];
        const next = current.includes(id)
            ? current.filter((entry) => entry !== id)
            : [...current, id];
        this.patchProfile({ [key]: next } as Partial<OnboardingProfile>);
    }

    goTo(step: OnboardingStepId): void {
        if (!this.steps().includes(step)) {
            return;
        }
        this.stateSignal.update((current) => ({
            ...current,
            currentStep: step,
            updatedAt: new Date().toISOString(),
        }));
    }

    next(): void {
        const steps = this.steps();
        const index = steps.indexOf(this.stateSignal().currentStep);
        if (index < 0 || index >= steps.length - 1) {
            return;
        }
        this.goTo(steps[index + 1]);
    }

    previous(): void {
        const steps = this.steps();
        const index = steps.indexOf(this.stateSignal().currentStep);
        if (index <= 0) {
            return;
        }
        this.goTo(steps[index - 1]);
    }

    complete(): void {
        if (this.stateSignal().profile.name.trim().length === 0) {
            return;
        }
        this.stateSignal.update((current) => ({
            ...current,
            completed: true,
            updatedAt: new Date().toISOString(),
        }));
        this.authService.markOnboardingCompleted();
    }

    reset(): void {
        const user = this.authService.currentUser();
        if (user) {
            localStorage.removeItem(STATE_STORAGE_PREFIX + user.email);
            this.authService.resetOnboardingForCurrentUser();
        }
        this.stateSignal.set(this.buildInitialState());
    }

    private loadInitialState(): OnboardingState {
        const user = this.authService.currentUser();
        if (!user) {
            return this.buildInitialState();
        }
        return this.loadStateForEmail(user.email);
    }

    private loadStateForEmail(email: string): OnboardingState {
        const raw = localStorage.getItem(STATE_STORAGE_PREFIX + email);
        if (!raw) {
            return this.buildInitialState();
        }
        try {
            const parsed = JSON.parse(raw) as Partial<OnboardingState>;
            return this.normalize(parsed);
        } catch {
            return this.buildInitialState();
        }
    }

    private normalize(partial: Partial<OnboardingState>): OnboardingState {
        const fallback = this.buildInitialState();
        const profile: OnboardingProfile = {
            ...fallback.profile,
            ...(partial.profile ?? {}),
        };
        return {
            mode: partial.mode ?? fallback.mode,
            profile,
            currentStep: partial.currentStep ?? fallback.currentStep,
            completed: partial.completed ?? false,
            updatedAt: partial.updatedAt ?? fallback.updatedAt,
            version: partial.version ?? ONBOARDING_STATE_VERSION,
        };
    }

    private buildInitialState(): OnboardingState {
        return {
            mode: OnboardingMode.Individual,
            profile: EMPTY_ONBOARDING_PROFILE,
            currentStep: OnboardingStepId.Basics,
            completed: false,
            updatedAt: new Date().toISOString(),
            version: ONBOARDING_STATE_VERSION,
        };
    }

    private persist(email: string, state: OnboardingState): void {
        localStorage.setItem(STATE_STORAGE_PREFIX + email, JSON.stringify(state));
    }
}

type ArrayProfileKey = {
    [Key in keyof OnboardingProfile]: OnboardingProfile[Key] extends ReadonlyArray<string>
        ? Key
        : never;
}[keyof OnboardingProfile];
