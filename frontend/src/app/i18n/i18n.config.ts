import { isDevMode, provideAppInitializer, inject } from '@angular/core';
import { provideTransloco, TranslocoService } from '@jsverse/transloco';
import { firstValueFrom } from 'rxjs';

import { TranslocoHttpLoader } from './transloco-loader';

export const I18N_AVAILABLE_LANGS = ['en', 'es'] as const;
export type AppLang = (typeof I18N_AVAILABLE_LANGS)[number];

export const I18N_DEFAULT_LANG: AppLang = 'en';
export const I18N_STORAGE_KEY = 'ec-hackaton-lang';

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
            return firstValueFrom(transloco.load(transloco.getActiveLang()));
        }),
    ];
}

export function isAppLang(value: string | null): value is AppLang {
    return value !== null && (I18N_AVAILABLE_LANGS as readonly string[]).includes(value);
}
