import { ChangeDetectionStrategy, Component, input, output, signal } from '@angular/core';
import { TranslocoModule, TranslocoService } from '@jsverse/transloco';
import { inject } from '@angular/core';

import { PlanShareSlide } from '../plan-share.model';

@Component({
    selector: 'app-share-slide',
    imports: [TranslocoModule],
    template: `
        <div class="ec-slide">
            <span class="ec-slide__wordmark">ELECTRIC CASTLE</span>

            <h1 class="ec-slide__headline">
                {{ 'plan.story.share.headline' | transloco }}
            </h1>

            <p class="ec-slide__body">
                {{ 'plan.story.share.body' | transloco }}
            </p>

            <button
                type="button"
                class="ec-slide__cta-primary"
                (click)="onCopy()"
            >
                {{ copied() ? ('plan.story.share.copied' | transloco) : ('plan.story.share.copyCta' | transloco) }}
            </button>

            @if (canShare()) {
                <button
                    type="button"
                    class="ec-slide__cta-secondary"
                    (click)="onShare()"
                >
                    {{ 'plan.story.share.shareCta' | transloco }}
                </button>
            }

            <button
                type="button"
                class="ec-slide__replay"
                (click)="replay.emit()"
            >
                <i class="pi pi-replay ec-slide__replay-icon" aria-hidden="true"></i>
                {{ 'plan.story.share.replayCta' | transloco }}
            </button>
        </div>
    `,
    styles: `
        :host {
            display: contents;
        }
        .ec-slide {
            position: absolute;
            inset: 0;
            background-color: var(--ec-red);
            display: flex;
            flex-direction: column;
            align-items: center;
            justify-content: center;
            padding: var(--space-12) var(--space-6);
            gap: var(--space-4);
            text-align: center;
        }
        .ec-slide__wordmark {
            font-size: 28px;
            font-weight: 900;
            color: var(--ec-yellow);
            letter-spacing: 0.06em;
            text-transform: uppercase;
        }
        .ec-slide__headline {
            margin: var(--space-4) 0 0;
            font-size: 34px;
            font-weight: 800;
            text-transform: uppercase;
            line-height: 1.1;
            color: var(--ec-white);
        }
        .ec-slide__body {
            margin: 0;
            font-size: 14px;
            line-height: 1.55;
            color: rgba(255, 255, 255, 0.92);
            max-width: 320px;
        }
        .ec-slide__cta-primary {
            margin-top: var(--space-5);
            width: 100%;
            max-width: 320px;
            min-height: 56px;
            padding: var(--space-3) var(--space-4);
            background-color: var(--ec-yellow);
            color: var(--ec-dark-navy);
            border: 0;
            border-radius: var(--radius-none);
            font-size: 14px;
            font-weight: 800;
            text-transform: uppercase;
            letter-spacing: 0.1em;
            cursor: pointer;
            transition: filter var(--duration-fast) var(--ease-out);
        }
        .ec-slide__cta-primary:hover:not(:disabled) {
            filter: brightness(0.93);
        }
        .ec-slide__cta-secondary {
            background: transparent;
            border: 1px solid var(--ec-white);
            border-radius: var(--radius-none);
            color: var(--ec-white);
            padding: var(--space-2) var(--space-4);
            font-size: 13px;
            font-weight: 700;
            text-transform: uppercase;
            letter-spacing: 0.08em;
            cursor: pointer;
        }
        .ec-slide__cta-secondary:hover {
            background-color: rgba(255, 255, 255, 0.12);
        }
        .ec-slide__replay {
            margin-top: var(--space-3);
            background: transparent;
            border: 0;
            color: rgba(255, 255, 255, 0.85);
            font-size: 12px;
            font-weight: 700;
            text-transform: uppercase;
            letter-spacing: 0.08em;
            cursor: pointer;
            display: inline-flex;
            align-items: center;
            gap: var(--space-2);
            padding: var(--space-2);
        }
        .ec-slide__replay-icon {
            font-size: 14px;
        }
    `,
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ShareSlideComponent {
    private readonly transloco = inject(TranslocoService);

    readonly slide = input.required<PlanShareSlide>();

    readonly replay = output<void>();

    protected readonly copied = signal(false);

    protected canShare(): boolean {
        return typeof navigator !== 'undefined' && typeof navigator.share === 'function';
    }

    protected async onCopy(): Promise<void> {
        const url = this.buildShareUrl();
        try {
            await navigator.clipboard.writeText(url);
            this.copied.set(true);
            setTimeout(() => this.copied.set(false), 2000);
        } catch {
            // clipboard API blocked; fall through silently
        }
    }

    protected async onShare(): Promise<void> {
        const url = this.buildShareUrl();
        const text = this.transloco.translate('plan.story.share.shareText');
        try {
            await navigator.share({ text, url });
        } catch {
            // user cancelled or share rejected; ignore
        }
    }

    private buildShareUrl(): string {
        return `${window.location.origin}/p/${this.slide().uuid}`;
    }
}
