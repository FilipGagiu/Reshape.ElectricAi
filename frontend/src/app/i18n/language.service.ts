import { isPlatformBrowser } from '@angular/common';
import { effect, inject, Injectable, PLATFORM_ID, signal } from '@angular/core';
import { TranslocoService } from '@jsverse/transloco';

import { AppLang, I18N_AVAILABLE_LANGS, I18N_DEFAULT_LANG, I18N_STORAGE_KEY, isAppLang } from './i18n.config';

@Injectable({ providedIn: 'root' })
export class LanguageService {
    private readonly platformId = inject(PLATFORM_ID);
    private readonly transloco = inject(TranslocoService);

    readonly availableLangs = I18N_AVAILABLE_LANGS;
    readonly currentLang = signal<AppLang>(this.resolveInitialLang());

    constructor() {
        this.transloco.setActiveLang(this.currentLang());

        effect(() => {
            const lang = this.currentLang();
            this.transloco.setActiveLang(lang);

            if (isPlatformBrowser(this.platformId)) {
                localStorage.setItem(I18N_STORAGE_KEY, lang);
            }
        });
    }

    setLang(lang: AppLang) {
        this.currentLang.set(lang);
    }

    private resolveInitialLang(): AppLang {
        if (!isPlatformBrowser(this.platformId)) {
            return I18N_DEFAULT_LANG;
        }

        const stored = localStorage.getItem(I18N_STORAGE_KEY);
        if (isAppLang(stored)) return stored;

        const browserLang = navigator.language.split('-')[0];
        return isAppLang(browserLang) ? browserLang : I18N_DEFAULT_LANG;
    }
}
