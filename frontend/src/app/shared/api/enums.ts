/**
 * Single source of truth for every BE enum. Keep aligned with the C# enums in
 * `src/Reshape.ElectricAi.Core/Enums/`. When BE adds a value, update here and
 * every dropdown / chip group consuming the array picks it up automatically.
 *
 * Pattern: each enum is a `readonly` tuple → the TS union type is derived via
 * `typeof XXX_VALUES[number]`. No duplication.
 */

export const MUSIC_GENRE_VALUES = [
    'AcidHouse', 'AfricanFunk', 'AfroFunk', 'AfroHouse', 'AfroLatin', 'AfroTech',
    'AltPop', 'AltRock', 'Alternative', 'AlternativeRock', 'Amapiano', 'Ambient',
    'ArabicFusion', 'ArabicRock', 'ArtPop', 'AvantGarde', 'Balkan', 'Ballroom',
    'Bass', 'BassMusic', 'Bhangra', 'BigBeat', 'Blues', 'Breakbeat', 'Breaks',
    'CaribbeanHouse', 'Collage', 'Comedy', 'Contemporary', 'CulturalFusion',
    'Dance', 'DanceGrooves', 'DancePop', 'Dancehall', 'Dark', 'DarkPop',
    'Deep', 'DeepGrooves', 'DeepHouse', 'DeepMinimal', 'DeepTech', 'Disco',
    'DiyRap', 'Dj', 'DjDuo', 'Dnb', 'DowntempoToAfroHouse', 'DreamPop',
    'DrumAndBass', 'Dub', 'Dubstep', 'DynamicHouse', 'Ebm', 'Eclectic',
    'EclecticElectronic', 'EclecticHipHopInspired', 'EclecticMix', 'Electro',
    'ElectroPop', 'ElectroSwing', 'Electronic', 'ElectronicHybrid',
    'ElegantNightlife', 'Euphoria', 'EventSeries', 'Experimental', 'Folk',
    'FolkFusion', 'FolkMetal', 'FolkPop', 'FolkPunk', 'Footwork',
    'ForwardThinkingElectronic', 'FrenchSamples', 'Fresh', 'Funk', 'FunkyBass',
    'Fusion', 'FutureFunk', 'Garage', 'GaragePunk', 'GarageRock', 'Global',
    'GlobalBass', 'GlobalFusion', 'GlobalPop', 'Gospel', 'GothicRock', 'Gqom',
    'Groove', 'GrooveHouse', 'GypsyPunk', 'Hardcore', 'HardcorePunk',
    'Hardwave', 'HipHop', 'House', 'HouseClassics', 'HybridLiveSets', 'Indie',
    'IndieDance', 'IndieElectronica', 'IndieFolk', 'IndiePop', 'IndieRock',
    'IndonesianFunk', 'Jazz', 'JazzHouse', 'JerseyClub', 'JumpUp', 'Jungle',
    'Latin', 'LatinClub', 'LoFi', 'MainstreamMeetsUnderground', 'Mc', 'Melodic',
    'MelodicHouse', 'MelodicTechno', 'Metal', 'MicroHouse', 'MiddleEastern',
    'Minimal', 'MinimalTechno', 'MultiGenre', 'NuDisco', 'NuJazz', 'OldSchoolRap',
    'OrganicHouse', 'Pop', 'PopPunk', 'PostPunk', 'PowerPop', 'Progressive',
    'ProgressiveMetal', 'Psychedelia', 'Psychedelic', 'RAndB', 'Rap', 'Reggae',
    'Reggaeton', 'Riddim', 'Rock', 'Roots', 'Ska', 'Soul', 'Soulful', 'Swing',
    'TechHouse', 'Techno', 'Technopop', 'Trap', 'TropicalDisco', 'Turntablist',
    'UkGarage', 'Underground', 'UndergroundClub', 'Various', 'VariousElectronic',
    'Vinyl', 'VinylExplorer', 'VinylOnly', 'VinylOnlyElectronic', 'Vocalist',
    'Vocals', 'Wave', 'World', 'WorldMusic', 'Other',
] as const;
export type MusicGenre = (typeof MUSIC_GENRE_VALUES)[number];

export const FOOD_RESTRICTION_VALUES = [
    'Vegan', 'Vegetarian', 'NoPeanuts', 'NoMeat', 'NoPork', 'NoDairy',
    'NoGluten', 'NoShellfish', 'NoEggs', 'Halal', 'Kosher',
] as const;
export type FoodRestriction = (typeof FOOD_RESTRICTION_VALUES)[number];

export const CUISINE_VALUES = [
    'American', 'Italian', 'Romanian', 'Mexican', 'Chinese', 'Japanese',
    'Indian', 'Thai', 'French', 'Greek', 'Mediterranean', 'MiddleEastern',
    'Bbq', 'StreetFood', 'Lebanese', 'Hungarian', 'Other',
] as const;
export type Cuisine = (typeof CUISINE_VALUES)[number];

export const ACTIVITY_TYPE_VALUES = [
    'Relax', 'Energetic', 'Adrenaline', 'Social', 'Creative', 'Wellness', 'Discovery',
] as const;
export type ActivityType = (typeof ACTIVITY_TYPE_VALUES)[number];

export const TICKET_TYPE_VALUES = ['Standard', 'Vip', 'UltraVip', 'Black'] as const;
export type TicketType = (typeof TICKET_TYPE_VALUES)[number];

export const AGE_GROUP_VALUES = [
    'Under18', 'Adult18To24', 'Adult25To34', 'Adult35To44', 'Adult45Plus',
] as const;
export type AgeGroup = (typeof AGE_GROUP_VALUES)[number];

export const CREW_KIND_VALUES = ['Solo', 'WithGroup'] as const;
export type CrewKind = (typeof CREW_KIND_VALUES)[number];

export const TRANSPORT_MODE_VALUES = [
    'RideShare', 'Car', 'EcTrain', 'EcBus', 'Helicopter',
    'Plane', 'Bike', 'CfrTrain', 'Flixbus',
] as const;
export type TransportMode = (typeof TRANSPORT_MODE_VALUES)[number];

export const ACCOMMODATION_VALUES = [
    'VillageRental', 'Camping', 'CarCamping', 'RvCamping', 'Glamping', 'Hotel',
] as const;
export type Accommodation = (typeof ACCOMMODATION_VALUES)[number];
