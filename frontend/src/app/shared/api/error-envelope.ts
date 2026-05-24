import { HttpErrorResponse } from '@angular/common/http';

export interface ErrorEnvelope {
    readonly code: string;
    readonly message: string;
    readonly details?: Readonly<Record<string, unknown>>;
}

export interface ErrorEnvelopeWrapper {
    readonly error: ErrorEnvelope;
}

export const ErrorCode = {
    ValidationFailed: 'validation-failed',
    Unauthorized: 'unauthorized',
    Forbidden: 'forbidden',
    NotFound: 'not-found',
    Conflict: 'conflict',
    PreferencesInsufficient: 'preferences-insufficient',
    ChatBudgetExceeded: 'chat-budget-exceeded',
    InternalError: 'internal-error',
    NetworkError: 'network-error',
    Unknown: 'unknown',
} as const;

export type ErrorCodeValue = (typeof ErrorCode)[keyof typeof ErrorCode];

export function extractErrorEnvelope(err: unknown): ErrorEnvelope {
    if (err instanceof HttpErrorResponse) {
        if (err.status === 0) {
            return { code: ErrorCode.NetworkError, message: 'Network unreachable.' };
        }
        const body = err.error as ErrorEnvelopeWrapper | ErrorEnvelope | string | null;
        if (isWrappedEnvelope(body)) return body.error;
        if (isEnvelope(body)) return body;
        return { code: ErrorCode.Unknown, message: err.message || `HTTP ${err.status}` };
    }
    if (err instanceof Error) {
        return { code: ErrorCode.Unknown, message: err.message };
    }
    return { code: ErrorCode.Unknown, message: 'Unknown error.' };
}

function isWrappedEnvelope(body: unknown): body is ErrorEnvelopeWrapper {
    return (
        typeof body === 'object' &&
        body !== null &&
        'error' in body &&
        isEnvelope((body as ErrorEnvelopeWrapper).error)
    );
}

function isEnvelope(body: unknown): body is ErrorEnvelope {
    return (
        typeof body === 'object' &&
        body !== null &&
        typeof (body as ErrorEnvelope).code === 'string' &&
        typeof (body as ErrorEnvelope).message === 'string'
    );
}
