import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { API_BASE_URL, apiUrl } from './api-config';
import {
    AuthResponse,
    LoginRequest,
    RefreshRequest,
    RegisterRequest,
    UserDto,
} from './dto/auth.dto';

@Injectable({ providedIn: 'root' })
export class AuthApi {
    private readonly http = inject(HttpClient);
    private readonly baseUrl = inject(API_BASE_URL);

    register(payload: RegisterRequest): Observable<AuthResponse> {
        return this.http.post<AuthResponse>(apiUrl(this.baseUrl, '/auth/register'), payload);
    }

    login(payload: LoginRequest): Observable<AuthResponse> {
        return this.http.post<AuthResponse>(apiUrl(this.baseUrl, '/auth/login'), payload);
    }

    refresh(payload: RefreshRequest): Observable<AuthResponse> {
        return this.http.post<AuthResponse>(apiUrl(this.baseUrl, '/auth/refresh'), payload);
    }

    me(): Observable<UserDto> {
        return this.http.get<UserDto>(apiUrl(this.baseUrl, '/auth/me'));
    }
}
