import {
    AccommodationSectionData,
    FoodSectionData,
    GreetingSectionData,
    ItinerarySectionDto,
    TopArtistsSectionData,
    TransportSectionData,
    VibeActivitiesSectionData,
} from '@shared/api/dto/itinerary.dto';

export type ParsedSection =
    | { readonly kind: 'greeting'; readonly data: GreetingSectionData }
    | { readonly kind: 'transport'; readonly data: TransportSectionData }
    | { readonly kind: 'vibeActivities'; readonly data: VibeActivitiesSectionData }
    | { readonly kind: 'food'; readonly data: FoodSectionData }
    | { readonly kind: 'topArtists'; readonly data: TopArtistsSectionData }
    | { readonly kind: 'accommodation'; readonly data: AccommodationSectionData };

/**
 * Narrow an `ItinerarySectionDto` (whose `data` is `unknown`) into the
 * specific shape associated with its `key`. Unknown keys return null so the
 * renderer can skip them silently — forward-compat for new backend sections.
 */
export function parseSection(section: ItinerarySectionDto): ParsedSection | null {
    const data = (section.data ?? null) as Record<string, unknown> | null;
    if (!data) return null;
    switch (section.key) {
        case 'greeting':
            return { kind: 'greeting', data: data as unknown as GreetingSectionData };
        case 'transport':
            return { kind: 'transport', data: data as unknown as TransportSectionData };
        case 'vibeActivities':
            return { kind: 'vibeActivities', data: data as unknown as VibeActivitiesSectionData };
        case 'food':
            return { kind: 'food', data: data as unknown as FoodSectionData };
        case 'topArtists':
            return { kind: 'topArtists', data: data as unknown as TopArtistsSectionData };
        case 'accommodation':
            return { kind: 'accommodation', data: data as unknown as AccommodationSectionData };
        default:
            return null;
    }
}

/**
 * Backend-retrieved titles often carry a source prefix like
 * `ec-website/vip-experience#FOOD À LA CARTE` or `Lineup: DJ SAUCE`.
 * Strip those so the UI shows the human-readable tail.
 */
export function cleanTitle(raw: string): string {
    if (!raw) return raw;
    const hashIndex = raw.lastIndexOf('#');
    let cleaned = hashIndex >= 0 ? raw.slice(hashIndex + 1) : raw;
    cleaned = cleaned.replace(/^lineup:\s*/i, '');
    return cleaned.trim();
}

/**
 * Trim a long retrieval snippet down to a single sentence (or 160 chars,
 * whichever comes first). Removes leading markdown heading + collapses
 * whitespace so the card preview stays scannable.
 */
export function cleanSnippet(raw: string | undefined): string {
    if (!raw) return '';
    const stripped = raw
        .replace(/^##\s+[^\n]*\n+/, '')
        .replace(/\s+/g, ' ')
        .trim();
    if (!stripped) return '';
    const firstSentenceEnd = stripped.search(/(?<=[.!?])\s/);
    const cap = 160;
    const candidate =
        firstSentenceEnd > 0 ? stripped.slice(0, firstSentenceEnd + 1) : stripped;
    if (candidate.length <= cap) return candidate;
    return `${candidate.slice(0, cap - 1).trimEnd()}…`;
}

const DAY_FORMATTER_CACHE = new Map<string, Intl.DateTimeFormat>();

export function formatDayHeading(isoDate: string, locale: string): string {
    if (!isoDate) return '';
    const cacheKey = locale;
    let formatter = DAY_FORMATTER_CACHE.get(cacheKey);
    if (!formatter) {
        formatter = new Intl.DateTimeFormat(locale, {
            weekday: 'short',
            day: 'numeric',
            month: 'short',
        });
        DAY_FORMATTER_CACHE.set(cacheKey, formatter);
    }
    const parsed = new Date(isoDate);
    if (Number.isNaN(parsed.getTime())) return isoDate;
    return formatter.format(parsed);
}

export function formatEventTime(iso: string | undefined, locale: string): string {
    if (!iso) return '';
    const parsed = new Date(iso);
    if (Number.isNaN(parsed.getTime())) return '';
    return parsed.toLocaleString(locale, {
        weekday: 'short',
        hour: '2-digit',
        minute: '2-digit',
    });
}
