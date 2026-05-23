import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { TranslocoModule } from '@jsverse/transloco';

import { EcModalComponent } from '@shared/components/ec-modal/ec-modal.component';

import { HotQuestion } from './models/question.model';

/**
 * Expanded view of a hot question. Shows the curated answer with an
 * "Ask a follow-up" primary CTA at the bottom.
 *
 * Visual spec: visual-design-language.md §18 Modal + plan §"Hot-question modal".
 */
@Component({
    selector: 'app-hot-question-modal',
    imports: [EcModalComponent, TranslocoModule],
    templateUrl: './hot-question-modal.component.html',
    styleUrl: './hot-question-modal.component.css',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class HotQuestionModalComponent {
    readonly question = input<HotQuestion | null>(null);
    readonly open = input<boolean>(false);

    readonly close = output<void>();
    readonly askFollowUp = output<HotQuestion>();

    protected readonly titleId = 'hot-question-modal-title';

    protected readonly paragraphs = computed(() => {
        const q = this.question();
        if (!q) return [];
        return q.curatedAnswer
            .split(/\n\n+/)
            .map((p) => p.trim())
            .filter((p) => p.length > 0);
    });

    protected onAskFollowUp(): void {
        const q = this.question();
        if (q) this.askFollowUp.emit(q);
    }
}
