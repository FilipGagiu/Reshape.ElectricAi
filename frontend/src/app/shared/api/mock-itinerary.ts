import { ItineraryResponse } from './dto/itinerary.dto';

/**
 * Synthetic itinerary used while the backend is incomplete. `ItineraryApi`
 * returns this whenever the bypass-login dev flag is active so the plan and
 * story views can be exercised end-to-end without a real JWT.
 *
 * Delete once the backend serves stable data on `GET /api/v1/Itinerary`.
 */
export const MOCK_ITINERARY_RESPONSE: ItineraryResponse = {
    preferences: {
        name: 'Paul',
        origin: 'Cluj-Napoca',
        crew: { kind: 'WithGroup', estimatedSize: 4 },
        vibeTags: ['Late nights', 'Lake breaks', 'New sounds'],
        musicGenres: ['House', 'Techno', 'Electronic'],
        mustSeeArtists: ['The Cure', 'Apashe', 'Sub Focus'],
        foodRestrictions: ['Vegetarian'],
        cuisines: ['Italian', 'Romanian', 'StreetFood'],
        activityInterests: ['Relax', 'Discovery', 'Wellness'],
        suggestedTransport: { mode: 'EcBus', note: 'Pickup at Cluj Hub 09:00' },
        suggestedAccommodation: { type: 'Camping', note: 'Bring a warm sleeping bag' },
        ticketType: 'Vip',
        ageGroup: 'Adult25To34',
        completionPercent: 0.92,
        updatedUtc: '2026-05-24T09:00:00.000Z',
    },
    itinerary: {
        generatedUtc: '2026-05-24T09:00:00.000Z',
        sections: [
            { key: 'transport', data: { mode: 'EcBus', pickup: 'Cluj Hub', departure: '09:00' }, diagnostic: null },
            { key: 'accommodation', data: { type: 'Camping', tip: 'Earplugs welcome' }, diagnostic: null },
            { key: 'music', data: { headliners: ['The Cure', 'Apashe'] }, diagnostic: null },
            { key: 'food', data: { topVendors: ['Vegan Bites', 'Sarmale Cart'] }, diagnostic: null },
            { key: 'activities', data: { picks: ['Lake swim', 'Castle tour'] }, diagnostic: null },
        ],
    },
};
