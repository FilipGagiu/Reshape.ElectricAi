import { Conversation, HotQuestion } from '../models/question.model';

/**
 * Mock fixtures while backend isn't ready. EC-festival-flavored Q&A.
 * Order = freshness. Top card gets `isFreshest: true`.
 */

export const MOCK_HOT_QUESTIONS: readonly HotQuestion[] = [
    {
        id: 'hq-001',
        text: 'Can I bring a refillable bottle?',
        askedCount: 28,
        isFreshest: true,
        curatedAnswer:
            "Yes. Bring it empty and fill it at any of our water refill stations across the festival site. " +
            'Reusable bottles cut down on plastic waste, which is part of our Zero Waste Goal. ' +
            'Glass bottles are not allowed anywhere on site, so make sure yours is plastic or metal.',
    },
    {
        id: 'hq-002',
        text: 'What time does the Main Stage open?',
        askedCount: 47,
        curatedAnswer:
            'Main Stage gates open at 16:00 on day one and 14:00 on the remaining festival days. ' +
            "First acts usually start about 30 minutes after gates. Check the Daily Schedule in the app for the exact set times.",
    },
    {
        id: 'hq-003',
        text: 'Is the camping area shaded?',
        askedCount: 32,
        curatedAnswer:
            'Partly. Camping Verde has tree cover in the western section. The eastern section is open meadow. ' +
            "Bring a tarp or a tent with a sun fly if you're camping east. Mornings get hot fast in July.",
    },
    {
        id: 'hq-004',
        text: 'How do I get to Bonțida from Cluj-Napoca?',
        askedCount: 19,
        curatedAnswer:
            'Free shuttles run from Cluj-Napoca central station every 30 minutes during festival days. ' +
            'The ride is about 40 minutes. You can also take a taxi or rideshare (around 80 RON). ' +
            'See the International page for full transport options.',
    },
    {
        id: 'hq-005',
        text: 'Are there food options for vegans?',
        askedCount: 14,
        curatedAnswer:
            'Yes. Around a third of the food vendors offer dedicated vegan menus. Look for the green leaf badge on the Food map. ' +
            'You can also filter the vendor list by dietary tag in the app.',
    },
] as const;

const conversationOneTime = new Date(Date.now() - 1000 * 60 * 60 * 6); // 6 hours ago
const conversationTwoTime = new Date(Date.now() - 1000 * 60 * 60 * 28); // ~1 day ago

export const MOCK_CONVERSATIONS: readonly Conversation[] = [
    {
        id: 'conv-001',
        firstQuestion: 'When is the lineup announced?',
        updatedAt: conversationOneTime,
        messages: [
            {
                id: 'msg-001-1',
                role: 'user',
                text: 'When is the lineup announced?',
                createdAt: new Date(conversationOneTime.getTime() - 1000 * 60 * 5),
            },
            {
                id: 'msg-001-2',
                role: 'assistant',
                text:
                    'The first wave drops in early March. Headliners follow in April. ' +
                    "Turn on notifications in your profile and you'll get the heads-up the moment names land.",
                createdAt: new Date(conversationOneTime.getTime() - 1000 * 60 * 4),
            },
            {
                id: 'msg-001-3',
                role: 'user',
                text: 'Will there be any techno acts on the main stage this year?',
                createdAt: new Date(conversationOneTime.getTime() - 1000 * 60 * 2),
            },
            {
                id: 'msg-001-4',
                role: 'assistant',
                text:
                    "We can't confirm specific names until the official reveal, " +
                    'but the Main Stage usually carries 2-3 high-energy electronic acts per festival. ' +
                    'Hangar and Booha are the techno-heaviest stages.',
                createdAt: conversationOneTime,
            },
        ],
    },
    {
        id: 'conv-002',
        firstQuestion: 'Where can I park if I drive in?',
        updatedAt: conversationTwoTime,
        messages: [
            {
                id: 'msg-002-1',
                role: 'user',
                text: 'Where can I park if I drive in?',
                createdAt: new Date(conversationTwoTime.getTime() - 1000 * 60 * 2),
            },
            {
                id: 'msg-002-2',
                role: 'assistant',
                text:
                    'Festival parking is in the marked lots near the southern entrance. ' +
                    'Bring 30 RON per day in cash or pay via the app. The lots fill up fast on day one, so arrive early.',
                createdAt: conversationTwoTime,
            },
        ],
    },
] as const;
