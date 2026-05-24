export interface VapidPublicKeyResponse {
    readonly publicKey: string;
}

export interface SubscribeRequest {
    readonly endpoint: string;
    readonly p256dh: string;
    readonly auth: string;
    readonly userAgent?: string;
}

export interface UnsubscribeRequest {
    readonly endpoint: string;
}

export interface SendRequest {
    readonly title: string;
    readonly body: string;
    readonly icon?: string;
    readonly badge?: string;
    readonly url?: string;
}

export interface SendResult {
    readonly delivered: number;
    readonly pruned: number;
    readonly failed: number;
}
