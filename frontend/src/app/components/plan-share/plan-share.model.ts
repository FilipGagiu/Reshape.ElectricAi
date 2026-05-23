/**
 * Shape of a personalised Electric Castle plan rendered in the stories viewer.
 *
 * Backend not implemented yet. `PlanShareService.getPlanByUuid` returns this
 * shape from a hardcoded mock for now; the real implementation will call
 * `GET /api/v1/plans/:uuid` with the same return shape.
 */

export type TransportMethod = 'car' | 'train' | 'bus' | 'bike' | 'onFoot';
export type AccommodationKind = 'festivalCamping' | 'bontida' | 'cluj';

export interface PlanWelcomeSlide {
    readonly type: 'welcome';
    readonly name: string;
}

export interface PlanTransportSlide {
    readonly type: 'transport';
    readonly method: TransportMethod;
    readonly from: string;
}

export interface PlanSleepSlide {
    readonly type: 'sleep';
    readonly accommodation: AccommodationKind;
}

export interface PlanMusicSlide {
    readonly type: 'music';
    readonly artists: ReadonlyArray<string>;
    readonly genres: ReadonlyArray<string>;
}

export interface PlanFoodSlide {
    readonly type: 'food';
    readonly cuisines: ReadonlyArray<string>;
    readonly allergies: ReadonlyArray<string>;
}

export interface PlanActivityVibeSlide {
    readonly type: 'activityVibe';
    readonly activityKeys: ReadonlyArray<string>; // i18n key suffixes under plan.story.activityVibe.activity
    readonly vibeKey: string; // i18n key suffix under plan.story.activityVibe.vibe
}

export interface PlanShareSlide {
    readonly type: 'share';
    readonly uuid: string;
}

export type PlanSlide =
    | PlanWelcomeSlide
    | PlanTransportSlide
    | PlanSleepSlide
    | PlanMusicSlide
    | PlanFoodSlide
    | PlanActivityVibeSlide
    | PlanShareSlide;

export interface PlanData {
    readonly uuid: string;
    readonly ownerName: string;
    readonly slides: ReadonlyArray<PlanSlide>;
    readonly createdAt: string;
}

/** Default per-slide auto-advance duration in milliseconds. */
export const SLIDE_DURATION_MS = 6000;

/** Hold-to-pause threshold in milliseconds. */
export const HOLD_TO_PAUSE_THRESHOLD_MS = 300;

/** Hardcoded UUID for the demo mock plan. */
export const MOCK_PLAN_UUID = '00000000-0000-0000-0000-000000000001';
