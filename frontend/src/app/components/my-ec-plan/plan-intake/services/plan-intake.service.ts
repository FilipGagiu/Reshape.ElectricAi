import { Injectable, Signal, computed, effect, inject, signal } from '@angular/core';
import { TranslocoService } from '@jsverse/transloco';
import { firstValueFrom } from 'rxjs';

import { ItineraryApi } from '@shared/api/itinerary-api';
import { ItineraryStore } from '@shared/api/itinerary-store';
import { ItineraryGenerationRequest, WizardAnswer } from '@shared/api/dto/itinerary.dto';
import { extractErrorEnvelope } from '@shared/api/error-envelope';
import { AuthService } from '@shared/services/auth.service';
import { PlanOnboardingService } from '@shared/services/plan-onboarding.service';

import {
    EMPTY_PLAN_INTAKE_STATE,
    PlanIntakeAnswer,
    PlanIntakeQuestion,
    PlanIntakeQuestionId,
    PlanIntakeState,
    PlanIntakeStatus,
    PlanIntakeTranscriptItem,
} from '../models/plan-intake.model';
import { PLAN_INTAKE_QUESTIONS } from './plan-intake.questions';

const STORAGE_PREFIX = 'ec-plan-intake-v3-';
const ITINERARY_REQUEST_VERSION = 1;
const GENERATE_MAX_ATTEMPTS = 3;
const GENERATE_RETRY_DELAY_MS = 1000;
const ANON_STORAGE_KEY = `${STORAGE_PREFIX}anonymous`;

const ASSISTANT_TYPING_DELAY_MS = 750;

const GREETING_KEY = 'plan.intake.greeting';
const THANKS_KEY = 'plan.intake.thanks';

@Injectable({ providedIn: 'root' })
export class PlanIntakeService {
    private readonly itineraryApi = inject(ItineraryApi);
    private readonly itineraryStore = inject(ItineraryStore);
    private readonly auth = inject(AuthService);
    private readonly transloco = inject(TranslocoService);
    private readonly planOnboarding = inject(PlanOnboardingService);

    private readonly stateSignal = signal<PlanIntakeState>(this.loadInitialState());
    private readonly isAssistantTypingSignal = signal(false);
    private typingTimerId: ReturnType<typeof setTimeout> | null = null;

    readonly state: Signal<PlanIntakeState> = this.stateSignal.asReadonly();
    readonly status: Signal<PlanIntakeStatus> = computed(() => this.stateSignal().status);
    readonly isCompleted: Signal<boolean> = computed(
        () => this.stateSignal().status === 'submitted',
    );
    readonly answeredCount: Signal<number> = computed(() => this.stateSignal().answers.length);
    readonly totalQuestions: Signal<number> = computed(() => PLAN_INTAKE_QUESTIONS.length);
    readonly currentIndex: Signal<number> = computed(() => this.stateSignal().currentIndex);
    readonly answers: Signal<ReadonlyArray<PlanIntakeAnswer>> = computed(
        () => this.stateSignal().answers,
    );
    readonly isAssistantTyping = this.isAssistantTypingSignal.asReadonly();

    readonly currentQuestion: Signal<PlanIntakeQuestion | null> = computed(() => {
        const state = this.stateSignal();
        if (state.status !== 'collecting') return null;
        return PLAN_INTAKE_QUESTIONS[state.currentIndex] ?? null;
    });

    readonly transcript: Signal<ReadonlyArray<PlanIntakeTranscriptItem>> = computed(() =>
        this.buildTranscript(this.stateSignal()),
    );

    constructor() {
        effect(() => {
            const user = this.auth.currentUser();
            this.stateSignal.set(this.loadStateForKey(this.storageKeyFor(user?.email)));
        });

        effect(() => {
            const state = this.stateSignal();
            const user = this.auth.currentUser();
            this.persist(this.storageKeyFor(user?.email), state);
        });
    }

    submitAnswer(rawText: string): void {
        const text = rawText.trim();
        if (!text) return;
        this.cancelAssistantTyping();
        this.recordAnswerAndAdvance({ text, skipped: false });
    }

    /**
     * Record an empty/skipped answer for the current question and advance.
     * Stepped variant only — chat mode never calls this.
     */
    skipCurrent(): void {
        this.cancelAssistantTyping();
        this.recordAnswerAndAdvance({ text: '', skipped: true });
    }

    private cancelAssistantTyping(): void {
        this.clearTypingTimer();
        this.isAssistantTypingSignal.set(false);
    }

    private recordAnswerAndAdvance(payload: { text: string; skipped: boolean }): void {
        const current = this.stateSignal();
        if (current.status !== 'collecting') return;
        if (this.isAssistantTypingSignal()) return;

        const question = PLAN_INTAKE_QUESTIONS[current.currentIndex];
        if (!question) return;

        const answer: PlanIntakeAnswer = {
            questionId: question.id,
            text: payload.text,
            skipped: payload.skipped,
            answeredAt: new Date().toISOString(),
        };

        const existing = current.answers;
        const nextAnswers =
            current.currentIndex < existing.length
                ? existing.map((entry, idx) => (idx === current.currentIndex ? answer : entry))
                : [...existing, answer];

        const nextIndex = current.currentIndex + 1;
        const isLast = nextIndex >= PLAN_INTAKE_QUESTIONS.length;

        this.stateSignal.set({
            ...current,
            status: isLast ? 'submitting' : 'collecting',
            currentIndex: nextIndex,
            answers: nextAnswers,
            errorCode: undefined,
            updatedAt: new Date().toISOString(),
        });

        if (isLast) {
            void this.submit();
            return;
        }

        this.startAssistantTyping();
    }

    private startAssistantTyping(): void {
        this.clearTypingTimer();
        this.isAssistantTypingSignal.set(true);
        this.typingTimerId = setTimeout(() => {
            this.isAssistantTypingSignal.set(false);
            this.typingTimerId = null;
        }, ASSISTANT_TYPING_DELAY_MS);
    }

    private clearTypingTimer(): void {
        if (this.typingTimerId !== null) {
            clearTimeout(this.typingTimerId);
            this.typingTimerId = null;
        }
    }

    async retrySubmit(): Promise<void> {
        const current = this.stateSignal();
        if (current.status !== 'error') return;
        this.stateSignal.set({
            ...current,
            status: 'submitting',
            errorCode: undefined,
            updatedAt: new Date().toISOString(),
        });
        await this.submit();
    }

    /**
     * Step back one question. Preserves answers so prefill works on return.
     * No-op on the first question. Stepped variant only.
     */
    previous(): void {
        const current = this.stateSignal();
        if (current.currentIndex <= 0) return;
        this.clearTypingTimer();
        this.isAssistantTypingSignal.set(false);
        this.stateSignal.set({
            ...current,
            status: 'collecting',
            currentIndex: current.currentIndex - 1,
            errorCode: undefined,
            updatedAt: new Date().toISOString(),
        });
    }

    /**
     * Step forward one question — only if the next slot is already answered
     * (or skipped). Prevents leap-frogging over unanswered questions.
     * Stepped variant only.
     */
    goForward(): void {
        const current = this.stateSignal();
        if (current.currentIndex >= PLAN_INTAKE_QUESTIONS.length - 1) return;
        if (current.currentIndex >= current.answers.length) return;
        this.clearTypingTimer();
        this.isAssistantTypingSignal.set(false);
        this.stateSignal.set({
            ...current,
            status: 'collecting',
            currentIndex: current.currentIndex + 1,
            errorCode: undefined,
            updatedAt: new Date().toISOString(),
        });
    }

    reset(): void {
        const user = this.auth.currentUser();
        localStorage.removeItem(this.storageKeyFor(user?.email));
        this.stateSignal.set(this.freshState());
    }

    findAnswer(questionId: PlanIntakeQuestionId): PlanIntakeAnswer | undefined {
        return this.stateSignal().answers.find((entry) => entry.questionId === questionId);
    }

    private async submit(): Promise<void> {
        const snapshot = this.stateSignal();
        const request = this.buildGenerationRequest(snapshot.answers);

        let lastError: unknown = null;
        for (let attempt = 1; attempt <= GENERATE_MAX_ATTEMPTS; attempt += 1) {
            try {
                const response = await firstValueFrom(this.itineraryApi.generate(request));
                this.itineraryStore.set(response);
                this.markSubmitted();
                return;
            } catch (error) {
                lastError = error;
                console.warn(
                    `[plan-intake] generate attempt ${attempt}/${GENERATE_MAX_ATTEMPTS} failed`,
                    error,
                );
                if (attempt < GENERATE_MAX_ATTEMPTS) {
                    // Brief backoff so transient backend hiccups get a chance to
                    // clear before the next try. Status stays 'submitting' so
                    // the loader animation keeps running silently.
                    await new Promise<void>((resolve) =>
                        setTimeout(resolve, GENERATE_RETRY_DELAY_MS),
                    );
                }
            }
        }

        const envelope = extractErrorEnvelope(lastError);
        console.error('[plan-intake] submit failed after retries', envelope);
        this.stateSignal.update((current) => ({
            ...current,
            status: 'error',
            errorCode: envelope.code,
            updatedAt: new Date().toISOString(),
        }));
    }

    private buildGenerationRequest(answers: ReadonlyArray<PlanIntakeAnswer>): ItineraryGenerationRequest {
        const lastQuestionId = PLAN_INTAKE_QUESTIONS[PLAN_INTAKE_QUESTIONS.length - 1]?.id;
        const freeTextAnswer = lastQuestionId
            ? answers.find((entry) => entry.questionId === lastQuestionId)
            : undefined;
        const structuredAnswers = answers
            .filter((entry) => entry.questionId !== lastQuestionId)
            .map((entry) => this.toWizardAnswer(entry));

        return {
            version: ITINERARY_REQUEST_VERSION,
            locale: this.transloco.getActiveLang() ?? null,
            submittedAt: new Date().toISOString(),
            answers: structuredAnswers,
            freeText: this.normalizeFreeText(freeTextAnswer),
        };
    }

    private toWizardAnswer(answer: PlanIntakeAnswer): WizardAnswer {
        const question = PLAN_INTAKE_QUESTIONS.find((entry) => entry.id === answer.questionId);
        const questionText = question ? this.transloco.translate(question.promptKey) : answer.questionId;
        return {
            question: questionText,
            answer: answer.skipped ? null : answer.text,
            answeredAt: answer.answeredAt,
        };
    }

    private normalizeFreeText(answer: PlanIntakeAnswer | undefined): string | null {
        if (!answer || answer.skipped) return null;
        const trimmed = answer.text.trim();
        return trimmed.length > 0 ? trimmed : null;
    }

    private markSubmitted(): void {
        this.stateSignal.update((current) => ({
            ...current,
            status: 'submitted',
            errorCode: undefined,
            updatedAt: new Date().toISOString(),
        }));
        this.planOnboarding.markCompleted(this.auth.currentUser()?.email);
    }

    private buildTranscript(state: PlanIntakeState): ReadonlyArray<PlanIntakeTranscriptItem> {
        const items: PlanIntakeTranscriptItem[] = [];

        items.push({
            id: 'plan-intake-greeting',
            role: 'assistant',
            kind: 'i18n',
            text: GREETING_KEY,
            createdAt: state.updatedAt,
        });

        state.answers.forEach((answer, index) => {
            const question = PLAN_INTAKE_QUESTIONS[index];
            if (question) {
                items.push({
                    id: `plan-intake-prompt-${question.id}`,
                    role: 'assistant',
                    kind: 'i18n',
                    text: question.promptKey,
                    createdAt: answer.answeredAt,
                });
            }
            items.push({
                id: `plan-intake-answer-${answer.questionId}`,
                role: 'user',
                kind: answer.skipped ? 'i18n' : 'literal',
                text: answer.skipped ? 'plan.intake.skipped' : answer.text,
                createdAt: answer.answeredAt,
            });
        });

        // Only append a "next prompt" bubble when the user is at the unanswered
        // frontier. If they navigated back via the stepped variant, currentIndex
        // sits inside the already-answered range — no fresh prompt needed.
        if (
            state.status === 'collecting' &&
            !this.isAssistantTypingSignal() &&
            state.currentIndex === state.answers.length
        ) {
            const next = PLAN_INTAKE_QUESTIONS[state.currentIndex];
            if (next) {
                items.push({
                    id: `plan-intake-prompt-${next.id}`,
                    role: 'assistant',
                    kind: 'i18n',
                    text: next.promptKey,
                    createdAt: state.updatedAt,
                });
            }
        }

        if (state.status === 'submitted') {
            items.push({
                id: 'plan-intake-thanks',
                role: 'assistant',
                kind: 'i18n',
                text: THANKS_KEY,
                createdAt: state.updatedAt,
            });
        }

        return items;
    }

    private storageKeyFor(email: string | null | undefined): string {
        return email ? `${STORAGE_PREFIX}${email}` : ANON_STORAGE_KEY;
    }

    private loadInitialState(): PlanIntakeState {
        const user = this.auth.currentUser();
        return this.loadStateForKey(this.storageKeyFor(user?.email));
    }

    private loadStateForKey(key: string): PlanIntakeState {
        const raw = localStorage.getItem(key);
        if (!raw) return this.freshState();
        try {
            const parsed = JSON.parse(raw) as Partial<PlanIntakeState>;
            return this.normalize(parsed);
        } catch {
            return this.freshState();
        }
    }

    private normalize(partial: Partial<PlanIntakeState>): PlanIntakeState {
        const fallback = this.freshState();
        const answers = Array.isArray(partial.answers)
            ? partial.answers.filter((entry): entry is PlanIntakeAnswer =>
                  Boolean(
                      entry &&
                          typeof entry.text === 'string' &&
                          typeof entry.answeredAt === 'string' &&
                          PLAN_INTAKE_QUESTIONS.some((question) => question.id === entry.questionId),
                  ),
              )
            : [];

        const boundedIndex = Math.max(
            0,
            Math.min(partial.currentIndex ?? answers.length, PLAN_INTAKE_QUESTIONS.length),
        );

        const status: PlanIntakeStatus =
            partial.status === 'submitted'
                ? 'submitted'
                : boundedIndex >= PLAN_INTAKE_QUESTIONS.length
                  ? partial.status === 'error'
                      ? 'error'
                      : 'submitting'
                  : 'collecting';

        return {
            ...fallback,
            answers,
            currentIndex: boundedIndex,
            status,
            errorCode: partial.errorCode,
            updatedAt: partial.updatedAt ?? fallback.updatedAt,
        };
    }

    private freshState(): PlanIntakeState {
        return { ...EMPTY_PLAN_INTAKE_STATE, updatedAt: new Date().toISOString() };
    }

    private persist(key: string, state: PlanIntakeState): void {
        try {
            localStorage.setItem(key, JSON.stringify(state));
        } catch {
            // localStorage may be unavailable (private mode); swallow.
        }
    }

}
