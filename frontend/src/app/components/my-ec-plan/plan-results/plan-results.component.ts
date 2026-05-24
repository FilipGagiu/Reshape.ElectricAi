import {
    ChangeDetectionStrategy,
    Component,
    DestroyRef,
    Signal,
    computed,
    effect,
    inject,
    isDevMode,
    signal,
} from '@angular/core';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { TranslocoModule, TranslocoService } from '@jsverse/transloco';

import { EcTopbarComponent } from '@shared/components/ec-topbar/ec-topbar.component';
import { ItineraryApi } from '@shared/api/itinerary-api';
import { ItineraryStore } from '@shared/api/itinerary-store';
import { TopArtistsApi } from '@shared/api/top-artists-api';
import {
    GreetingSectionData,
    ItineraryResponse,
    ItinerarySectionDto,
    RetrievedItem,
} from '@shared/api/dto/itinerary.dto';
import { TopArtistRow } from '@shared/api/dto/top-artists.dto';
import { PreferencesDto } from '@shared/api/dto/preferences.dto';
import {
    PLACEHOLDER_ARTIST_IMAGE,
    resolveArtistImageByName,
} from '@shared/api/artists';
import { AuthService } from '@shared/services/auth.service';
import { PlanOnboardingService } from '@shared/services/plan-onboarding.service';

import { itineraryToPlanData } from '@components/plan-share/itinerary-to-plan.mapper';
import { PlanData } from '@components/plan-share/plan-share.model';
import { StoriesViewerComponent } from '@components/plan-share/stories-viewer.component';

import { PlanIntakeService } from '../plan-intake/services/plan-intake.service';
import {
    ParsedSection,
    cleanSnippet,
    cleanTitle,
    formatEventTime,
    parseSection,
} from './parse-section';

interface RenderItem {
    readonly id: string;
    readonly title: string;
    readonly snippet: string;
    readonly time: string;
}

type RenderedSection =
    | {
          readonly kind: 'transport';
          readonly mode: string | null;
          readonly note: string | null;
          readonly isEmpty: boolean;
      }
    | {
          readonly kind: 'vibeActivities';
          readonly vibeTags: ReadonlyArray<string>;
          readonly items: ReadonlyArray<RenderItem>;
      }
    | {
          readonly kind: 'food';
          readonly restrictions: ReadonlyArray<string>;
          readonly items: ReadonlyArray<RenderItem>;
          readonly cuisines: ReadonlyArray<string>;
      }
    | {
          readonly kind: 'accommodation';
          readonly type: string | null;
          readonly note: string | null;
          readonly isEmpty: boolean;
      };

interface HeroSnapshot {
    readonly headline: string;
    readonly origin: string | null;
    readonly crewLabelKey: string | null;
    readonly crewLabelParams: { count: number } | null;
}

@Component({
    selector: 'app-plan-results',
    imports: [EcTopbarComponent, TranslocoModule, StoriesViewerComponent, ReactiveFormsModule],
    templateUrl: './plan-results.component.html',
    styleUrl: './plan-results.component.css',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PlanResultsComponent {
    private readonly store = inject(ItineraryStore);
    private readonly itineraryApi = inject(ItineraryApi);
    private readonly topArtistsApi = inject(TopArtistsApi);
    private readonly destroyRef = inject(DestroyRef);
    private readonly router = inject(Router);
    private readonly route = inject(ActivatedRoute);
    private readonly transloco = inject(TranslocoService);
    private readonly auth = inject(AuthService);
    private readonly planOnboarding = inject(PlanOnboardingService);
    private readonly planIntake = inject(PlanIntakeService);

    private readonly routeParams = toSignal(this.route.paramMap, {
        initialValue: this.route.snapshot.paramMap,
    });
    protected readonly planUuid = computed<string | null>(() => this.routeParams().get('uuid'));
    protected readonly readOnly = computed(() => this.planUuid() !== null);

    private readonly sharedResponse = signal<ItineraryResponse | null>(null);
    private readonly sharedNotFound = signal(false);

    protected readonly isDevMode = isDevMode();
    protected readonly submissionError = computed(
        () => !this.readOnly() && this.planIntake.status() === 'error',
    );
    protected readonly retrying = signal(false);
    protected readonly storyOpen = signal(false);
    protected readonly refreshing = signal(false);
    protected readonly refreshFailed = signal(false);

    private readonly currentResponse = computed<ItineraryResponse | null>(() =>
        this.readOnly() ? this.sharedResponse() : this.store.itinerary(),
    );

    protected readonly hasItinerary = computed(() => this.currentResponse() !== null);
    protected readonly preferences: Signal<PreferencesDto | null> = computed(
        () => this.currentResponse()?.preferences ?? null,
    );
    protected readonly sections: Signal<ReadonlyArray<ItinerarySectionDto>> = computed(
        () => this.currentResponse()?.itinerary.sections ?? [],
    );
    private readonly topArtistsFromApi = signal<ReadonlyArray<TopArtistRow>>([]);
    private readonly topArtistsFromItinerary = computed<ReadonlyArray<TopArtistRow>>(() => {
        const section = this.parsedSections().find(
            (entry): entry is Extract<ParsedSection, { kind: 'topArtists' }> =>
                entry.kind === 'topArtists',
        );
        const names = (section?.data.topOverall ?? []).map((item) => cleanTitle(item.title));
        return this.toTopArtistRows(names);
    });
    protected readonly topArtists = computed<ReadonlyArray<TopArtistRow>>(() =>
        this.auth.isAuthenticated() ? this.topArtistsFromApi() : this.topArtistsFromItinerary(),
    );
    private readonly topArtistNames = computed<ReadonlyArray<string>>(() =>
        this.topArtists().map((row) => row.name),
    );
    protected readonly storyPlanData = computed<PlanData | null>(() =>
        itineraryToPlanData(this.currentResponse(), this.topArtistNames()),
    );
    protected readonly notFound = computed(
        () => this.readOnly() && this.sharedNotFound() && !this.sharedResponse(),
    );

    protected readonly shareUrl = computed<string | null>(() => {
        const id = this.currentResponse()?.itinerary?.id;
        if (!id || typeof window === 'undefined') return null;
        return `${window.location.origin}/plan/${id}`;
    });
    protected readonly canNativeShare =
        typeof navigator !== 'undefined' && typeof navigator.share === 'function';
    protected readonly copied = signal(false);
    private copiedResetHandle: ReturnType<typeof setTimeout> | null = null;

    protected readonly refineOpen = signal(false);
    protected readonly refineSubmitting = signal(false);
    protected readonly refineError = signal(false);
    protected readonly refineControl = new FormControl<string>('', { nonNullable: true });
    protected readonly canRefine = computed(() => !this.readOnly() && !!this.currentResponse()?.itinerary?.id);

    private readonly parsedSections = computed<ReadonlyArray<ParsedSection>>(() =>
        this.sections()
            .map((section) => parseSection(section))
            .filter((parsed): parsed is ParsedSection => parsed !== null),
    );

    protected readonly hero = computed<HeroSnapshot>(() => {
        const greeting = this.parsedSections().find(
            (entry): entry is Extract<ParsedSection, { kind: 'greeting' }> =>
                entry.kind === 'greeting',
        );
        const preferences = this.preferences();
        return this.buildHero(greeting?.data ?? null, preferences);
    });

    private readonly bodySections = computed<ReadonlyArray<ParsedSection>>(() =>
        this.parsedSections().filter((entry) => entry.kind !== 'greeting'),
    );

    protected readonly renderedBodySections = computed<ReadonlyArray<RenderedSection>>(() => {
        const locale = this.transloco.getActiveLang();
        return this.bodySections()
            .map((section): RenderedSection | null => this.toRenderedSection(section, locale))
            .filter((entry): entry is RenderedSection => entry !== null);
    });

    constructor() {
        effect(() => {
            const uuid = this.planUuid();
            this.refreshing.set(true);
            this.refreshFailed.set(false);
            if (uuid) {
                this.sharedNotFound.set(false);
                this.topArtistsFromApi.set([]);
            } else if (this.auth.isAuthenticated()) {
                this.loadTopArtists();
            } else {
                this.topArtistsFromApi.set([]);
            }
            const source$ = uuid
                ? this.itineraryApi.getById(uuid)
                : this.itineraryApi.getCurrent();
            source$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
                next: (response) => {
                    if (uuid) {
                        this.sharedResponse.set(response ?? null);
                        this.sharedNotFound.set(!response);
                    } else if (response) {
                        this.store.set(response);
                    }
                    this.refreshing.set(false);
                },
                error: () => {
                    if (uuid) {
                        this.sharedResponse.set(null);
                        this.sharedNotFound.set(true);
                    } else {
                        this.refreshFailed.set(true);
                    }
                    this.refreshing.set(false);
                },
            });
        });
    }

    private mapItems(
        items: ReadonlyArray<RetrievedItem>,
        locale: string,
    ): ReadonlyArray<RenderItem> {
        return items.map((item) => ({
            id: item.id,
            title: cleanTitle(item.title),
            snippet: cleanSnippet(item.snippet),
            time: formatEventTime(item.eventUtc, locale),
        }));
    }

    private loadTopArtists(): void {
        this.topArtistsApi
            .getTopArtists()
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: (artists) => this.topArtistsFromApi.set(this.toTopArtistRows(artists)),
                error: () => this.topArtistsFromApi.set([]),
            });
    }

    private toTopArtistRows(names: ReadonlyArray<string>): ReadonlyArray<TopArtistRow> {
        return names.slice(0, 5).map((name, index) => ({
            rank: index + 1,
            name,
            imagePath: resolveArtistImageByName(name),
        }));
    }

    private toRenderedSection(section: ParsedSection, locale: string): RenderedSection | null {
        switch (section.kind) {
            case 'transport': {
                const isEmpty = !section.data.mode && !section.data.note;
                return {
                    kind: 'transport',
                    mode: section.data.mode,
                    note: section.data.note,
                    isEmpty,
                };
            }
            case 'vibeActivities':
                return {
                    kind: 'vibeActivities',
                    vibeTags: section.data.vibeTags ?? [],
                    items: this.mapItems(section.data.topActivities ?? [], locale),
                };
            case 'food':
                return {
                    kind: 'food',
                    restrictions: section.data.restrictions ?? [],
                    items: this.mapItems(section.data.topRestaurants ?? [], locale),
                    cuisines: (section.data.preferredCuisines ?? []).slice(0, 5),
                };
            case 'topArtists':
                return null;
            case 'accommodation': {
                const isEmpty = !section.data.type && !section.data.note;
                return {
                    kind: 'accommodation',
                    type: section.data.type,
                    note: section.data.note,
                    isEmpty,
                };
            }
            default:
                return null;
        }
    }

    protected onArtistImgError(event: Event): void {
        const target = event.target;
        if (target instanceof HTMLImageElement && target.src !== PLACEHOLDER_ARTIST_IMAGE) {
            target.src = PLACEHOLDER_ARTIST_IMAGE;
        }
    }

    protected openStory(): void {
        if (!this.storyPlanData()) return;
        this.storyOpen.set(true);
    }

    protected closeStory(): void {
        this.storyOpen.set(false);
    }

    protected async sharePlan(): Promise<void> {
        const url = this.shareUrl();
        if (!url) return;
        if (this.canNativeShare) {
            try {
                await navigator.share({
                    title: this.transloco.translate('plan.results.share.shareTitle'),
                    text: this.transloco.translate('plan.results.share.shareText'),
                    url,
                });
                return;
            } catch {
                // user cancelled or share rejected; fall through to copy.
            }
        }
        await this.copyShareLink();
    }

    protected async copyShareLink(): Promise<void> {
        const url = this.shareUrl();
        if (!url) return;
        try {
            await navigator.clipboard.writeText(url);
        } catch {
            return;
        }
        this.copied.set(true);
        if (this.copiedResetHandle) clearTimeout(this.copiedResetHandle);
        this.copiedResetHandle = setTimeout(() => {
            this.copied.set(false);
            this.copiedResetHandle = null;
        }, 2000);
    }

    protected redoWizard(): void {
        this.planOnboarding.clearCompleted(this.auth.currentUser()?.email);
        void this.router.navigateByUrl('/plan');
    }

    protected async retrySubmission(): Promise<void> {
        if (this.retrying()) return;
        this.retrying.set(true);
        try {
            await this.planIntake.retrySubmit();
        } finally {
            this.retrying.set(false);
        }
    }

    protected toggleRefine(): void {
        this.refineOpen.update((open) => !open);
        if (this.refineOpen()) {
            this.refineError.set(false);
        }
    }

    protected async submitRefine(): Promise<void> {
        const itineraryId = this.currentResponse()?.itinerary?.id;
        const freeText = this.refineControl.value.trim();
        if (!itineraryId || !freeText || this.refineSubmitting()) return;
        this.refineSubmitting.set(true);
        this.refineError.set(false);
        try {
            const response = await firstValueFrom(
                this.itineraryApi.refine({
                    locale: this.transloco.getActiveLang() ?? null,
                    itineraryId,
                    freeText,
                }),
            );
            if (response) {
                this.store.set(response);
            }
            this.refineControl.setValue('', { emitEvent: false });
            this.refineOpen.set(false);
        } catch {
            this.refineError.set(true);
        } finally {
            this.refineSubmitting.set(false);
        }
    }

    private buildHero(
        greeting: GreetingSectionData | null,
        preferences: PreferencesDto | null,
    ): HeroSnapshot {
        const origin = greeting?.origin?.trim() || preferences?.origin?.trim() || null;
        const crewKind = greeting?.crew?.kind ?? preferences?.crew?.kind ?? null;
        const crewSize = greeting?.crew?.size ?? preferences?.crew?.estimatedSize ?? null;
        const headline = this.transloco.translate('plan.results.hero.namelessGreeting');

        if (crewKind === 'Solo' || crewKind === 'solo') {
            return {
                headline,
                origin,
                crewLabelKey: 'plan.results.hero.soloFallback',
                crewLabelParams: null,
            };
        }
        if (typeof crewSize === 'number' && crewSize > 0) {
            return {
                headline,
                origin,
                crewLabelKey: 'plan.results.hero.crewLabel',
                crewLabelParams: { count: crewSize },
            };
        }
        return { headline, origin, crewLabelKey: null, crewLabelParams: null };
    }
}
