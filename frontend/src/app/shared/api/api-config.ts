import { InjectionToken, Provider } from '@angular/core';

import { environment } from '../../../environments/environment';

export const API_BASE_URL = new InjectionToken<string>('API_BASE_URL', {
    factory: () => environment.apiBaseUrl,
});

export const API_VERSION_PREFIX = '/api/v1';

export function apiUrl(baseUrl: string, path: string): string {
    const normalized = path.startsWith('/') ? path : `/${path}`;
    return `${baseUrl}${API_VERSION_PREFIX}${normalized}`;
}

export function provideApiBaseUrl(baseUrl: string): Provider {
    return { provide: API_BASE_URL, useValue: baseUrl };
}
