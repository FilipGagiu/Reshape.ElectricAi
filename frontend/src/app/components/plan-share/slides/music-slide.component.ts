import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { TranslocoModule } from '@jsverse/transloco';

import { SlideLoopFadeDirective } from './loop-fade.directive';

import { PlanMusicSlide } from '../plan-share.model';

@Component({
    selector: 'app-music-slide',
    imports: [TranslocoModule, SlideLoopFadeDirective],
    template: `
        <div class="ec-slide">
            <video
                class="ec-slide__video" appSlideLoopFade
                src="/media/sliders/slide_4_bg.mp4"
                autoplay
                muted
                loop
                playsinline
                disablepictureinpicture
                aria-hidden="true"
            ></video>
            <span class="ec-slide__scrim" aria-hidden="true"></span>
            <h1 class="ec-slide__headline">
                {{ 'plan.story.music.headline' | transloco }}
            </h1>
            <span class="ec-slide__accent" aria-hidden="true"></span>

            <div class="ec-slide__artists">
                @for (artist of slide().artists; track artist; let isLast = $last) {
                    <span class="ec-slide__artist">{{ artist }}</span>
                    @if (!isLast) {
                        <span class="ec-slide__separator" aria-hidden="true">×</span>
                    }
                }
            </div>

            <p class="ec-slide__body">
                {{
                    'plan.story.music.bodySuffix'
                        | transloco
                            : {
                                  genres: genresLabel()
                              }
                }}
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
            background: linear-gradient(135deg, var(--ec-dark-navy) 0%, #1a2040 100%);
            display: flex;
            flex-direction: column;
            align-items: center;
            justify-content: center;
            padding: var(--space-12) var(--space-6);
            gap: var(--space-4);
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
            background-color: rgb(134 12 12 / 55%);
            z-index: 1;
            pointer-events: none;
            filter: contrast(5);
        }
        .ec-slide__headline,
        .ec-slide__accent,
        .ec-slide__artists,
        .ec-slide__body {
            position: relative;
            z-index: 2;
        }
        .ec-slide__headline {
            margin: 0;
            font-size: 36px;
            font-weight: 800;
            text-transform: uppercase;
            line-height: 1.1;
            color: var(--ec-white);
            animation: ec-slide-fade-up var(--duration-deliberate) var(--ease-emphasized) both;
        }
        .ec-slide__accent {
            display: block;
            width: 64px;
            height: 2px;
            background-color: var(--ec-red);
        }
        .ec-slide__artists {
            display: flex;
            flex-direction: column;
            align-items: center;
            gap: var(--space-1);
            margin-top: var(--space-6);
        }
        .ec-slide__artist {
            font-size: 26px;
            font-weight: 800;
            font-style: italic;
            color: var(--ec-yellow);
            text-transform: uppercase;
            letter-spacing: 0.02em;
            line-height: 1.1;
            animation: ec-slide-fade-up var(--duration-deliberate) var(--ease-emphasized) both;
        }
        .ec-slide__artist:nth-child(1) {
            animation-delay: 80ms;
        }
        .ec-slide__artist:nth-child(3) {
            animation-delay: 180ms;
        }
        .ec-slide__artist:nth-child(5) {
            animation-delay: 280ms;
        }
        .ec-slide__separator {
            font-size: 22px;
            color: rgba(255, 255, 255, 0.55);
            font-weight: 400;
        }
        .ec-slide__body {
            margin: var(--space-5) 0 0;
            font-size: 22px;
            font-weight: 700;
            line-height: 1.45;
            color: var(--ec-white);
            max-width: 340px;
        }
        @keyframes ec-slide-fade-up {
            from {
                opacity: 0;
                transform: translateY(16px);
            }
            to {
                opacity: 1;
                transform: translateY(0);
            }
        }
        @media (prefers-reduced-motion: reduce) {
            .ec-slide__headline,
            .ec-slide__artist {
                animation: none;
            }
        }
    `,
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MusicSlideComponent {
    readonly slide = input.required<PlanMusicSlide>();

    protected genresLabel(): string {
        return this.slide().genres.join(' & ');
    }
}
