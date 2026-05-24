import { PlanIntakeQuestion, PlanIntakeQuestionId } from '../models/plan-intake.model';

const KEY_PREFIX = 'plan.intake';

const promptKey = (id: PlanIntakeQuestionId): string => `${KEY_PREFIX}.q.${id}.prompt`;
const placeholderKey = (id: PlanIntakeQuestionId): string =>
    `${KEY_PREFIX}.q.${id}.placeholder`;
const descriptionKey = (id: PlanIntakeQuestionId): string =>
    `${KEY_PREFIX}.q.${id}.description`;
const chipKey = (id: PlanIntakeQuestionId, suffix: string): string =>
    `${KEY_PREFIX}.q.${id}.chip.${suffix}`;

export const PLAN_INTAKE_QUESTIONS: ReadonlyArray<PlanIntakeQuestion> = [
    {
        id: PlanIntakeQuestionId.Location,
        promptKey: promptKey(PlanIntakeQuestionId.Location),
        placeholderKey: placeholderKey(PlanIntakeQuestionId.Location),
        descriptionKey: descriptionKey(PlanIntakeQuestionId.Location),
        suggestionKeys: [
            chipKey(PlanIntakeQuestionId.Location, 'cluj'),
            chipKey(PlanIntakeQuestionId.Location, 'bucharest'),
            chipKey(PlanIntakeQuestionId.Location, 'otherRomania'),
            chipKey(PlanIntakeQuestionId.Location, 'abroad'),
        ],
    },
    {
        id: PlanIntakeQuestionId.GroupSize,
        promptKey: promptKey(PlanIntakeQuestionId.GroupSize),
        placeholderKey: placeholderKey(PlanIntakeQuestionId.GroupSize),
        descriptionKey: descriptionKey(PlanIntakeQuestionId.GroupSize),
        suggestionKeys: [
            chipKey(PlanIntakeQuestionId.GroupSize, 'solo'),
            chipKey(PlanIntakeQuestionId.GroupSize, 'duo'),
            chipKey(PlanIntakeQuestionId.GroupSize, 'smallCrew'),
            chipKey(PlanIntakeQuestionId.GroupSize, 'bigCrew'),
        ],
    },
    {
        id: PlanIntakeQuestionId.Accommodation,
        promptKey: promptKey(PlanIntakeQuestionId.Accommodation),
        placeholderKey: placeholderKey(PlanIntakeQuestionId.Accommodation),
        descriptionKey: descriptionKey(PlanIntakeQuestionId.Accommodation),
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
        descriptionKey: descriptionKey(PlanIntakeQuestionId.Vibe),
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
        descriptionKey: descriptionKey(PlanIntakeQuestionId.Music),
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
        descriptionKey: descriptionKey(PlanIntakeQuestionId.Dietary),
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
        descriptionKey: descriptionKey(PlanIntakeQuestionId.Extra),
        suggestionKeys: [chipKey(PlanIntakeQuestionId.Extra, 'nothing')],
    },
];
