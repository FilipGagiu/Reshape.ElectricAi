import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

/**
 * Segmented progress bars at the top of the stories viewer. One segment per
 * slide. The active segment fills linearly with `progressFraction`; past
 * segments are full, upcoming are dim.
 *
 * Visual spec: ui-designer locked spec, 3 px height, --space-1 gap,
 * --radius-none, white@60 filled / white@100 active / white@25 upcoming.
 */
@Component({
    selector: 'app-story-progress',
    imports: [],
    template: `
        <div class="ec-story-progress">
            @for (segment of segments(); track $index) {
                <span class="ec-story-progress__bar">
                    <span
                        class="ec-story-progress__fill"
                        [style.width.%]="segment.fillPct"
                        [class.ec-story-progress__fill--active]="segment.isActive"
                        [class.ec-story-progress__fill--past]="segment.isPast"
                    ></span>
                </span>
            }
        </div>
    `,
    styles: `
        :host {
            display: block;
        }

        .ec-story-progress {
            display: flex;
            gap: var(--space-1);
            padding: 0 var(--space-4);
        }

        .ec-story-progress__bar {
            flex: 1;
            height: 3px;
            background-color: rgba(255, 255, 255, 0.25);
            border-radius: var(--radius-none);
            overflow: hidden;
        }

        .ec-story-progress__fill {
            display: block;
            height: 100%;
            background-color: rgba(255, 255, 255, 1);
            transition: none;
        }

        .ec-story-progress__fill--past {
            background-color: rgba(255, 255, 255, 0.6);
        }
    `,
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StoryProgressComponent {
    readonly count = input.required<number>();
    readonly currentIndex = input.required<number>();
    readonly progressFraction = input<number>(0);

    protected readonly segments = computed(() => {
        const total = this.count();
        const active = this.currentIndex();
        const fraction = Math.max(0, Math.min(1, this.progressFraction()));
        return Array.from({ length: total }, (_, index) => {
            if (index < active) return { isPast: true, isActive: false, fillPct: 100 };
            if (index === active)
                return { isPast: false, isActive: true, fillPct: fraction * 100 };
            return { isPast: false, isActive: false, fillPct: 0 };
        });
    });
}
