import { Injectable, signal } from '@angular/core';

export type NotificationPermissionState = NotificationPermission | 'unsupported';

export interface DemoNotificationOptions {
    title: string;
    body: string;
    delayMs?: number;
}

@Injectable({ providedIn: 'root' })
export class NotificationService {
    readonly permission = signal<NotificationPermissionState>(this.readInitialPermission());

    async requestPermission(): Promise<NotificationPermissionState> {
        if (!this.isSupported()) {
            this.permission.set('unsupported');
            return 'unsupported';
        }
        const result = await Notification.requestPermission();
        this.permission.set(result);
        return result;
    }

    async showDemo({ title, body, delayMs = 0 }: DemoNotificationOptions): Promise<void> {
        if (!this.isSupported()) {
            return;
        }
        if (this.permission() !== 'granted') {
            const result = await this.requestPermission();
            if (result !== 'granted') {
                return;
            }
        }
        const options: NotificationOptions = {
            body,
            icon: '/icons/icon-192x192.png',
            badge: '/icons/icon-96x96.png',
            tag: 'ec-demo',
            data: { url: '/' },
        };
        const registration = await this.getServiceWorkerRegistration();
        const fire = (): void => {
            if (registration) {
                registration.showNotification(title, options);
                return;
            }
            new Notification(title, options);
        };

        if (delayMs > 0) {
            setTimeout(fire, delayMs);
            return;
        }
        fire();
    }

    private async getServiceWorkerRegistration(): Promise<ServiceWorkerRegistration | null> {
        if (!('serviceWorker' in navigator)) {
            return null;
        }
        const existing = await navigator.serviceWorker.getRegistration();
        if (!existing) {
            return null;
        }
        return navigator.serviceWorker.ready;
    }

    private isSupported(): boolean {
        return (
            typeof window !== 'undefined' &&
            'Notification' in window &&
            'serviceWorker' in navigator
        );
    }

    private readInitialPermission(): NotificationPermissionState {
        if (typeof window === 'undefined' || !('Notification' in window)) {
            return 'unsupported';
        }
        return Notification.permission;
    }
}
