import { ChangeDetectionStrategy, Component, effect, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { Select } from 'primeng/select';

import { AppLang } from './i18n.config';
import { LanguageService } from './language.service';

interface LanguageOption {
    label: string;
    value: AppLang;
}

@Component({
    selector: 'app-language-switcher',
    imports: [Select, ReactiveFormsModule],
    template: `
        <p-select
            [options]="options"
            [formControl]="languageControl"
            optionLabel="label"
            optionValue="value"
            size="small"
            appendTo="body"
            styleClass="!w-24"
        />
    `,
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LanguageSwitcherComponent {
    private readonly languageService = inject(LanguageService);

    protected readonly options: LanguageOption[] = this.languageService.availableLangs.map((lang) => ({
        label: lang.toUpperCase(),
        value: lang,
    }));

    protected readonly languageControl = new FormControl<AppLang>(this.languageService.currentLang(), { nonNullable: true });

    constructor() {
        this.languageControl.valueChanges.pipe(takeUntilDestroyed()).subscribe((value) => {
            this.languageService.setLang(value);
        });

        effect(() => {
            const current = this.languageService.currentLang();
            if (this.languageControl.value !== current) {
                this.languageControl.setValue(current, { emitEvent: false });
            }
        });
    }
}
