# ec-hackaton

Angular 21 + PrimeNG 21 + Tailwind 4. Standalone components, signals, OnPush, zoneless change detection.

## Develop

```bash
npm install
npm start                    # dev server at http://localhost:4200
npm run build                # production build
npm test                     # karma + jasmine
```

## Project layout

```
src/
├── app/
│   ├── components/          # route-level components (home, ...)
│   ├── config/              # app config, routes, theme, app-info constants
│   ├── i18n/                # Transloco loader, language service, switcher
│   ├── layout/              # sidebar layout + layout service
│   └── shared/              # reusable services + components (dark-mode, ...)
├── styles.css               # Tailwind + PrimeNG tokens + global styles
└── index.html
public/
└── i18n/                    # translation JSONs
```

## Customizing

- **App name** — `src/app/config/app-info.ts` (`APP_NAME` + `APP_NAME_DISPLAY`). Used by sidebar logo + browser tab title.
- **Theme** — `src/app/config/theme.ts` (PrimeNG preset overriding Aura).
- **Routes** — `src/app/config/app.routes.ts`.
- **Nav menu** — `navGroups` array in `src/app/layout/sidebar-layout/sidebar-layout.ts`.
- **Translations** — `public/i18n/<lang>.json`. Add a language: drop `<lang>.json`, append it to `I18N_AVAILABLE_LANGS` in `src/app/i18n/i18n.config.ts`.

## Conventions

- Standalone components (no NgModules)
- Signals for state, `computed()` for derived state
- `inject()` over constructor injection
- `@if` / `@for` / `@switch` over structural directives
- Reactive Forms over template-driven (no `ngModel`)
- `class`/`style` bindings over `ngClass`/`ngStyle`
- `ChangeDetectionStrategy.OnPush` everywhere
