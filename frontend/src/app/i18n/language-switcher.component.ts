import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';

import { AppLang } from './i18n.config';
import { LanguageService } from './language.service';

const FLAG_BY_LANG: Readonly<Record<AppLang, string>> = {
    en: '🇬🇧',
    ro: '🇷🇴',
};

@Component({
    selector: 'app-language-switcher',
    template: `
        <button
            type="button"
            class="lang-flag-btn"
            (click)="toggle()"
            [attr.aria-label]="'Switch language to ' + nextLangLabel()"
        >
            <span class="lang-flag-btn__flag" aria-hidden="true">{{ currentFlag() }}</span>
        </button>
    `,
    styles: [
        `
            :host {
                display: inline-flex;
            }
            .lang-flag-btn {
                width: 36px;
                height: 36px;
                display: inline-flex;
                align-items: center;
                justify-content: center;
                padding: 0;
                background-color: var(--ec-dark-navy, #0f1428);
                border: 1px solid rgba(255, 255, 255, 0.3);
                border-radius: var(--radius-none, 0);
                cursor: pointer;
                transition: background-color 120ms ease, border-color 120ms ease;
            }
            .lang-flag-btn:hover,
            .lang-flag-btn:focus-visible {
                background-color: rgba(15, 20, 40, 0.85);
                border-color: rgba(255, 255, 255, 0.6);
            }
            .lang-flag-btn__flag {
                font-size: 20px;
                line-height: 1;
            }
        `,
    ],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LanguageSwitcherComponent {
    private readonly languageService = inject(LanguageService);

    protected readonly currentFlag = computed(() => FLAG_BY_LANG[this.languageService.currentLang()]);
    protected readonly nextLangLabel = computed(() =>
        this.languageService.currentLang() === 'en' ? 'Romanian' : 'English',
    );

    protected toggle(): void {
        const current = this.languageService.currentLang();
        this.languageService.setLang(current === 'en' ? 'ro' : 'en');
    }
}
