import { ChangeDetectionStrategy, Component, DestroyRef, Signal, computed, inject, isDevMode, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Router } from '@angular/router';
import { TranslocoModule, TranslocoService } from '@jsverse/transloco';

import { EcTopbarComponent } from '@shared/components/ec-topbar/ec-topbar.component';
import { ItineraryApi } from '@shared/api/itinerary-api';
import { ItineraryStore } from '@shared/api/itinerary-store';
import {
    Accommodation,
    ActivityType,
    AgeGroup,
    CrewKind,
    Cuisine,
    FoodRestriction,
    MusicGenre,
    PreferencesDto,
    TicketType,
    TransportMode,
} from '@shared/api/dto/preferences.dto';
import { ItinerarySectionDto } from '@shared/api/dto/itinerary.dto';
import { AuthService } from '@shared/services/auth.service';
import { PlanOnboardingService } from '@shared/services/plan-onboarding.service';

import { MOCK_PLAN_UUID } from '@components/plan-share/plan-share.model';
import { PlanIntakeService } from '../plan-intake/services/plan-intake.service';

const ENUM_KEY_PREFIX = 'plan.results.enum';

const CREW_LABEL_KEYS: Readonly<Record<CrewKind, string>> = {
    Solo: `${ENUM_KEY_PREFIX}.crew.Solo`,
    WithGroup: `${ENUM_KEY_PREFIX}.crew.WithGroup`,
};

const TRANSPORT_LABEL_KEYS: Partial<Record<TransportMode, string>> = {
    RideShare: `${ENUM_KEY_PREFIX}.transport.RideShare`,
    Car: `${ENUM_KEY_PREFIX}.transport.Car`,
    EcTrain: `${ENUM_KEY_PREFIX}.transport.EcTrain`,
    EcBus: `${ENUM_KEY_PREFIX}.transport.EcBus`,
    Helicopter: `${ENUM_KEY_PREFIX}.transport.Helicopter`,
};

const ACCOMMODATION_LABEL_KEYS: Partial<Record<Accommodation, string>> = {
    VillageRental: `${ENUM_KEY_PREFIX}.accommodation.VillageRental`,
    Camping: `${ENUM_KEY_PREFIX}.accommodation.Camping`,
    CarCamping: `${ENUM_KEY_PREFIX}.accommodation.CarCamping`,
    RvCamping: `${ENUM_KEY_PREFIX}.accommodation.RvCamping`,
    Glamping: `${ENUM_KEY_PREFIX}.accommodation.Glamping`,
};

const TICKET_LABEL_KEYS: Readonly<Record<TicketType, string>> = {
    Standard: `${ENUM_KEY_PREFIX}.ticket.Standard`,
    Vip: `${ENUM_KEY_PREFIX}.ticket.Vip`,
    UltraVip: `${ENUM_KEY_PREFIX}.ticket.UltraVip`,
    Black: `${ENUM_KEY_PREFIX}.ticket.Black`,
};

const AGE_LABEL_KEYS: Readonly<Record<AgeGroup, string>> = {
    Under18: `${ENUM_KEY_PREFIX}.age.Under18`,
    Adult18To24: `${ENUM_KEY_PREFIX}.age.Adult18To24`,
    Adult25To34: `${ENUM_KEY_PREFIX}.age.Adult25To34`,
    Adult35To44: `${ENUM_KEY_PREFIX}.age.Adult35To44`,
    Adult45Plus: `${ENUM_KEY_PREFIX}.age.Adult45Plus`,
};

const MUSIC_LABEL_KEYS: Partial<Record<MusicGenre, string>> = {
    HipHop: `${ENUM_KEY_PREFIX}.music.HipHop`,
    House: `${ENUM_KEY_PREFIX}.music.House`,
    Balkan: `${ENUM_KEY_PREFIX}.music.Balkan`,
    Rock: `${ENUM_KEY_PREFIX}.music.Rock`,
    Folk: `${ENUM_KEY_PREFIX}.music.Folk`,
    Techno: `${ENUM_KEY_PREFIX}.music.Techno`,
    Pop: `${ENUM_KEY_PREFIX}.music.Pop`,
    Electronic: `${ENUM_KEY_PREFIX}.music.Electronic`,
    Jazz: `${ENUM_KEY_PREFIX}.music.Jazz`,
    Metal: `${ENUM_KEY_PREFIX}.music.Metal`,
    Other: `${ENUM_KEY_PREFIX}.music.Other`,
};

const FOOD_LABEL_KEYS: Readonly<Record<FoodRestriction, string>> = {
    Vegan: `${ENUM_KEY_PREFIX}.foodRestriction.Vegan`,
    Vegetarian: `${ENUM_KEY_PREFIX}.foodRestriction.Vegetarian`,
    NoPeanuts: `${ENUM_KEY_PREFIX}.foodRestriction.NoPeanuts`,
    NoMeat: `${ENUM_KEY_PREFIX}.foodRestriction.NoMeat`,
    NoPork: `${ENUM_KEY_PREFIX}.foodRestriction.NoPork`,
    NoDairy: `${ENUM_KEY_PREFIX}.foodRestriction.NoDairy`,
    NoGluten: `${ENUM_KEY_PREFIX}.foodRestriction.NoGluten`,
    NoShellfish: `${ENUM_KEY_PREFIX}.foodRestriction.NoShellfish`,
    NoEggs: `${ENUM_KEY_PREFIX}.foodRestriction.NoEggs`,
    Halal: `${ENUM_KEY_PREFIX}.foodRestriction.Halal`,
    Kosher: `${ENUM_KEY_PREFIX}.foodRestriction.Kosher`,
};

const CUISINE_LABEL_KEYS: Partial<Record<Cuisine, string>> = {
    American: `${ENUM_KEY_PREFIX}.cuisine.American`,
    Italian: `${ENUM_KEY_PREFIX}.cuisine.Italian`,
    Romanian: `${ENUM_KEY_PREFIX}.cuisine.Romanian`,
    Mexican: `${ENUM_KEY_PREFIX}.cuisine.Mexican`,
    Chinese: `${ENUM_KEY_PREFIX}.cuisine.Chinese`,
    Japanese: `${ENUM_KEY_PREFIX}.cuisine.Japanese`,
    Indian: `${ENUM_KEY_PREFIX}.cuisine.Indian`,
    Thai: `${ENUM_KEY_PREFIX}.cuisine.Thai`,
    French: `${ENUM_KEY_PREFIX}.cuisine.French`,
    Greek: `${ENUM_KEY_PREFIX}.cuisine.Greek`,
    Mediterranean: `${ENUM_KEY_PREFIX}.cuisine.Mediterranean`,
    MiddleEastern: `${ENUM_KEY_PREFIX}.cuisine.MiddleEastern`,
    Bbq: `${ENUM_KEY_PREFIX}.cuisine.Bbq`,
    StreetFood: `${ENUM_KEY_PREFIX}.cuisine.StreetFood`,
    Other: `${ENUM_KEY_PREFIX}.cuisine.Other`,
};

const ACTIVITY_LABEL_KEYS: Readonly<Record<ActivityType, string>> = {
    Relax: `${ENUM_KEY_PREFIX}.activity.Relax`,
    Energetic: `${ENUM_KEY_PREFIX}.activity.Energetic`,
    Adrenaline: `${ENUM_KEY_PREFIX}.activity.Adrenaline`,
    Social: `${ENUM_KEY_PREFIX}.activity.Social`,
    Creative: `${ENUM_KEY_PREFIX}.activity.Creative`,
    Wellness: `${ENUM_KEY_PREFIX}.activity.Wellness`,
    Discovery: `${ENUM_KEY_PREFIX}.activity.Discovery`,
};

interface LabelledValue {
    readonly raw: string;
    readonly labelKey: string | null;
}

interface SectionPreview {
    readonly key: string | null;
    readonly diagnostic: string | null;
    readonly rawJson: string;
}

@Component({
    selector: 'app-plan-results',
    imports: [EcTopbarComponent, TranslocoModule],
    templateUrl: './plan-results.component.html',
    styleUrl: './plan-results.component.css',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PlanResultsComponent {
    private readonly store = inject(ItineraryStore);
    private readonly itineraryApi = inject(ItineraryApi);
    private readonly destroyRef = inject(DestroyRef);
    private readonly router = inject(Router);
    private readonly transloco = inject(TranslocoService);
    private readonly auth = inject(AuthService);
    private readonly planOnboarding = inject(PlanOnboardingService);
    private readonly planIntake = inject(PlanIntakeService);

    protected readonly isDevMode = isDevMode();
    protected readonly submissionError = computed(() => this.planIntake.status() === 'error');
    protected readonly retrying = signal(false);
    protected readonly showRawSections = signal(false);
    protected readonly storyHref = `/p/${MOCK_PLAN_UUID}`;
    protected readonly refreshing = signal(false);
    protected readonly refreshFailed = signal(false);

    protected readonly hasItinerary = this.store.hasItinerary;
    protected readonly preferences: Signal<PreferencesDto | null> = this.store.preferences;
    protected readonly sections: Signal<ReadonlyArray<ItinerarySectionDto>> = this.store.sections;

    protected readonly completionPercent = computed(() => {
        const value = this.preferences()?.completionPercent ?? 0;
        if (value <= 1) return Math.round(value * 100);
        return Math.round(value);
    });

    protected readonly displayName = computed(() => this.preferences()?.name?.trim() || null);

    protected readonly crewLabel = computed(() => {
        const crew = this.preferences()?.crew;
        if (!crew) return null;
        return this.resolveLabel(crew.kind, CREW_LABEL_KEYS as Partial<Record<string, string>>);
    });

    protected readonly transportLabel = computed(() => {
        const mode = this.preferences()?.suggestedTransport?.mode;
        if (!mode) return null;
        return this.resolveLabel(mode, TRANSPORT_LABEL_KEYS as Partial<Record<string, string>>);
    });

    protected readonly accommodationLabel = computed(() => {
        const type = this.preferences()?.suggestedAccommodation?.type;
        if (!type) return null;
        return this.resolveLabel(type, ACCOMMODATION_LABEL_KEYS as Partial<Record<string, string>>);
    });

    protected readonly ticketLabel = computed(() => {
        const ticket = this.preferences()?.ticketType;
        if (!ticket) return null;
        return this.resolveLabel(ticket, TICKET_LABEL_KEYS as Partial<Record<string, string>>);
    });

    protected readonly ageLabel = computed(() => {
        const age = this.preferences()?.ageGroup;
        if (!age) return null;
        return this.resolveLabel(age, AGE_LABEL_KEYS as Partial<Record<string, string>>);
    });

    protected readonly musicLabels = computed<ReadonlyArray<LabelledValue>>(() =>
        this.mapLabels(this.preferences()?.musicGenres, MUSIC_LABEL_KEYS as Partial<Record<string, string>>),
    );

    protected readonly foodRestrictionLabels = computed<ReadonlyArray<LabelledValue>>(() =>
        this.mapLabels(this.preferences()?.foodRestrictions, FOOD_LABEL_KEYS as Partial<Record<string, string>>),
    );

    protected readonly cuisineLabels = computed<ReadonlyArray<LabelledValue>>(() =>
        this.mapLabels(this.preferences()?.cuisines, CUISINE_LABEL_KEYS as Partial<Record<string, string>>),
    );

    protected readonly activityLabels = computed<ReadonlyArray<LabelledValue>>(() =>
        this.mapLabels(this.preferences()?.activityInterests, ACTIVITY_LABEL_KEYS as Partial<Record<string, string>>),
    );

    protected readonly vibeTags = computed<ReadonlyArray<string>>(
        () => this.preferences()?.vibeTags?.filter((entry): entry is string => !!entry?.trim()) ?? [],
    );

    protected readonly mustSeeArtists = computed<ReadonlyArray<string>>(
        () => this.preferences()?.mustSeeArtists?.filter((entry): entry is string => !!entry?.trim()) ?? [],
    );

    protected readonly sectionPreviews = computed<ReadonlyArray<SectionPreview>>(() =>
        this.sections().map((section) => ({
            key: section.key,
            diagnostic: section.diagnostic,
            rawJson: this.safeStringify(section.data),
        })),
    );

    constructor() {
        this.refreshing.set(true);
        this.itineraryApi
            .getCurrent()
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: (response) => {
                    if (response) {
                        this.store.set(response);
                        this.refreshFailed.set(false);
                    }
                    this.refreshing.set(false);
                },
                error: () => {
                    this.refreshFailed.set(true);
                    this.refreshing.set(false);
                },
            });
    }

    protected resolveLabelText(value: LabelledValue): string {
        if (!value.labelKey) return value.raw;
        const translated = this.transloco.translate(value.labelKey);
        return translated === value.labelKey ? value.raw : translated;
    }

    protected toggleRawSections(): void {
        this.showRawSections.update((current) => !current);
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

    private resolveLabel(raw: string, lookup: Partial<Record<string, string>>): LabelledValue {
        return { raw, labelKey: lookup[raw] ?? null };
    }

    private mapLabels(
        values: ReadonlyArray<string> | undefined,
        lookup: Partial<Record<string, string>>,
    ): ReadonlyArray<LabelledValue> {
        if (!values?.length) return [];
        return values.map((value) => this.resolveLabel(value, lookup));
    }

    private safeStringify(value: unknown): string {
        try {
            return JSON.stringify(value, null, 2);
        } catch {
            return '';
        }
    }
}
