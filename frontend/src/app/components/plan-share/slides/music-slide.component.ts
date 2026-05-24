import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { TranslocoModule } from '@jsverse/transloco';

import { PLACEHOLDER_ARTIST_IMAGE, resolveArtistImageByName } from '@shared/api/artists';

import { SlideLoopFadeDirective } from './loop-fade.directive';

import { PlanMusicSlide } from '../plan-share.model';

interface ArtistRow {
    readonly rank: number;
    readonly name: string;
    readonly imagePath: string;
}

const MAX_ARTISTS = 5;

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

            <ol class="ec-slide__lineup">
                @for (row of artistRows(); track row.rank) {
                    <li class="ec-slide__lineup-row">
                        <span class="ec-slide__rank" aria-hidden="true">{{ row.rank }}</span>
                        <span class="ec-slide__avatar">
                            <img
                                class="ec-slide__avatar-img"
                                [src]="row.imagePath"
                                [alt]="row.name"
                                loading="lazy"
                                (error)="onImgError($event)"
                            />
                        </span>
                        <span class="ec-slide__artist">{{ row.name }}</span>
                    </li>
                }
            </ol>

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
        .ec-slide__lineup,
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
        .ec-slide__lineup {
            list-style: none;
            margin: var(--space-5) 0 0;
            padding: 0;
            display: flex;
            flex-direction: column;
            align-items: stretch;
            gap: var(--space-3);
            width: 100%;
            max-width: 340px;
        }
        .ec-slide__lineup-row {
            display: grid;
            grid-template-columns: 32px 56px 1fr;
            align-items: center;
            gap: var(--space-3);
            text-align: left;
            animation: ec-slide-fade-up var(--duration-deliberate) var(--ease-emphasized) both;
        }
        .ec-slide__lineup-row:nth-child(1) { animation-delay: 80ms; }
        .ec-slide__lineup-row:nth-child(2) { animation-delay: 140ms; }
        .ec-slide__lineup-row:nth-child(3) { animation-delay: 200ms; }
        .ec-slide__lineup-row:nth-child(4) { animation-delay: 260ms; }
        .ec-slide__lineup-row:nth-child(5) { animation-delay: 320ms; }
        .ec-slide__rank {
            font-size: 28px;
            font-weight: 900;
            font-style: italic;
            color: var(--ec-yellow);
            text-align: center;
            line-height: 1;
            letter-spacing: 0.02em;
        }
        .ec-slide__avatar {
            display: block;
            width: 56px;
            height: 56px;
            background-color: var(--ec-dark-navy);
            border: 1px solid rgba(255, 255, 255, 0.18);
            border-radius: var(--radius-none);
            overflow: hidden;
        }
        .ec-slide__avatar-img {
            display: block;
            width: 100%;
            height: 100%;
            object-fit: cover;
        }
        .ec-slide__artist {
            font-size: 20px;
            font-weight: 800;
            font-style: italic;
            color: var(--ec-white);
            text-transform: uppercase;
            letter-spacing: 0.02em;
            line-height: 1.15;
            min-width: 0;
            word-break: break-word;
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
            .ec-slide__lineup-row {
                animation: none;
            }
        }
    `,
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MusicSlideComponent {
    readonly slide = input.required<PlanMusicSlide>();

    protected readonly artistRows = computed<ReadonlyArray<ArtistRow>>(() =>
        this.slide()
            .artists.slice(0, MAX_ARTISTS)
            .map((name, index) => ({
                rank: index + 1,
                name,
                imagePath: resolveArtistImageByName(name),
            })),
    );

    protected genresLabel(): string {
        return this.slide().genres.join(' & ');
    }

    protected onImgError(event: Event): void {
        const target = event.target;
        if (target instanceof HTMLImageElement && target.src !== PLACEHOLDER_ARTIST_IMAGE) {
            target.src = PLACEHOLDER_ARTIST_IMAGE;
        }
    }
}
