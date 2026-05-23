import { inject } from '@angular/core';
import { CanMatchFn } from '@angular/router';

import { AuthService } from '@shared/services/auth.service';

export const authGuard: CanMatchFn = () => inject(AuthService).isAuthenticated();

export const guestGuard: CanMatchFn = () => !inject(AuthService).isAuthenticated();
