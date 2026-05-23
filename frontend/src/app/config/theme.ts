import { definePreset } from '@primeuix/themes';
import Aura from '@primeuix/themes/aura';

export const EcHackatonTheme = definePreset(Aura, {
    semantic: {
        primary: {
            '50': '#fff0f0',
            '100': '#ffdcdc',
            '200': '#ffb8b8',
            '300': '#ff8a8a',
            '400': '#ff5252',
            '500': '#ff0000',
            '600': '#d40000',
            '700': '#aa0000',
            '800': '#800000',
            '900': '#5c0000',
            '950': '#330000',
        },
        accent: {
            '50': '#edfcf6',
            '100': '#d5f6e7',
            '200': '#aeecd4',
            '300': '#78ddbc',
            '400': '#3abe97',
            '500': '#1faa86',
            '600': '#12896c',
            '700': '#0e6e59',
            '800': '#0e5748',
            '900': '#0c483c',
            '950': '#062822',
        },
        borderRadius: {
            none: '0',
            xs: '0',
            sm: '0',
            md: '0',
            lg: '0',
            xl: '0',
        },
    },
});
