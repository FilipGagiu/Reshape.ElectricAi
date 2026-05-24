import { Routes } from '@angular/router';

import { authGuard, guestGuard } from '@shared/guards/auth.guard';

export const routes: Routes = [
    {
        // Public shareable plan view. Reuses MobileLayout so anonymous
        // viewers still see the app chrome (top bar + bottom nav), with the
        // read-only plan rendered inside. No auth guard — link works for
        // everyone. Parent path is the full `plan/:uuid` segment so `/plan`
        // (no uuid) skips this entry and falls through to the authed group.
        path: 'plan/:uuid',
        loadComponent: () =>
            import('@layout/mobile-layout/mobile-layout').then((m) => m.MobileLayoutComponent),
        children: [
            {
                path: '',
                loadComponent: () =>
                    import('@components/my-ec-plan/plan-results/plan-results.component').then(
                        (m) => m.PlanResultsComponent,
                    ),
            },
        ],
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
                    import('@components/my-ec-plan/my-plan-page/my-plan-page.component').then(
                        (m) => m.MyPlanPageComponent,
                    ),
            },
            { path: 'plan-steps', redirectTo: 'plan' },
            {
                path: 'faq',
                loadComponent: () =>
                    import('@components/faq/faq.component').then((m) => m.FaqComponent),
            },
            {
                path: 'settings',
                loadComponent: () =>
                    import('@components/settings/settings.component').then(
                        (m) => m.SettingsComponent,
                    ),
            },
            {
                path: 'admin',
                loadComponent: () =>
                    import('@components/admin/admin.component').then((m) => m.AdminComponent),
            },
        ],
    },
    { path: '**', redirectTo: '' },
];
