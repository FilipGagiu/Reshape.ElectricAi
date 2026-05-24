import { isDevMode, provideAppInitializer, inject } from '@angular/core';
import { provideTransloco, TranslocoService } from '@jsverse/transloco';
import { firstValueFrom } from 'rxjs';

import { TranslocoHttpLoader } from './transloco-loader';

export const I18N_AVAILABLE_LANGS = ['en', 'ro'] as const;
export type AppLang = (typeof I18N_AVAILABLE_LANGS)[number];

export const I18N_DEFAULT_LANG: AppLang = 'en';
export const I18N_STORAGE_KEY = 'ec-hackaton-lang';

/** Temp: when false, app is hardcoded to EN and the switcher button is hidden. */
export const LANGUAGE_SWITCHER_ENABLED = false;

export function provideI18n() {
    return [
        provideTransloco({
            config: {
                availableLangs: [...I18N_AVAILABLE_LANGS],
                defaultLang: I18N_DEFAULT_LANG,
                fallbackLang: I18N_DEFAULT_LANG,
                reRenderOnLangChange: true,
                prodMode: !isDevMode(),
            },
            loader: TranslocoHttpLoader,
        }),
        provideAppInitializer(() => {
            const transloco = inject(TranslocoService);

            if (LANGUAGE_SWITCHER_ENABLED && typeof localStorage !== 'undefined') {
                const stored = localStorage.getItem(I18N_STORAGE_KEY);
                if (isAppLang(stored)) {
                    transloco.setActiveLang(stored);
                }
            }

            return firstValueFrom(transloco.load(transloco.getActiveLang()));
        }),
    ];
}

export function isAppLang(value: string | null): value is AppLang {
    return value !== null && (I18N_AVAILABLE_LANGS as readonly string[]).includes(value);
}
