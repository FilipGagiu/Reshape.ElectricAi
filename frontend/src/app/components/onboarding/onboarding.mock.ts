import {
    Accommodation,
    SelectOption,
    TicketType,
    Transportation,
    TypedSelectOption,
} from './onboarding.model';

export const TICKET_OPTIONS: ReadonlyArray<TypedSelectOption<TicketType>> = [
    {
        id: TicketType.NoTicket,
        value: TicketType.NoTicket,
        labelKey: 'onboarding.ticket.noTicket',
        icon: 'pi-times-circle',
    },
    {
        id: TicketType.FullPass,
        value: TicketType.FullPass,
        labelKey: 'onboarding.ticket.fullPass',
        icon: 'pi-ticket',
    },
    {
        id: TicketType.DayTicket,
        value: TicketType.DayTicket,
        labelKey: 'onboarding.ticket.dayTicket',
        icon: 'pi-calendar',
    },
    {
        id: TicketType.Vip,
        value: TicketType.Vip,
        labelKey: 'onboarding.ticket.vip',
        icon: 'pi-star',
    },
    {
        id: TicketType.UltraVip,
        value: TicketType.UltraVip,
        labelKey: 'onboarding.ticket.ultraVip',
        icon: 'pi-crown',
    },
    {
        id: TicketType.BlackTicket,
        value: TicketType.BlackTicket,
        labelKey: 'onboarding.ticket.blackTicket',
        icon: 'pi-moon',
    },
];

export const ACCOMMODATION_OPTIONS: ReadonlyArray<TypedSelectOption<Accommodation>> = [
    {
        id: Accommodation.FestivalCamping,
        value: Accommodation.FestivalCamping,
        labelKey: 'onboarding.accommodation.festivalCamping',
        icon: 'pi-home',
    },
    {
        id: Accommodation.Bontida,
        value: Accommodation.Bontida,
        labelKey: 'onboarding.accommodation.bontida',
        icon: 'pi-map-marker',
    },
    {
        id: Accommodation.Cluj,
        value: Accommodation.Cluj,
        labelKey: 'onboarding.accommodation.cluj',
        icon: 'pi-building',
    },
];

export const TRANSPORTATION_OPTIONS: ReadonlyArray<TypedSelectOption<Transportation>> = [
    { id: Transportation.Car, value: Transportation.Car, labelKey: 'onboarding.transport.car', icon: '🚗' },
    { id: Transportation.Train, value: Transportation.Train, labelKey: 'onboarding.transport.train', icon: '🚆' },
    { id: Transportation.Bus, value: Transportation.Bus, labelKey: 'onboarding.transport.bus', icon: '🚌' },
    { id: Transportation.Bike, value: Transportation.Bike, labelKey: 'onboarding.transport.bike', icon: '🚲' },
    { id: Transportation.OnFoot, value: Transportation.OnFoot, labelKey: 'onboarding.transport.onFoot', icon: '🚶' },
];

export const ARTIST_OPTIONS: ReadonlyArray<SelectOption> = [
    { id: 'bmth', labelKey: 'onboarding.artist.bmth', icon: '🤘' },
    { id: 'apashe', labelKey: 'onboarding.artist.apashe', icon: '🎻' },
    { id: 'skepta', labelKey: 'onboarding.artist.skepta', icon: '🎤' },
    { id: 'subFocus', labelKey: 'onboarding.artist.subFocus', icon: '🎧' },
    { id: 'tameImpala', labelKey: 'onboarding.artist.tameImpala', icon: '🌀' },
    { id: 'arcticMonkeys', labelKey: 'onboarding.artist.arcticMonkeys', icon: '🎸' },
    { id: 'fkaTwigs', labelKey: 'onboarding.artist.fkaTwigs', icon: '✨' },
    { id: 'fourTet', labelKey: 'onboarding.artist.fourTet', icon: '🎚️' },
    { id: 'massiveAttack', labelKey: 'onboarding.artist.massiveAttack', icon: '🌑' },
    { id: 'idles', labelKey: 'onboarding.artist.idles', icon: '🔥' },
];

export const GENRE_OPTIONS: ReadonlyArray<SelectOption> = [
    { id: 'rock', labelKey: 'onboarding.genre.rock', icon: '🎸' },
    { id: 'electronic', labelKey: 'onboarding.genre.electronic', icon: '🎛️' },
    { id: 'house', labelKey: 'onboarding.genre.house', icon: '🏠' },
    { id: 'techno', labelKey: 'onboarding.genre.techno', icon: '⚙️' },
    { id: 'dnb', labelKey: 'onboarding.genre.dnb', icon: '🥁' },
    { id: 'hipHop', labelKey: 'onboarding.genre.hipHop', icon: '🎤' },
    { id: 'indie', labelKey: 'onboarding.genre.indie', icon: '🎼' },
    { id: 'pop', labelKey: 'onboarding.genre.pop', icon: '🌟' },
    { id: 'metal', labelKey: 'onboarding.genre.metal', icon: '🤘' },
    { id: 'jazz', labelKey: 'onboarding.genre.jazz', icon: '🎷' },
];

export const CUISINE_OPTIONS: ReadonlyArray<SelectOption> = [
    { id: 'italian', labelKey: 'onboarding.cuisine.italian', icon: '🍝' },
    { id: 'romanian', labelKey: 'onboarding.cuisine.romanian', icon: '🥘' },
    { id: 'asian', labelKey: 'onboarding.cuisine.asian', icon: '🍜' },
    { id: 'mexican', labelKey: 'onboarding.cuisine.mexican', icon: '🌮' },
    { id: 'middleEastern', labelKey: 'onboarding.cuisine.middleEastern', icon: '🥙' },
    { id: 'indian', labelKey: 'onboarding.cuisine.indian', icon: '🍛' },
    { id: 'bbq', labelKey: 'onboarding.cuisine.bbq', icon: '🔥' },
    { id: 'vegan', labelKey: 'onboarding.cuisine.vegan', icon: '🥗' },
];

export const ALLERGY_OPTIONS: ReadonlyArray<SelectOption> = [
    { id: 'vegan', labelKey: 'onboarding.allergy.vegan', icon: '🌱' },
    { id: 'vegetarian', labelKey: 'onboarding.allergy.vegetarian', icon: '🥦' },
    { id: 'glutenFree', labelKey: 'onboarding.allergy.glutenFree', icon: '🌾' },
    { id: 'dairyFree', labelKey: 'onboarding.allergy.dairyFree', icon: '🥛' },
    { id: 'nutAllergy', labelKey: 'onboarding.allergy.nutAllergy', icon: '🥜' },
    { id: 'halal', labelKey: 'onboarding.allergy.halal', icon: '☪️' },
    { id: 'kosher', labelKey: 'onboarding.allergy.kosher', icon: '✡️' },
    { id: 'pescatarian', labelKey: 'onboarding.allergy.pescatarian', icon: '🐟' },
];

export const ACTIVITY_OPTIONS: ReadonlyArray<SelectOption> = [
    { id: 'yoga', labelKey: 'onboarding.activity.yoga', icon: '🧘' },
    { id: 'workshops', labelKey: 'onboarding.activity.workshops', icon: '🛠️' },
    { id: 'artTours', labelKey: 'onboarding.activity.artTours', icon: '🎨' },
    { id: 'lakeSwim', labelKey: 'onboarding.activity.lakeSwim', icon: '🏊' },
    { id: 'silentDisco', labelKey: 'onboarding.activity.silentDisco', icon: '🎧' },
    { id: 'foodTour', labelKey: 'onboarding.activity.foodTour', icon: '🍽️' },
    { id: 'wellness', labelKey: 'onboarding.activity.wellness', icon: '💆' },
    { id: 'castleTour', labelKey: 'onboarding.activity.castleTour', icon: '🏰' },
];
