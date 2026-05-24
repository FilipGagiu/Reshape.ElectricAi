import { MOCK_PLAN_UUID, PlanData } from './plan-share.model';

/**
 * @deprecated Backend now serves plan data via `GET /api/v1/Itinerary`
 * (tied to the JWT). `PlanShareService.getCurrent()` calls the real API
 * + `itineraryToPlanData()` mapper. This mock is kept only so historical
 * imports don't break; remove once no consumer references it.
 *
 * Persona: Andra, 24, from Cluj-Napoca. Full festival pass, doesn't want to
 * camp, will stay at a Cluj hotel and take the shuttle. Likes electronic +
 * pop. Top artists picked: Bring Me The Horizon, Apashe, Sub Focus. Cuisine
 * Italian + Romanian, no allergies. Activities yoga + lake swim.
 */
export const MOCK_PLAN: PlanData = {
    uuid: MOCK_PLAN_UUID,
    ownerName: 'Andra',
    createdAt: new Date().toISOString(),
    slides: [
        { type: 'welcome', name: 'Andra' },
        { type: 'transport', method: 'train', from: 'Cluj-Napoca' },
        { type: 'sleep', accommodation: 'cluj' },
        {
            type: 'music',
            artists: ['Bring Me The Horizon', 'Apashe', 'Sub Focus'],
            genres: ['electronic', 'pop'],
        },
        {
            type: 'food',
            cuisines: ['italian', 'romanian'],
            allergies: [],
        },
        {
            type: 'activityVibe',
            activityKeys: ['yoga', 'lakeSwim', 'castleTour'],
            vibeKey: 'curiousAndChill',
        },
        { type: 'share', uuid: MOCK_PLAN_UUID },
    ],
};
