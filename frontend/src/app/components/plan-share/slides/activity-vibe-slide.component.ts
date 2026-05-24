import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { TranslocoModule } from '@jsverse/transloco';

import { SlideLoopFadeDirective } from './loop-fade.directive';

import { PlanActivityVibeSlide } from '../plan-share.model';

/**
 * Activity & vibe slide: what to do beyond the music, plus a one-word vibe
 * descriptor. Yellow-tinted scrim over the slide_6_bg.mp4 background.
 */
@Component({
    selector: 'app-activity-vibe-slide',
    imports: [TranslocoModule, SlideLoopFadeDirective],
    template: `
        <div class="ec-slide">
            <video
                class="ec-slide__video" appSlideLoopFade
                src="/media/sliders/slide_6_bg.mp4"
                autoplay
                muted
                loop
                playsinline
                disablepictureinpicture
                aria-hidden="true"
            ></video>
            <span class="ec-slide__scrim" aria-hidden="true"></span>

            <h1 class="ec-slide__headline">
                {{ 'plan.story.activityVibe.headline' | transloco }}
            </h1>
            <span class="ec-slide__accent" aria-hidden="true"></span>

            <div class="ec-slide__chips">
                @for (key of slide().activityKeys; track key) {
                    <span class="ec-slide__chip">
                        {{ 'plan.story.activityVibe.activity.' + key | transloco }}
                    </span>
                }
            </div>

            <p class="ec-slide__vibe">
                {{ 'plan.story.activityVibe.vibe.' + slide().vibeKey | transloco }}
            </p>

            <p class="ec-slide__body">
                {{ 'plan.story.activityVibe.body' | transloco }}
            </p>
        </div>
    `,
    styles: `
        :host {
            display: contents;
        }
        .ec-slide {
            position: absolute;
            inset: 0;
            background-color: var(--ec-dark-navy);
            display: flex;
            flex-direction: column;
            align-items: center;
            justify-content: center;
            padding: var(--space-12) var(--space-6);
            gap: var(--space-3);
            text-align: center;
            overflow: hidden;
            isolation: isolate;
        }
        .ec-slide__video {
            position: absolute;
            inset: 0;
            width: 100%;
            height: 100%;
            object-fit: cover;
            filter: blur(3px);
            transform: scale(1.04);
            z-index: 0;
            pointer-events: none;
        }
        .ec-slide__scrim {
            position: absolute;
            inset: 0;
            background-color: rgb(8 119 114 / 60%);
            z-index: 1;
            pointer-events: none;
            filter: contrast(5);
        }
        .ec-slide__headline,
        .ec-slide__accent,
        .ec-slide__chips,
        .ec-slide__vibe,
        .ec-slide__body {
            position: relative;
            z-index: 2;
        }
        .ec-slide__headline {
            margin: 0;
            font-size: 30px;
            font-weight: 800;
            text-transform: uppercase;
            line-height: 1.1;
            color: var(--ec-white);
            max-width: 320px;
        }
        .ec-slide__accent {
            display: block;
            width: 64px;
            height: 2px;
            background-color: var(--ec-red);
        }
        .ec-slide__chips {
            display: flex;
            flex-wrap: wrap;
            justify-content: center;
            gap: var(--space-2);
            margin-top: var(--space-3);
        }
        .ec-slide__chip {
            padding: var(--space-2) var(--space-4);
            background-color: var(--ec-dark-navy);
            color: var(--ec-white);
            border: 1px solid var(--ec-white);
            border-radius: var(--radius-none);
            font-size: 13px;
            font-weight: 700;
            text-transform: uppercase;
            letter-spacing: 0.04em;
        }
        .ec-slide__vibe {
            margin: var(--space-3) 0 0;
            font-size: 22px;
            font-weight: 900;
            font-style: italic;
            color: var(--ec-dark-navy);
            text-transform: uppercase;
            letter-spacing: 0.02em;
        }
        .ec-slide__body {
            margin: var(--space-2) 0 0;
            font-size: 14px;
            line-height: 1.55;
            color: rgba(15, 20, 40, 0.85);
            max-width: 320px;
        }
    `,
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ActivityVibeSlideComponent {
    readonly slide = input.required<PlanActivityVibeSlide>();
}
