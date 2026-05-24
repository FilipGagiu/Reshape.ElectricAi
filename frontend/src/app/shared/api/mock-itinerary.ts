import { ItineraryResponse } from './dto/itinerary.dto';

/**
 * Synthetic itinerary used while the backend is incomplete. `ItineraryApi`
 * returns this whenever the bypass-login dev flag is active so the plan and
 * story views can be exercised end-to-end without a real JWT.
 *
 * Mirrors the real backend's section-driven payload (greeting / transport /
 * vibeActivities / food / topArtists / accommodation) so every render
 * branch in the new plan page gets exercised.
 *
 * Delete once the backend serves stable data on `GET /api/v1/Itinerary`.
 */
export const MOCK_ITINERARY_RESPONSE: ItineraryResponse = {
    preferences: {
        name: null,
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
        id: '00000000-0000-0000-0000-000000000mock',
        generatedUtc: '2026-05-24T09:00:00.000Z',
        sections: [
            {
                key: 'greeting',
                data: {
                    crew: { kind: 'WithGroup', size: 4 },
                    name: null,
                    origin: 'Cluj-Napoca',
                },
                diagnostic: null,
            },
            {
                key: 'transport',
                data: { mode: 'EcBus', note: 'Pickup at Cluj Hub 09:00' },
                diagnostic: null,
            },
            {
                key: 'vibeActivities',
                data: {
                    vibeTags: ['Late nights', 'Lake breaks', 'New sounds'],
                    topActivities: [
                        {
                            id: 'mock-activity-1',
                            score: 0.42,
                            title: 'ec-website/vip-experience#FOOD À LA CARTE',
                            snippet:
                                '## FOOD À LA CARTE\n\nElectric Castle VIP food service — à la carte dining available exclusively for VIP ticket holders. Not your mom’s food. But she will most certainly approve of our Chef’s cooking.',
                        },
                        {
                            id: 'mock-activity-2',
                            score: 0.36,
                            title: 'ec-website/international#Grab tasty bites',
                            snippet:
                                'Recommended restaurants in Cluj-Napoca for Electric Castle visitors: Casa Boema, Bulgakov, Eggcetera, Samsara Foodhouse.',
                        },
                        {
                            id: 'mock-activity-3',
                            score: 0.29,
                            title: 'ec-website/vip-experience#CHARGING SPOTS',
                            snippet:
                                'Dedicated charging spots for VIP ticket holders. All phone bars happy.',
                        },
                    ],
                },
                diagnostic: null,
            },
            {
                key: 'food',
                data: {
                    restrictions: ['Vegetarian'],
                    topRestaurants: [
                        {
                            id: 'mock-food-1',
                            score: 0.34,
                            title: 'ec-website/vip-experience#FOOD À LA CARTE',
                            snippet:
                                'Electric Castle VIP food service — à la carte dining for VIP ticket holders.',
                        },
                        {
                            id: 'mock-food-2',
                            score: 0.29,
                            title: 'ec-website/international#Grab tasty bites',
                            snippet:
                                'Casa Boema, Bulgakov, Samsara Foodhouse, Tortelli Pasta Bar — plant-based and Romanian highlights in Cluj.',
                        },
                    ],
                    preferredCuisines: ['Italian', 'Romanian', 'StreetFood'],
                },
                diagnostic: null,
            },
            {
                key: 'topArtists',
                data: {
                    byDay: [
                        {
                            date: '2026-07-16',
                            artists: [
                                {
                                    id: 'mock-artist-1',
                                    score: 0.6,
                                    title: 'The Cure — Main Stage',
                                    eventUtc: '2026-07-16T20:00:00.000Z',
                                },
                                {
                                    id: 'mock-artist-2',
                                    score: 0.5,
                                    title: 'Apashe — Dance Arena',
                                    eventUtc: '2026-07-16T23:30:00.000Z',
                                },
                            ],
                        },
                        {
                            date: '2026-07-17',
                            artists: [
                                {
                                    id: 'mock-artist-3',
                                    score: 0.4,
                                    title: 'Sub Focus — Hangar',
                                    eventUtc: '2026-07-17T22:00:00.000Z',
                                },
                            ],
                        },
                    ],
                    topOverall: [
                        {
                            id: 'mock-artist-1',
                            score: 0.6,
                            title: 'The Cure — Main Stage',
                            eventUtc: '2026-07-16T20:00:00.000Z',
                        },
                    ],
                },
                diagnostic: null,
            },
            {
                key: 'accommodation',
                data: { type: 'Camping', note: 'Bring a warm sleeping bag, nights run cold.' },
                diagnostic: null,
            },
        ],
    },
};
