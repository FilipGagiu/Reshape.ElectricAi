import { FeedCategory, FeedFilter, FeedUrgency } from './live-feed.model';

export interface CategoryMeta {
    readonly icon: string;
    readonly accent: string;
    readonly labelKey: string;
    readonly urgency: FeedUrgency;
}

export const CATEGORY_META: Readonly<Record<FeedCategory, CategoryMeta>> = {
    [FeedCategory.General]: {
        icon: 'pi-info-circle',
        accent: 'var(--ec-info)',
        labelKey: 'liveFeed.category.general',
        urgency: FeedUrgency.Low,
    },
    [FeedCategory.Transport]: {
        icon: 'pi-car',
        accent: 'var(--ec-info)',
        labelKey: 'liveFeed.category.transport',
        urgency: FeedUrgency.Medium,
    },
    [FeedCategory.Accommodation]: {
        icon: 'pi-home',
        accent: 'var(--ec-info)',
        labelKey: 'liveFeed.category.accommodation',
        urgency: FeedUrgency.Low,
    },
    [FeedCategory.Food]: {
        icon: 'pi-shopping-cart',
        accent: 'var(--ec-success)',
        labelKey: 'liveFeed.category.food',
        urgency: FeedUrgency.Low,
    },
    [FeedCategory.Music]: {
        icon: 'pi-volume-up',
        accent: 'var(--ec-yellow)',
        labelKey: 'liveFeed.category.music',
        urgency: FeedUrgency.Medium,
    },
    [FeedCategory.Lineup]: {
        icon: 'pi-star',
        accent: 'var(--ec-yellow)',
        labelKey: 'liveFeed.category.lineup',
        urgency: FeedUrgency.Medium,
    },
    [FeedCategory.Activity]: {
        icon: 'pi-bolt',
        accent: 'var(--ec-yellow)',
        labelKey: 'liveFeed.category.activity',
        urgency: FeedUrgency.Low,
    },
    [FeedCategory.Weather]: {
        icon: 'pi-cloud',
        accent: 'var(--ec-warning)',
        labelKey: 'liveFeed.category.weather',
        urgency: FeedUrgency.High,
    },
    [FeedCategory.Rules]: {
        icon: 'pi-shield',
        accent: 'var(--ec-gray-mid)',
        labelKey: 'liveFeed.category.rules',
        urgency: FeedUrgency.Low,
    },
    [FeedCategory.Ticket]: {
        icon: 'pi-ticket',
        accent: 'var(--ec-info)',
        labelKey: 'liveFeed.category.ticket',
        urgency: FeedUrgency.Medium,
    },
    [FeedCategory.Safety]: {
        icon: 'pi-exclamation-triangle',
        accent: 'var(--ec-red)',
        labelKey: 'liveFeed.category.safety',
        urgency: FeedUrgency.High,
    },
    [FeedCategory.Health]: {
        icon: 'pi-heart',
        accent: 'var(--ec-success)',
        labelKey: 'liveFeed.category.health',
        urgency: FeedUrgency.Medium,
    },
};

export const FILTER_TO_CATEGORIES: Readonly<Record<FeedFilter, ReadonlyArray<FeedCategory>>> = {
    [FeedFilter.All]: [],
    [FeedFilter.Urgent]: [FeedCategory.Safety, FeedCategory.Health, FeedCategory.Weather],
    [FeedFilter.Schedule]: [FeedCategory.Music, FeedCategory.Lineup, FeedCategory.Activity],
    [FeedFilter.GettingAround]: [FeedCategory.Transport, FeedCategory.Accommodation],
    [FeedFilter.General]: [
        FeedCategory.Food,
        FeedCategory.Rules,
        FeedCategory.Ticket,
        FeedCategory.General,
    ],
};

export const FILTER_LABEL_KEY: Readonly<Record<FeedFilter, string>> = {
    [FeedFilter.All]: 'liveFeed.filter.all',
    [FeedFilter.Urgent]: 'liveFeed.filter.urgent',
    [FeedFilter.Schedule]: 'liveFeed.filter.schedule',
    [FeedFilter.GettingAround]: 'liveFeed.filter.gettingAround',
    [FeedFilter.General]: 'liveFeed.filter.general',
};

export const FILTER_ORDER: ReadonlyArray<FeedFilter> = [
    FeedFilter.All,
    FeedFilter.Urgent,
    FeedFilter.Schedule,
    FeedFilter.GettingAround,
    FeedFilter.General,
];

/**
 * Category priority used to derive the "right now" pinned capsule
 * and to sort within-tier when needed. Lower index = higher priority.
 */
export const PRIORITY_ORDER: ReadonlyArray<FeedCategory> = [
    FeedCategory.Safety,
    FeedCategory.Health,
    FeedCategory.Weather,
    FeedCategory.Transport,
    FeedCategory.Lineup,
    FeedCategory.Music,
    FeedCategory.Ticket,
    FeedCategory.Activity,
    FeedCategory.Rules,
    FeedCategory.Accommodation,
    FeedCategory.Food,
    FeedCategory.General,
];
