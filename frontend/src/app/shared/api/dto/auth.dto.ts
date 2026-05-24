export type UserRole = 'User' | 'Organizer';

export interface UserDto {
    readonly id: string;
    readonly email: string;
    readonly role: UserRole;
}

export interface AuthResponse {
    readonly accessToken: string;
    readonly refreshToken: string;
    readonly expiresIn: number;
    readonly user: UserDto;
}

export interface RegisterRequest {
    readonly email: string;
    readonly password: string;
}

export interface LoginRequest {
    readonly email: string;
    readonly password: string;
}

export interface RefreshRequest {
    readonly refreshToken: string;
}
