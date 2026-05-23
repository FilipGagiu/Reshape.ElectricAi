import { Component, inject } from '@angular/core';
import { ThemesEnum } from '@shared/models/dark-mode.model';
import { DarkModeService } from '@shared/services/dark-mode.service';
import { ButtonModule } from 'primeng/button';
import { TooltipModule } from 'primeng/tooltip';

@Component({
    selector: 'app-dark-mode-switcher',
    imports: [ButtonModule, TooltipModule],
    templateUrl: './dark-mode-switcher.component.html',
    styleUrl: './dark-mode-switcher.component.css',
})
export class DarkModeSwitcherComponent {
    darkModeService = inject(DarkModeService);
    themesEnum = ThemesEnum;
}
