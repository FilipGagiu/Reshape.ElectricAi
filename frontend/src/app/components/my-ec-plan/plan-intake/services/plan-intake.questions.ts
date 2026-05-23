import { PlanIntakeQuestion, PlanIntakeQuestionId } from '../models/plan-intake.model';

const KEY_PREFIX = 'plan.intake';

const promptKey = (id: PlanIntakeQuestionId): string => `${KEY_PREFIX}.q.${id}.prompt`;
const placeholderKey = (id: PlanIntakeQuestionId): string =>
    `${KEY_PREFIX}.q.${id}.placeholder`;
const chipKey = (id: PlanIntakeQuestionId, suffix: string): string =>
    `${KEY_PREFIX}.q.${id}.chip.${suffix}`;

export const PLAN_INTAKE_QUESTIONS: ReadonlyArray<PlanIntakeQuestion> = [
    {
        id: PlanIntakeQuestionId.Name,
        promptKey: promptKey(PlanIntakeQuestionId.Name),
        placeholderKey: placeholderKey(PlanIntakeQuestionId.Name),
        suggestionKeys: [],
    },
    {
        id: PlanIntakeQuestionId.Origin,
        promptKey: promptKey(PlanIntakeQuestionId.Origin),
        placeholderKey: placeholderKey(PlanIntakeQuestionId.Origin),
        suggestionKeys: [
            chipKey(PlanIntakeQuestionId.Origin, 'clujSolo'),
            chipKey(PlanIntakeQuestionId.Origin, 'bucharestCrew'),
            chipKey(PlanIntakeQuestionId.Origin, 'abroadGroup'),
        ],
    },
    {
        id: PlanIntakeQuestionId.Accommodation,
        promptKey: promptKey(PlanIntakeQuestionId.Accommodation),
        placeholderKey: placeholderKey(PlanIntakeQuestionId.Accommodation),
        suggestionKeys: [
            chipKey(PlanIntakeQuestionId.Accommodation, 'festivalCamping'),
            chipKey(PlanIntakeQuestionId.Accommodation, 'bontida'),
            chipKey(PlanIntakeQuestionId.Accommodation, 'cluj'),
            chipKey(PlanIntakeQuestionId.Accommodation, 'undecided'),
        ],
    },
    {
        id: PlanIntakeQuestionId.Vibe,
        promptKey: promptKey(PlanIntakeQuestionId.Vibe),
        placeholderKey: placeholderKey(PlanIntakeQuestionId.Vibe),
        suggestionKeys: [
            chipKey(PlanIntakeQuestionId.Vibe, 'chillExplore'),
            chipKey(PlanIntakeQuestionId.Vibe, 'raveFood'),
            chipKey(PlanIntakeQuestionId.Vibe, 'discoveryWorkshops'),
        ],
    },
    {
        id: PlanIntakeQuestionId.Music,
        promptKey: promptKey(PlanIntakeQuestionId.Music),
        placeholderKey: placeholderKey(PlanIntakeQuestionId.Music),
        suggestionKeys: [
            chipKey(PlanIntakeQuestionId.Music, 'surprise'),
            chipKey(PlanIntakeQuestionId.Music, 'electronic'),
            chipKey(PlanIntakeQuestionId.Music, 'rock'),
            chipKey(PlanIntakeQuestionId.Music, 'hipHop'),
        ],
    },
    {
        id: PlanIntakeQuestionId.Dietary,
        promptKey: promptKey(PlanIntakeQuestionId.Dietary),
        placeholderKey: placeholderKey(PlanIntakeQuestionId.Dietary),
        suggestionKeys: [
            chipKey(PlanIntakeQuestionId.Dietary, 'none'),
            chipKey(PlanIntakeQuestionId.Dietary, 'vegetarian'),
            chipKey(PlanIntakeQuestionId.Dietary, 'vegan'),
            chipKey(PlanIntakeQuestionId.Dietary, 'glutenFree'),
        ],
    },
    {
        id: PlanIntakeQuestionId.Extra,
        promptKey: promptKey(PlanIntakeQuestionId.Extra),
        placeholderKey: placeholderKey(PlanIntakeQuestionId.Extra),
        suggestionKeys: [chipKey(PlanIntakeQuestionId.Extra, 'nothing')],
    },
];
