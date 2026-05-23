import { isPlatformBrowser } from '@angular/common';
import { effect, inject, Injectable, PLATFORM_ID, signal } from '@angular/core';
import { ThemesEnum } from '@shared/models/dark-mode.model';

@Injectable({
    providedIn: 'root',
})
export class DarkModeService {
    private readonly platformId = inject(PLATFORM_ID);
    private mediaQueryList?: MediaQueryList;
    private userPreference = signal<boolean>(false); // Track if user has manually set preference

    currentTheme = signal<ThemesEnum>(ThemesEnum.LIGHT);

    constructor() {
        // Initialize theme immediately (sync with early detection)
        if (isPlatformBrowser(this.platformId)) {
            this.syncWithEarlyDetection();
            this.listenToSystemChanges();
        }

        // Apply theme changes to DOM
        effect(() => {
            if (!isPlatformBrowser(this.platformId)) return;

            const currentTheme = this.currentTheme();
            const element = document.documentElement;

            if (currentTheme === ThemesEnum.DARK) {
                element.classList.add('ec-hackaton-dark');
            } else {
                element.classList.remove('ec-hackaton-dark');
            }
        });
    }

    private syncWithEarlyDetection() {
        // Check if early detection already applied theme
        const htmlElement = document.documentElement;
        const hasEarlyDark = htmlElement.classList.contains('ec-hackaton-dark');

        // Sync our state with what early detection determined
        const localStorageTheme = localStorage.getItem('ec-hackaton-theme-mode') as ThemesEnum;

        if (localStorageTheme) {
            // User has saved preference
            this.userPreference.set(true);
            this.currentTheme.set(localStorageTheme);
        } else {
            // Following system preference
            this.userPreference.set(false);
            this.currentTheme.set(hasEarlyDark ? ThemesEnum.DARK : ThemesEnum.LIGHT);
        }
    }

    private initializeTheme() {
        const localStorageTheme = localStorage.getItem('ec-hackaton-theme-mode') as ThemesEnum;
        if (localStorageTheme) {
            // User has a saved preference
            this.userPreference.set(true);
            this.currentTheme.set(localStorageTheme);
        } else {
            // No saved preference, use system preference
            this.userPreference.set(false);
            const userPrefersDark = window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches;
            this.currentTheme.set(userPrefersDark ? ThemesEnum.DARK : ThemesEnum.LIGHT);
        }
    }

    private listenToSystemChanges() {
        if (!isPlatformBrowser(this.platformId)) {
            return;
        }

        this.mediaQueryList = window.matchMedia('(prefers-color-scheme: dark)');

        // Listen for changes to system color scheme preference
        const handleSystemThemeChange = (event: MediaQueryListEvent) => {
            // Only auto-switch if user hasn't manually set a preference
            if (!this.userPreference()) {
                this.currentTheme.set(event.matches ? ThemesEnum.DARK : ThemesEnum.LIGHT);
            }
        };

        // Modern browsers
        if (this.mediaQueryList.addEventListener) {
            this.mediaQueryList.addEventListener('change', handleSystemThemeChange);
        } else {
            // Fallback for older browsers
            this.mediaQueryList.addListener(handleSystemThemeChange);
        }
    }

    checkLocalStorageTheme() {
        if (!isPlatformBrowser(this.platformId)) {
            return;
        }

        const localStorageTheme = localStorage.getItem('ec-hackaton-theme-mode') as ThemesEnum;
        if (localStorageTheme) {
            this.userPreference.set(true);
            this.currentTheme.set(localStorageTheme);
        } else {
            this.userPreference.set(false);
            const userPrefersDark = window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches;
            this.currentTheme.set(userPrefersDark ? ThemesEnum.DARK : ThemesEnum.LIGHT);
        }
    }

    toggleTheme() {
        // Mark as user preference when manually toggling
        this.userPreference.set(true);
        const newTheme = this.currentTheme() === ThemesEnum.LIGHT ? ThemesEnum.DARK : ThemesEnum.LIGHT;
        this.currentTheme.set(newTheme);

        // Only save to localStorage when user manually toggles
        if (isPlatformBrowser(this.platformId)) {
            localStorage.setItem('ec-hackaton-theme-mode', newTheme);
        }
    }

    /**
     * Reset to system preference (removes user preference)
     */
    resetToSystemPreference() {
        if (!isPlatformBrowser(this.platformId)) {
            return;
        }

        localStorage.removeItem('ec-hackaton-theme-mode');
        this.userPreference.set(false);

        const userPrefersDark = window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches;
        this.currentTheme.set(userPrefersDark ? ThemesEnum.DARK : ThemesEnum.LIGHT);
    }

    /**
     * Get current system preference without changing current theme
     */
    getSystemPreference(): ThemesEnum {
        if (!isPlatformBrowser(this.platformId)) {
            return ThemesEnum.LIGHT;
        }

        const userPrefersDark = window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches;
        return userPrefersDark ? ThemesEnum.DARK : ThemesEnum.LIGHT;
    }

    /**
     * Check if current theme matches system preference
     */
    isFollowingSystemPreference(): boolean {
        return !this.userPreference();
    }

    /**
     * Cleanup method for destroying the service
     */
    ngOnDestroy() {
        if (this.mediaQueryList) {
            // Clean up event listeners
            if (this.mediaQueryList.removeEventListener) {
                this.mediaQueryList.removeEventListener('change', () => {});
            } else {
                this.mediaQueryList.removeListener(() => {});
            }
        }
    }
}
