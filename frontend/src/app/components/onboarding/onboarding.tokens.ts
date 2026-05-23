import { OnboardingMode, OnboardingStepId } from './onboarding.model';

export const INDIVIDUAL_STEP_REGISTRY: ReadonlyArray<OnboardingStepId> = [
    OnboardingStepId.Basics,
    OnboardingStepId.AgeGroup,
    OnboardingStepId.Ticket,
    OnboardingStepId.Accommodation,
    OnboardingStepId.Transport,
    OnboardingStepId.Artists,
    OnboardingStepId.Music,
    OnboardingStepId.Cuisine,
    OnboardingStepId.Allergies,
    OnboardingStepId.Activities,
];

export const STEP_REGISTRY_BY_MODE: Readonly<Record<OnboardingMode, ReadonlyArray<OnboardingStepId>>> = {
    [OnboardingMode.Individual]: INDIVIDUAL_STEP_REGISTRY,
    [OnboardingMode.Group]: INDIVIDUAL_STEP_REGISTRY,
};

export interface StepMeta {
    readonly icon: string;
    readonly heroGradient: string;
}

export const STEP_META: Readonly<Record<OnboardingStepId, StepMeta>> = {
    [OnboardingStepId.Basics]: {
        icon: '👋',
        heroGradient: 'from-primary-400 via-primary-500 to-accent-500',
    },
    [OnboardingStepId.AgeGroup]: {
        icon: '🎂',
        heroGradient: 'from-pink-400 via-rose-500 to-fuchsia-500',
    },
    [OnboardingStepId.Ticket]: {
        icon: '🎟️',
        heroGradient: 'from-amber-400 via-orange-500 to-red-500',
    },
    [OnboardingStepId.Accommodation]: {
        icon: '🏕️',
        heroGradient: 'from-emerald-400 via-teal-500 to-cyan-500',
    },
    [OnboardingStepId.Transport]: {
        icon: '🚆',
        heroGradient: 'from-sky-400 via-blue-500 to-indigo-500',
    },
    [OnboardingStepId.Artists]: {
        icon: '🎤',
        heroGradient: 'from-fuchsia-400 via-pink-500 to-rose-500',
    },
    [OnboardingStepId.Music]: {
        icon: '🎧',
        heroGradient: 'from-violet-400 via-purple-500 to-fuchsia-500',
    },
    [OnboardingStepId.Cuisine]: {
        icon: '🍽️',
        heroGradient: 'from-orange-400 via-rose-400 to-red-500',
    },
    [OnboardingStepId.Allergies]: {
        icon: '🌿',
        heroGradient: 'from-lime-400 via-green-500 to-emerald-500',
    },
    [OnboardingStepId.Activities]: {
        icon: '✨',
        heroGradient: 'from-yellow-400 via-amber-500 to-orange-500',
    },
};
