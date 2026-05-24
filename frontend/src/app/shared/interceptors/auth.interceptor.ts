import {
    HttpContextToken,
    HttpErrorResponse,
    HttpEvent,
    HttpInterceptorFn,
    HttpRequest,
} from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, from, Observable, switchMap, throwError } from 'rxjs';

import { AuthService } from '@shared/services/auth.service';
import { API_BASE_URL } from '@shared/tokens/api-base-url.token';

const RETRIED = new HttpContextToken<boolean>(() => false);

export const authInterceptor: HttpInterceptorFn = (req, next) => {
    const authService = inject(AuthService);
    const apiBaseUrl = inject(API_BASE_URL);
    const router = inject(Router);

    if (!req.url.startsWith(apiBaseUrl)) {
        return next(req);
    }

    const isRefreshCall = req.url.endsWith('/api/v1/auth/refresh');
    const authorized = withBearer(req, authService.accessToken());

    return next(authorized).pipe(
        catchError((error: unknown): Observable<HttpEvent<unknown>> => {
            if (!(error instanceof HttpErrorResponse) || error.status !== 401) {
                return throwError(() => error);
            }
            if (isRefreshCall || authorized.context.get(RETRIED)) {
                authService.logout();
                void router.navigateByUrl('/login');
                return throwError(() => error);
            }
            return from(authService.refresh()).pipe(
                switchMap((ok) => {
                    if (!ok) {
                        void router.navigateByUrl('/login');
                        return throwError(() => error);
                    }
                    const retried = withBearer(req, authService.accessToken()).clone({
                        context: req.context.set(RETRIED, true),
                    });
                    return next(retried);
                }),
            );
        }),
    );
};

function withBearer<T>(req: HttpRequest<T>, token: string | null): HttpRequest<T> {
    if (token === null) {
        return req;
    }
    return req.clone({
        setHeaders: { Authorization: `Bearer ${token}` },
    });
}
