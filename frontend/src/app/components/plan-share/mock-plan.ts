import { MOCK_PLAN_UUID, PlanData } from './plan-share.model';

/**
 * Persona: Andra, 24, from Cluj-Napoca. Full festival pass, doesn't want to
 * camp, will stay at a Cluj hotel and take the shuttle. Likes electronic +
 * pop. Top artists picked: Bring Me The Horizon, Apashe, Sub Focus. Cuisine
 * Italian + Romanian, no allergies. Activities yoga + lake swim.
 *
 * This is the single hardcoded plan served by the mock PlanShareService for
 * the demo `MOCK_PLAN_UUID`. When the real backend lands, this file goes
 * away and the service calls the API instead.
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
            type: 'budget',
            amountRon: 1700,
            breakdown: [
                { key: 'ticket', ron: 600 },
                { key: 'sleep', ron: 600 },
                { key: 'transport', ron: 150 },
                { key: 'food', ron: 200 },
                { key: 'chaos', ron: 150 },
            ],
        },
        {
            type: 'food',
            cuisines: ['italian', 'romanian'],
            allergies: [],
        },
        {
            type: 'weather',
            tempLow: 14,
            tempHigh: 28,
            tipKeys: ['layers', 'sunscreen', 'noGlass'],
        },
        { type: 'share', uuid: MOCK_PLAN_UUID },
    ],
};
