import { Routes } from '@angular/router';

import { authGuard, guestGuard } from '@shared/guards/auth.guard';

export const routes: Routes = [
    {
        // Public stories viewer — accepts a UUID, fetches the plan, renders
        // the Spotify-Wrapped-style slideshow. No layout chrome (fully
        // immersive). No auth guard so share links work for friends without
        // an account.
        path: 'p/:uuid',
        loadComponent: () =>
            import('@components/plan-share/stories-viewer.component').then(
                (m) => m.StoriesViewerComponent,
            ),
    },
    {
        path: '',
        canMatch: [guestGuard],
        loadComponent: () =>
            import('@layout/auth-layout/auth-layout').then((m) => m.AuthLayoutComponent),
        children: [
            {
                path: 'login',
                loadComponent: () =>
                    import('@components/auth/login/login.component').then((m) => m.LoginComponent),
            },
            {
                path: 'register',
                loadComponent: () =>
                    import('@components/auth/register/register.component').then(
                        (m) => m.RegisterComponent,
                    ),
            },
            { path: '', pathMatch: 'full', redirectTo: 'login' },
        ],
    },
    {
        path: '',
        canMatch: [authGuard],
        loadComponent: () =>
            import('@layout/mobile-layout/mobile-layout').then((m) => m.MobileLayoutComponent),
        children: [
            {
                path: '',
                loadComponent: () =>
                    import('@components/live-feed/live-feed.component').then(
                        (m) => m.LiveFeedComponent,
                    ),
            },
            {
                path: 'questions',
                loadComponent: () =>
                    import('@components/questions-chat/questions-chat.component').then(
                        (m) => m.QuestionsChatComponent,
                    ),
            },
            {
                path: 'plan',
                loadComponent: () =>
                    import('@components/my-ec-plan/my-ec-plan.component').then(
                        (m) => m.MyEcPlanComponent,
                    ),
            },
        ],
    },
    { path: '**', redirectTo: '' },
];
