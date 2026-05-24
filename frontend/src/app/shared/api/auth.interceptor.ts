import {
    HttpErrorResponse,
    HttpHandlerFn,
    HttpInterceptorFn,
    HttpRequest,
} from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { Observable, catchError, switchMap, throwError } from 'rxjs';

import { API_BASE_URL, apiUrl } from './api-config';
import { AuthApi } from './auth-api';
import { TokenStore } from './token-store';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
    const baseUrl = inject(API_BASE_URL);
    if (!isApiRequest(req, baseUrl)) {
        return next(req);
    }

    const tokens = inject(TokenStore);
    const router = inject(Router);
    const authApi = inject(AuthApi);
    const refreshUrl = apiUrl(baseUrl, '/auth/refresh');
    const isRefreshCall = req.url === refreshUrl;

    const accessToken = tokens.accessToken();
    const outgoing = accessToken && !isRefreshCall ? withBearer(req, accessToken) : req;

    return next(outgoing).pipe(
        catchError((err) => {
            if (!(err instanceof HttpErrorResponse) || err.status !== 401 || isRefreshCall) {
                return throwError(() => err);
            }
            return handleUnauthorized(req, next, tokens, authApi, router);
        }),
    );
};

function handleUnauthorized(
    req: HttpRequest<unknown>,
    next: HttpHandlerFn,
    tokens: TokenStore,
    authApi: AuthApi,
    router: Router,
): Observable<ReturnType<HttpHandlerFn> extends Observable<infer T> ? T : never> {
    const refreshToken = tokens.refreshToken();
    if (!refreshToken) {
        clearAndRedirect(tokens, router);
        return throwError(
            () => new HttpErrorResponse({ status: 401, statusText: 'No refresh token' }),
        );
    }

    return authApi.refresh({ refreshToken }).pipe(
        switchMap((response) => {
            tokens.rotate(response.accessToken, response.refreshToken);
            tokens.setUser(response.user);
            return next(withBearer(req, response.accessToken));
        }),
        catchError((refreshErr) => {
            clearAndRedirect(tokens, router);
            return throwError(() => refreshErr);
        }),
    );
}

function isApiRequest(req: HttpRequest<unknown>, baseUrl: string): boolean {
    if (req.url.startsWith(baseUrl)) return true;
    if (baseUrl.startsWith('/') && req.url.startsWith(baseUrl)) return true;
    return false;
}

function withBearer(req: HttpRequest<unknown>, token: string): HttpRequest<unknown> {
    return req.clone({ setHeaders: { Authorization: `Bearer ${token}` } });
}

function clearAndRedirect(tokens: TokenStore, router: Router): void {
    tokens.clear();
    void router.navigateByUrl('/login');
}
