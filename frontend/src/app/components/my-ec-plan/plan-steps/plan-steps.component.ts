import {
    ChangeDetectionStrategy,
    Component,
    ElementRef,
    Signal,
    computed,
    effect,
    inject,
    signal,
    untracked,
    viewChild,
} from '@angular/core';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { TranslocoModule, TranslocoService } from '@jsverse/transloco';

import {
    WizardProgressComponent,
    WizardProgressStep,
} from '@components/onboarding/ui/wizard-progress/wizard-progress.component';

import { PlanIntakeQuestion } from '../plan-intake/models/plan-intake.model';
import { PlanIntakeService } from '../plan-intake/services/plan-intake.service';

@Component({
    selector: 'app-plan-steps',
    imports: [
        TranslocoModule,
        ReactiveFormsModule,
        WizardProgressComponent,
    ],
    templateUrl: './plan-steps.component.html',
    styleUrl: './plan-steps.component.css',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PlanStepsComponent {
    private readonly transloco = inject(TranslocoService);
    private readonly router = inject(Router);
    protected readonly service = inject(PlanIntakeService);

    protected readonly textControl = new FormControl<string>('', { nonNullable: true });

    private readonly textValueSignal = toSignal(this.textControl.valueChanges, {
        initialValue: '',
    });

    protected readonly currentQuestion: Signal<PlanIntakeQuestion | null> =
        this.service.currentQuestion;
    protected readonly status = this.service.status;
    protected readonly answeredCount = this.service.answeredCount;
    protected readonly totalQuestions = this.service.totalQuestions;
    protected readonly isCompleted = this.service.isCompleted;

    protected readonly isSubmitting = computed(() => this.status() === 'submitting');
    protected readonly hasError = computed(() => this.status() === 'error');
    protected readonly isCollecting = computed(() => this.status() === 'collecting');

    protected readonly isLastQuestion = computed(
        () => this.answeredCount() === this.totalQuestions() - 1,
    );
    protected readonly isFirstQuestion = computed(() => this.answeredCount() === 0);

    protected readonly counterParams = computed(() => ({
        current: Math.min(this.answeredCount() + 1, this.totalQuestions()),
        total: this.totalQuestions(),
    }));

    protected readonly canContinue = computed(
        () => this.isCollecting() && this.textValueSignal().trim().length > 0,
    );

    protected readonly continueLabelKey = computed(() =>
        this.isLastQuestion() ? 'plan.steps.actions.finish' : 'plan.steps.actions.continue',
    );

    protected readonly progressSteps = computed<ReadonlyArray<WizardProgressStep>>(() => {
        const total = this.totalQuestions();
        const activeIndex = Math.min(this.answeredCount(), total - 1);
        return Array.from({ length: total }, (_, index) => ({
            index,
            active: index === activeIndex,
            reachable: index <= activeIndex,
        }));
    });

    protected readonly promptKey = computed(
        () => this.currentQuestion()?.promptKey ?? '',
    );
    protected readonly placeholderKey = computed(
        () => this.currentQuestion()?.placeholderKey ?? 'plan.intake.input.placeholder',
    );
    protected readonly suggestionKeys = computed<ReadonlyArray<string>>(
        () => this.currentQuestion()?.suggestionKeys ?? [],
    );

    private readonly textareaRef = viewChild<ElementRef<HTMLTextAreaElement>>('textarea');

    constructor() {
        effect(() => {
            const question = this.currentQuestion();
            if (!question) return;
            untracked(() => {
                this.textControl.setValue('', { emitEvent: false });
                queueMicrotask(() => {
                    this.textareaRef()?.nativeElement.focus();
                    this.resizeTextarea();
                });
            });
        });

        this.textControl.valueChanges
            .pipe(takeUntilDestroyed())
            .subscribe(() => queueMicrotask(() => this.resizeTextarea()));
    }

    protected applyChipKey(chipKey: string): void {
        const translated = this.transloco.translate(chipKey);
        this.textControl.setValue(translated);
        queueMicrotask(() => {
            this.textareaRef()?.nativeElement.focus();
            this.resizeTextarea();
        });
    }

    protected submit(): void {
        if (!this.canContinue()) return;
        const text = this.textControl.value.trim();
        if (!text) return;
        this.service.submitAnswer(text);
    }

    protected onKeydown(event: KeyboardEvent): void {
        if (event.key === 'Enter' && !event.shiftKey) {
            event.preventDefault();
            this.submit();
        }
    }

    protected retry(): void {
        void this.service.retrySubmit();
    }

    protected back(): void {
        if (this.isFirstQuestion()) {
            this.router.navigateByUrl('/');
            return;
        }
        this.service.previous();
    }

    protected reset(): void {
        this.service.reset();
    }

    protected gotoPlan(): void {
        this.router.navigateByUrl('/plan');
    }

    private resizeTextarea(): void {
        const textarea = this.textareaRef()?.nativeElement;
        if (!textarea) return;
        textarea.style.height = 'auto';
        const lineHeight = 24;
        const maxLines = 5;
        const maxHeight = lineHeight * maxLines;
        const next = Math.min(textarea.scrollHeight, maxHeight);
        textarea.style.height = `${next}px`;
        textarea.style.overflowY = textarea.scrollHeight > maxHeight ? 'auto' : 'hidden';
    }
}
