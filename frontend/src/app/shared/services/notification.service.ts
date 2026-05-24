import { Injectable, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';

import { PushApi } from '@shared/api/push-api';
import { extractErrorEnvelope } from '@shared/api/error-envelope';

export type NotificationPermissionState = NotificationPermission | 'unsupported';

export interface DemoNotificationOptions {
    title: string;
    body: string;
    delayMs?: number;
}

export type PushSubscribeStatus =
    | 'subscribed'
    | 'permission-denied'
    | 'unsupported'
    | 'failed';

@Injectable({ providedIn: 'root' })
export class NotificationService {
    private readonly pushApi = inject(PushApi);

    readonly permission = signal<NotificationPermissionState>(this.readInitialPermission());
    readonly pushSubscribed = signal<boolean>(false);

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

    async enablePush(): Promise<PushSubscribeStatus> {
        if (!this.isPushSupported()) return 'unsupported';
        if (this.permission() !== 'granted') {
            const granted = await this.requestPermission();
            if (granted !== 'granted') return 'permission-denied';
        }
        try {
            const registration = await this.getServiceWorkerRegistration();
            if (!registration) return 'unsupported';
            const existing = await registration.pushManager.getSubscription();
            const subscription = existing ?? await this.subscribeOnServer(registration);
            if (!subscription) return 'failed';
            await firstValueFrom(this.pushApi.subscribe(this.toSubscribePayload(subscription)));
            this.pushSubscribed.set(true);
            return 'subscribed';
        } catch (err) {
            const envelope = extractErrorEnvelope(err);
            console.warn('[push] enable failed', envelope);
            return 'failed';
        }
    }

    async disablePush(): Promise<void> {
        if (!this.isPushSupported()) return;
        try {
            const registration = await this.getServiceWorkerRegistration();
            const subscription = await registration?.pushManager.getSubscription();
            if (!subscription) {
                this.pushSubscribed.set(false);
                return;
            }
            await firstValueFrom(this.pushApi.unsubscribe({ endpoint: subscription.endpoint }));
            await subscription.unsubscribe();
            this.pushSubscribed.set(false);
        } catch (err) {
            console.warn('[push] disable failed', extractErrorEnvelope(err));
        }
    }

    private async subscribeOnServer(
        registration: ServiceWorkerRegistration,
    ): Promise<PushSubscription | null> {
        const { publicKey } = await firstValueFrom(this.pushApi.getPublicKey());
        return await registration.pushManager.subscribe({
            userVisibleOnly: true,
            applicationServerKey: urlBase64ToUint8Array(publicKey),
        });
    }

    private toSubscribePayload(subscription: PushSubscription): {
        endpoint: string;
        p256dh: string;
        auth: string;
        userAgent: string;
    } {
        const json = subscription.toJSON();
        const keys = (json.keys ?? {}) as { p256dh?: string; auth?: string };
        return {
            endpoint: subscription.endpoint,
            p256dh: keys.p256dh ?? '',
            auth: keys.auth ?? '',
            userAgent: navigator.userAgent,
        };
    }

    private isPushSupported(): boolean {
        return this.isSupported() && 'PushManager' in window;
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

function urlBase64ToUint8Array(base64String: string): Uint8Array<ArrayBuffer> {
    const padding = '='.repeat((4 - (base64String.length % 4)) % 4);
    const base64 = (base64String + padding).replace(/-/g, '+').replace(/_/g, '/');
    const rawData = atob(base64);
    const buffer = new ArrayBuffer(rawData.length);
    const output = new Uint8Array(buffer);
    for (let i = 0; i < rawData.length; i += 1) {
        output[i] = rawData.charCodeAt(i);
    }
    return output;
}
