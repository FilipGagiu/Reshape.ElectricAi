export const PLAN_INTAKE_STATE_VERSION = 2;

export type PlanIntakeStatus = 'collecting' | 'submitting' | 'submitted' | 'error';

export enum PlanIntakeQuestionId {
    Name = 'name',
    Origin = 'origin',
    Accommodation = 'accommodation',
    Vibe = 'vibe',
    Music = 'music',
    Dietary = 'dietary',
    Extra = 'extra',
}

export interface PlanIntakeQuestion {
    readonly id: PlanIntakeQuestionId;
    readonly promptKey: string;
    readonly placeholderKey: string;
    readonly descriptionKey?: string;
    readonly suggestionKeys: ReadonlyArray<string>;
}

export interface PlanIntakeAnswer {
    readonly questionId: PlanIntakeQuestionId;
    readonly text: string;
    readonly skipped?: boolean;
    readonly answeredAt: string;
}

export interface PlanIntakeState {
    readonly status: PlanIntakeStatus;
    readonly currentIndex: number;
    readonly answers: ReadonlyArray<PlanIntakeAnswer>;
    readonly errorCode?: string;
    readonly version: number;
    readonly updatedAt: string;
}

export interface PlanIntakeSubmittedAnswer {
    readonly question: string;
    readonly answer: string;
    readonly answeredAt: string;
}

export interface PlanIntakeSubmission {
    readonly version: number;
    readonly submittedAt: string;
    readonly locale: string;
    readonly answers: ReadonlyArray<PlanIntakeSubmittedAnswer>;
}

export type PlanIntakeBubbleRole = 'assistant' | 'user';
export type PlanIntakeBubbleKind = 'i18n' | 'literal';

export interface PlanIntakeTranscriptItem {
    readonly id: string;
    readonly role: PlanIntakeBubbleRole;
    readonly kind: PlanIntakeBubbleKind;
    readonly text: string;
    readonly createdAt: string;
}

export const EMPTY_PLAN_INTAKE_STATE: PlanIntakeState = {
    status: 'collecting',
    currentIndex: 0,
    answers: [],
    version: PLAN_INTAKE_STATE_VERSION,
    updatedAt: new Date(0).toISOString(),
};
