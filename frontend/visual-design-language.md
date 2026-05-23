# Electric Castle: Visual Design Language

> Comprehensive reference for app & web UI design. Based on full visual audit of `electriccastle.ro` and an internal design review.
>
> **This file is the source of truth for Electric Castle's visual identity.** Every frontend UI change (Angular components, styles, icons, layout) MUST conform to the rules here. Re-read before any FE edit. See [CLAUDE.md](../CLAUDE.md) §6b.
>
> Companion file: [text-copy-design-language.md](text-copy-design-language.md) (voice, tone, EN+RO copy, project overrides).
>
> Sources:
> - `electric-castle-visual-design-language.pdf` (16 pages, Volume II, May 2026, AI-generated brand audit).
> - Internal designer audit (ui-designer agent, 2026-05).

---

## Table of Contents

00. [Design Tokens (foundation)](#00-design-tokens-foundation)
01. [Color System](#01-color-system)
02. [Typography](#02-typography)
03. [Logo & Brand Mark](#03-logo--brand-mark)
04. [Button System](#04-button-system)
05. [Card Components](#05-card-components)
06. [Navigation Components](#06-navigation-components)
07. [Layout System & Spacing](#07-layout-system--spacing)
08. [Iconography](#08-iconography)
09. [Imagery Style & Photography](#09-imagery-style--photography)
10. [Color-Coded Pass Identification System](#10-color-coded-pass-identification-system)
11. [Stage Identity System](#11-stage-identity-system)
12. [Accessibility & Minimum Standards](#12-accessibility--minimum-standards)
13. [EC Radio Player (Persistent UI)](#13-ec-radio-player-persistent-ui)
14. [Form UI Patterns](#14-form-ui-patterns)
15. [Dark Mode (EC's Primary Aesthetic)](#15-dark-mode-ecs-primary-aesthetic)
16. [Sustainability Visual Identity](#16-sustainability-visual-identity)
17. [EC App UI Patterns](#17-ec-app-ui-patterns)
18. [Modal & Dialog System](#18-modal--dialog-system)
19. [Toast & Snackbar System](#19-toast--snackbar-system)
20. [Skeleton Loading & Spinners](#20-skeleton-loading--spinners)
21. [Empty State Pattern](#21-empty-state-pattern)
22. [Error Page Templates](#22-error-page-templates)
23. [Brand Asset Specs](#23-brand-asset-specs)

---

## 00. Design Tokens (foundation)

Every component spec in this file refers back to these tokens. Magic numbers in component CSS are a bug. If a value isn't here, add it here first.

CSS variable names ship in [src/styles.css](src/styles.css) under `:root` (light defaults) and `.ec-hackaton-dark` (dark overrides). Tailwind users can read them via `bg-[var(--ec-yellow)]` or similar arbitrary-value syntax.

### Spacing scale (4-base grid)

| Token | px | Common use |
|---|---|---|
| `--space-1` | 4 | Tight gap, icon padding |
| `--space-2` | 8 | Default gap between adjacent labels and inputs |
| `--space-3` | 12 | Card internal padding (small) |
| `--space-4` | 16 | Default card gap, button padding-x |
| `--space-5` | 20 | Section gap (mobile) |
| `--space-6` | 24 | Card padding (default), mobile horizontal margin |
| `--space-8` | 32 | Form field gap (loose), modal padding |
| `--space-10` | 40 | Section padding-y (mobile) |
| `--space-12` | 48 | Section padding-y (desktop) |
| `--space-16` | 64 | Hero section gap |
| `--space-20` | 80 | Desktop page margin |
| `--space-24` | 96 | Large hero |

**Rule:** never use a non-token px value for spacing. If you need 18px, find the closest token (16 or 20) and align.

### Border-radius scale (THE square-corner rule)

| Token | px | Allowed surfaces |
|---|---|---|
| `--radius-none` | **0** | **Default for everything.** Cards, inputs, buttons, modals, toasts, dropdowns, badges, EC Radio bar, sections, tables, list rows. |
| `--radius-sm` | 2 | EC logo monogram, small status pills (e.g. "DETAILS" tag), favicon. |
| `--radius-pill` | 9999 | Reserved. Approval required. Today only the dark-mode-switcher thumb and certain Pass-card status chips may use this. |

**Global rule: EC is a square-corners brand.** This is intentional and identity-defining. Any radius beyond `--radius-sm` (2px) requires explicit design sign-off and a note in this section. If you find yourself reaching for 4px / 8px / 12px / 16px while building an EC surface, you are drifting from the brand. Use `--radius-none` and re-examine.

The exceptions documented above (logo, small pills) exist for visual softening of dense red blocks of brand mark, not to introduce friendliness through curvature. EC's warmth comes from copy and color, not from rounded shapes.

### Elevation / shadow tokens

| Token | Value | When to use |
|---|---|---|
| `--shadow-1` | `0 6px 18px rgba(15, 20, 40, 0.18)` | Cards (default), ticket rows. |
| `--shadow-2` | `0 8px 24px rgba(15, 20, 40, 0.22)` | Dropdowns, popovers, hover-elevated cards. |
| `--shadow-3` | `0 16px 40px rgba(15, 20, 40, 0.32)` | Modals, full-screen overlays. |
| `--shadow-4` | `0 10px 30px rgba(15, 20, 40, 0.4)` | Toasts (float above all content). |
| `--shadow-glow-red` | `0 6px 18px rgba(220, 53, 41, 0.35)` | EC logo monogram (signature glow). |

On dark backgrounds shadows are subtler but never absent. Light backgrounds get the full opacity. Browsers compositing shadows on `#0F1428` show roughly half the apparent depth; that is acceptable.

### Motion tokens

| Token | Duration | Use |
|---|---|---|
| `--duration-fast` | 100ms | Hover-state color transitions, pressed-state feedback. |
| `--duration-base` | 150ms | Default for most UI transitions (button hover, link color shift). |
| `--duration-slow` | 250ms | Modal/drawer enter, card hover lift. |
| `--duration-deliberate` | 400ms | Page transitions, hero text reveals (use sparingly). |

| Token | Easing | Use |
|---|---|---|
| `--ease-out` | `cubic-bezier(0.2, 0.8, 0.2, 1)` | Default for entering elements (modal in, toast in). |
| `--ease-in-out` | `cubic-bezier(0.4, 0, 0.2, 1)` | Default for continuous motion (page transition, slider). |
| `--ease-emphasized` | `cubic-bezier(0.2, 0.0, 0, 1.0)` | Hero text appearance, dramatic reveals. |

**Rule:** transitions over 400ms must be approved. Festival energy is decisive, not lingering. Reduce all durations by 50% when `prefers-reduced-motion: reduce` is set.

### Z-index scale

| Token | Value | Layer |
|---|---|---|
| `--z-base` | 1 | Default in-flow elements. |
| `--z-sticky` | 100 | Sticky header, sticky filter bar. |
| `--z-dropdown` | 200 | Select dropdowns, autocomplete popovers. |
| `--z-modal-backdrop` | 300 | Modal scrim. |
| `--z-modal` | 400 | Modal content. |
| `--z-toast` | 500 | Toast/snackbar (floats above modals so error confirmations are visible). |
| `--z-tooltip` | 600 | Tooltips (top of stack). |

### Opacity scale

| Token | Value | Use |
|---|---|---|
| `--opacity-disabled` | 0.45 | Disabled buttons, disabled form fields. |
| `--opacity-hover-overlay` | 0.06 | Subtle hover wash on cards / list rows. |
| `--opacity-active-overlay` | 0.10 | Pressed/active state on cards. |
| `--opacity-scrim` | 0.6 | Modal backdrop tint on dark navy. |
| `--opacity-readonly` | 0.7 | Read-only inputs. |

### Breakpoint tokens

| Name | min-width | Typical device |
|---|---|---|
| `xs` | (default) | <640 px. Small phones. |
| `sm` | 640 px | Large phones. |
| `md` | 768 px | Tablets, narrow desktop. |
| `lg` | 1024 px | Desktop. |
| `xl` | 1280 px | Wide desktop. |
| `2xl` | 1536 px | Ultra-wide / 4K. |

Use Tailwind's `sm:` `md:` `lg:` `xl:` `2xl:` prefixes; they match these breakpoints. For Angular media queries that need raw CSS, use `@media (min-width: 768px) { … }`.

Max content width: 1280 px (no surface should be wider than this unless full-bleed hero / image background). Page horizontal margins:
- xs–sm: 24 px
- md: 40 px
- lg+: 80 px

### Focus ring

| Surface state | Spec |
|---|---|
| `:focus-visible` (default) | `outline: none; box-shadow: 0 0 0 3px rgba(220, 53, 41, 0.35);` |
| `:focus-visible` on EC Red surface | Switch ring to EC Yellow at same opacity: `0 0 0 3px rgba(255, 230, 0, 0.5);` |
| Inputs `:focus-visible` | Same EC Red glow + border color shift to `var(--ec-red)`. |

Never rely on browser-default outlines. Always honor `:focus-visible` (not `:focus`) so keyboard-only users get the ring and mouse users don't. WCAG 2.4.11 (Focus Not Obscured) and 2.4.13 (Focus Appearance) compliance baseline.

### Hit target

| Platform | Minimum |
|---|---|
| Web (mouse + keyboard) | 44 × 44 px |
| Web (touch-enabled / tablet) | 48 × 48 px |
| Mobile native (iOS / Android) | 48 × 48 px |
| Bottom-nav tab (mobile) | 56 px height |

These reconcile §12 Accessibility (44×44 baseline) and §17 EC App UI Patterns (48×48 for native mobile). Use 48×48 anywhere a touch interaction is plausible. CTA buttons across the app use **48 px minimum height** regardless of platform.

---

## 01. Color System

### Primary Palette

Electric Castle uses a bold, high-contrast palette anchored by a vivid red and a deep navy dark. These two colors form the backbone of the entire visual identity. A bright yellow serves as the primary accent / CTA color.

| Name | Token | Hex | Role |
|---|---|---|---|
| **EC Red** | `--ec-red` | `#DC3529` | Primary brand. Header / navbar, primary navigation labels, brand mark background, section-title accents, key visual dividers, error states, focus ring base. |
| **EC Dark Navy** | `--ec-dark-navy` | `#0F1428` | Primary background for all dark-mode sections, hero area, ticket cards (dark contexts), most content containers. Creates a premium, immersive nighttime festival feel. Also the body text color on light surfaces. |
| **EC Yellow** | `--ec-yellow` | `#FFE600` | Critical action color. Used **exclusively** for primary CTAs. Bright neon-adjacent yellow, maximum contrast against the dark navy background. |

### Secondary Palette (Pass & Stage Identification)

| Name | Token | Hex | Usage |
|---|---|---|---|
| General Pass Red-Orange | `--ec-pass-general` | `#AA3C32` | General Access Pass left-border stripe (ticket cards only). |
| VIP Pass Pink / Magenta | `--ec-pass-vip` | `#D25096` | VIP Experience pass identification. |
| Youth Pass U25 Blue | `--ec-pass-youth` | `#4682C8` | Youth Pass U25 identification. |
| Camping Pass Green | `--ec-pass-camping` | `#28B950` | Camping Pass identification. |
| Ultra VIP Purple | `--ec-pass-ultravip` | `#823CC8` | Ultra VIP Pass identification. |

See [§10](#10-color-coded-pass-identification-system) for the scoped usage rule.

### Accent & Support Palette

| Name | Token | Hex | Usage |
|---|---|---|---|
| Light Gray | `--ec-gray-light` | `#EBEBEB` | Editorial / informational section backgrounds (light mode page bg). |
| Off-White | `--ec-off-white` | `#F5F5F5` | Card surface in dark mode (warmth against dark navy). |
| Mid Gray | `--ec-gray-mid` | `#787878` | Secondary text, meta info, divider lines. |
| White | `--ec-white` | `#FFFFFF` | Text on dark backgrounds, light card surfaces. |

### Semantic Palette (state colors)

| Name | Token | Hex | Use |
|---|---|---|---|
| Success | `--ec-success` | `#28B950` | Reuses Camping Pass green. Success toasts, valid-input border, success icons. |
| Warning | `--ec-warning` | `#F2A93B` | Warning toasts, soft cautions. (Not in primary palette; reserved for system messaging.) |
| Error | `--ec-error` | `#DC3529` | Reuses EC Red. Error toasts, validation errors, destructive confirmations. |
| Info | `--ec-info` | `#4682C8` | Reuses Youth Pass blue. Info toasts, neutral system messages. |

### Color Application Rules

1. Never use EC Yellow (`#FFE600`) for text over white. Insufficient contrast (2.0:1). Yellow is a fill color, almost never a text color.
2. Never use EC Red (`#DC3529`) as a full-page background. Reserved for header strip, accents, dividers, focus rings, error states, and the logo block.
3. Dark Navy (`#0F1428`) is the primary background for all immersive / hero sections.
4. Light backgrounds (`#EBEBEB`, `#F5F5F5`, `#FFFFFF`) are used for editorial / informational sections and for cards in dark mode.
5. Pass accent colors appear **only as left-border stripes on ticket pass cards** (see §10). They are NOT general-purpose accents.
6. EC Yellow CTA buttons ALWAYS have dark navy (`#0F1428`) text. Never white. Never red.
7. The EC Radio player at the bottom of the page uses a dark navy background with a yellow play button.
8. EC Red on light section backgrounds: only used for accent lines, error text (large enough to pass 3:1 ratio), and focus rings. **Body text in EC Red on white fails WCAG AA at normal sizes, so don't use it for paragraphs.**

---

## 02. Typography

### Primary Typeface (Headline / Display)

EC's primary headline and display typeface is a bold, condensed sans-serif that evokes industrial / punk festival energy.

**Current state:** the original audit PDF flagged Bebas Neue / Barlow Condensed / Black Han Sans as visually-similar candidates. The actual brand typeface has not been pinned. The Angular app uses Roboto (the existing `body` font) for all type until a brand font is loaded. Headline weight: 800 / Black. Loading a condensed Google Font (Barlow Condensed Black or Bebas Neue) is tracked as future work; the spec below assumes the eventual condensed typeface and the current Roboto fallback works in the interim.

**Characteristics (target):**
- Extremely heavy weight (Black / Ultra Bold, 800–900).
- Slightly condensed proportions, fits more text in tighter spaces.
- Used for: main hero text, section headings, ALL CAPS navigation labels, ticket names, stage headings, number displays.
- Mixed case usage: ALL CAPS for navigation and buttons. Title Case for section headings. Sentence case for editorial headings.

### Type scale (discrete tokens, no overlapping ranges)

| Token | Size | Line height | Weight | Usage |
|---|---|---|---|---|
| `--type-display` | 56 px | 1.05 | 800 | Hero headlines, campaign titles. |
| `--type-h1` | 32 px | 1.1 | 800 | Page-level headings. |
| `--type-h2` | 24 px | 1.2 | 700 | Section headings. |
| `--type-h3` | 18 px | 1.3 | 700 | Sub-section headings, card titles. |
| `--type-h4` | 16 px | 1.4 | 600 | Tertiary sub-headings. |
| `--type-body-lg` | 16 px | 1.55 | 400 | Feature descriptions, intro paragraphs. |
| `--type-body` | 14 px | 1.55 | 400 | Standard body copy. |
| `--type-body-sm` | 13 px | 1.5 | 400 | Captions, secondary info, footnotes. |
| `--type-label` | 11 px | 1.3 | 700 | Form labels, badge text, tag text, navigation labels (ALL CAPS, letter-spacing 0.06em). |
| `--type-micro` | 10 px | 1.3 | 400 | Legal text, ANPC disclaimers (NEVER used for primary content). |

Mobile adjustments: bump body to 16px minimum, keep heading scale unchanged.

The pre-rewrite file used ranges (e.g. 20–28 px for H2) which permitted H2 to be smaller than H3. The discrete tokens above remove that ambiguity. Each token is one number; no ranges, no overlaps.

### Secondary Typeface (Handwriting / Script)

A key visual element of EC's identity is an authentic-looking handwriting / script font used for special slogans, section headlines, and the lineup display on the hero. It creates a raw, personal, underground aesthetic.

- Used for: hero lineup text (artist names in white handwriting style), specific section slogans.
- Examples: "THE VIBES SHOULDN'T END JUST BECAUSE THE CLOCK SAYS SO..." (EC Village). Band-name overlay on hero. "NATURE IS OUR DANCE FLOOR" (sustainability banner).
- Style: irregular, marker / chalk-like strokes. Evokes festival poster, hand-painted signage.
- Font: similar to Permanent Marker, Caveat Brush, or a custom brush script.
- Always white or light-colored when overlaid on dark backgrounds.
- Never used for body text or UI labels. Decorative / campaign use only.

### Tertiary Typeface (Editorial / Body)

Long-form content sections (About, Sustainability, Previous Editions, International page) may use a serif or modern humanist sans-serif for richer reading.

- Size: 16 px for body in editorial contexts (larger than UI body).
- Weight: Regular (400) for body, Bold (700) for emphasis.
- Line height: 1.65–1.8× for prose comfort.
- Font: possibly Georgia, Libre Baskerville, or similar editorial serif.
- Usage: ONLY in content-heavy editorial sections. NOT in navigation, CTAs, or UI elements.

### Patterns observed in context

| Element | Style | Example |
|---|---|---|
| Navigation labels | `--type-label` ALL CAPS, letter-spacing 0.06em | "TICKETS", "MENU", "BILETE" |
| Hero band names | Handwriting font, 36–72 px (scales to fill) | "THE CURE × TWENTY ONE PILOTS" |
| Page hero headline | `--type-display` mixed case or ALL CAPS | "FESTIVAL PASSES", "VIP EXPERIENCE" |
| Section heading | `--type-h2` Title Case | "Festival Passes", "Music Stages" |
| Card title | `--type-h3` Bold | "General Access Pass", "Main Stage" |
| Price display | 32 px Bold + 14 px "EUR" suffix | "250 EUR", "139 EUR" |
| Body paragraph | `--type-body` or `--type-body-lg` | About page, Sustainability descriptions |
| CTA button label | `--type-label` ALL CAPS, letter-spacing 0.1em | "ADD TO CART", "SIGN IN" |
| Badge / tag label | `--type-label` (resolves prior 9-11 vs 10-12 inconsistency) | "DETAILS", "LIMITED" |
| Stat number | 32 px Bold + unit | "274.000 attendees", "18.000 campers" |
| Date / meta | 11–13 px Regular | "16-19 July 2026", "+ booking fee 8%" |
| Footer link | 11 px Regular | "Privacy Policy", "Terms & Conditions" |
| Tab label | `--type-label` ALL CAPS | "FESTIVAL ACCESS", "DAY TICKETS" |

---

## 03. Logo & Brand Mark

### EC Logo Description

The Electric Castle wordmark consists of two elements:
1. The **EC monogram:** a bold, slightly italic (oblique) sans-serif letterform in white, placed inside a solid red (`#DC3529`) rectangle with `--radius-sm` (2 px) corners.
2. The full **ELECTRIC CASTLE** wordmark: used at larger scales (footer, print, splash screens).

### Logo Variants

| Variant | Spec | Use |
|---|---|---|
| **Positive (primary)** | EC Red box, white "EC" text, `--radius-sm` corners | All on-screen UI, headers, navbar. |
| **Reversed (on red)** | When the brand mark sits ON a red surface, the box becomes transparent and "EC" stays white | Headers that are already EC Red. |
| **Monochrome black** | Solid black box, white "EC" text | Print, fax, single-color contexts. |
| **Monochrome white** | Transparent box with white "EC" stroked outline | Photography overlays where the red would clash with image hues. |

The Angular app currently ships only the positive variant. Monochrome variants are documented for future asset delivery.

### Logo Usage Rules

- The EC monogram is always displayed in the top-left of the header / navbar.
- The full "ELECTRIC CASTLE" wordmark is used at larger scales (footer, print, splash screens, brand stamps below the monogram on auth pages).
- The logo container is always rectangular with `--radius-sm` (2 px) corners. No other radius.
- On light backgrounds: red rectangle with white "EC" text (positive variant).
- On dark / navy backgrounds: identical red rectangle with white "EC" text (positive variant). The dark surrounding makes the red feel more saturated; this is intentional.
- Minimum size: EC monogram should never be smaller than 24 × 18 px in any UI.
- Clearspace: minimum 1× height of EC block on all sides. No content closer than that.
- The "EC Radio" variant uses the EC monogram with "Radio" text appended. Treated as a sub-brand. Same color rules.

### Logo Don'ts

| Don't | Why |
|---|---|
| Stretch or skew the box | Distorts the brand. |
| Recolor the box (e.g. green for sustainability) | Brand mark identity is fixed. Use a CO-MARK if a partner co-brand is needed. |
| Place on a busy background without a solid red surface | Loses legibility. |
| Reduce below 24 × 18 px | "EC" becomes unreadable. |
| Add a drop shadow other than `--shadow-glow-red` | Off-brand. |
| Use 4px+ radius | Square-ish corners are the brand. Logo's 2px is a deliberate softening. |

### Brand Textures & Patterns

EC uses several recurring visual textures that extend the brand identity:

- **Hand-drawn / chalk artist names on the hero:** creates DIY festival poster aesthetic. White text over photographic background.
- **The "×" separator** between artist names (e.g., "THE CURE × TWENTY ONE PILOTS"). Always white, same weight as name text.
- **"AND MANY MORE"** appended to artist lists with a slightly more casual font style.
- **Green leaf icon** in the sustainability footer. Outlined, minimal, placed on green (`#28B950`) or dark backgrounds.
- **Horizontal rule dividers:** thin EC Red lines underline section titles. Approximately 0.5 px weight.
- **Dotted / dashed score separators** on ticket list rows (alternating `--ec-off-white` / white rows).

---

## 04. Button System

EC's button system is minimal but highly specific. Four canonical variants.

### Variants

| Variant | Visual | When to use |
|---|---|---|
| **Primary CTA** | Yellow (`--ec-yellow`) fill, dark navy (`--ec-dark-navy`) text, Bold ALL CAPS, `--radius-none` (square corners), letter-spacing 0.1em | "ADD TO CART", "SIGN IN", "GET TICKETS", "BUY NOW", "SUBSCRIBE" |
| **Secondary CTA** | Yellow outline (2px), dark navy text, transparent fill | "Details", "See More", "Watch Aftermovie" |
| **Tertiary pill** | Dark fill (`--ec-dark-navy`), white text, small tag-like, `--radius-sm` corners (2px) | "DETAILS" pills on ticket list rows |
| **Outlined-on-dark** | White border (2px), white text on dark background | "SEE HOW EASY IT IS TO GET TO ELECTRIC CASTLE" hero CTAs |

### Sizing

| Size | Height | Padding-x | Type | Use |
|---|---|---|---|---|
| `lg` | 56 px | `--space-6` (24) | `--type-label`, 14 px | Hero primary CTA |
| `md` (default) | 48 px | `--space-4` (16) | `--type-label`, 12 px | Form submit, in-card CTA |
| `sm` | 36 px | `--space-3` (12) | 11 px | Inline / dense CTA |
| `xs` (pill) | 24 px | `--space-2` (8) | 10 px | "DETAILS" tag, status chip |

### Button state matrix

| State | Primary | Secondary | Tertiary | Outlined-on-dark |
|---|---|---|---|---|
| default | yellow fill, dark text | yellow border, dark text | dark fill, white text | white border, white text |
| `:hover` | yellow `filter: brightness(0.93)` | yellow border filled to `var(--ec-yellow)` at `--opacity-hover-overlay`, text unchanged | dark fill with subtle lift | white border filled at `--opacity-hover-overlay`, transition 150ms |
| `:active` | yellow `filter: brightness(0.85)` | full yellow fill, dark text | dark fill slightly inset | white background, dark text |
| `:focus-visible` | yellow base + `box-shadow: 0 0 0 3px rgba(255, 230, 0, 0.5)` (yellow ring on yellow body) | EC Red focus ring (`--ec-red` at 35% alpha) | EC Red focus ring | white focus ring at 35% alpha |
| `:disabled` | `opacity: var(--opacity-disabled)` (0.45), no hover | same | same | same |
| `loading` | text replaced by spinner (white circle on dark text), button non-interactive | same | same | same |
| `error` | (not a button state; surface inline error message instead) | n/a | n/a | n/a |

All transitions use `--duration-base` (150ms) `--ease-out`.

### Button radius rule

All buttons use `--radius-none` (0). Pill variant uses `--radius-sm` (2px). NEVER any other radius. See §00 [Border-radius scale](#border-radius-scale-the-square-corner-rule).

### Cart count and inline counters

Cart count "(0)" in header uses minimal styling: parentheses, number, no box. Not a button.

---

## 05. Card Components

### Ticket / Pass Cards (Homepage grid)

A distinctive EC UI pattern. Vertical color-coded left border strip (`--space-1` to `--space-2` wide, i.e. 4–8 px) on an off-white card with dark text.

**Anatomy:**
- Background: `--ec-off-white` (`#F5F5F5`) for warmth against dark page background.
- **Border radius: `--radius-none` (0).** Square corners.
- Left border stripe: 5–6 px solid, pass-specific color (see §10).
- Pass name: `--type-h3` Bold, `--ec-dark-navy`, top-left of card content area.
- Price: 32 px Bold (between `--type-display` and `--type-h1`), `--ec-dark-navy`, bottom of card.
- "+ tax" suffix: 10 px Regular, same color, appended after price.
- Original price if discounted: struck-through, lighter weight, smaller size, above current price.
- "DETAILS" pill: tertiary button variant. `--radius-sm` (2 px).
- Card padding: `--space-3` (12 px) internal padding from stripe to content, `--space-2` (8 px) top / bottom.
- Card elevation: `--shadow-1`.

### Ticket List Row Cards (Tickets Page)

Horizontal full-width rows with more detail.

- Full-width row inside max-width content column.
- 5–6 px left color strip, matching pass type.
- Row height: 80 px minimum (touch-target compliant).
- Pass name: `--type-h3` Bold, left-aligned.
- "DETAILS" pill below the name.
- Price: 32 px Bold, "EUR" in 14 px lighter weight, right half of row.
- "+ booking fee 8%": below price, 10 px, gray, with info icon.
- "Limited Availability": right-aligned 10 px gray text, above CTA button.
- CTA: primary yellow button, right-aligned, label "ADD TO CART". `--radius-none`.
- Row hover: subtle background shift to `--ec-white` with `--opacity-hover-overlay` wash. Transition `--duration-base`.
- Row border-radius: `--radius-none` (0). Rows DO NOT round.

### Feature / Info Cards (EC Village, Music Stages)

Image-led cards with text below: bold title, optional description.
- **Border radius: `--radius-none` (0).**
- Music stage cards use ALL CAPS headings with the stage name in `--type-h2` bold.
- "What's good" scroll cards: minimal, single headline on white / off-white background, left red border accent.
- Previous Editions cards: year + attendee count + brief text. No fixed background color.

### Card state matrix

| State | Default | Hover | Active (pressed) | Selected | Disabled |
|---|---|---|---|---|---|
| Background | `--ec-off-white` | `--ec-off-white` + wash | inset shadow | wash + 2px EC Red left border | `opacity: var(--opacity-disabled)` |
| Cursor | default or pointer (if clickable) | pointer | pointer | pointer | not-allowed |
| Shadow | `--shadow-1` | `--shadow-2` (slight lift) | `--shadow-1` (resets) | `--shadow-2` | none |
| Transition | n/a | `--duration-base` `--ease-out` | `--duration-fast` | `--duration-base` | n/a |

---

## 06. Navigation Components

EC uses a minimal, two-level navigation system.

### Primary Header (Sticky)

- Full-width, solid EC Red (`--ec-red`) background, 56 px height.
- **Border-radius: 0** on all internal blocks.
- **Left:** EC logo monogram (positive variant).
- **Left-center:** date badge "16-19 JULY 2026". ALL CAPS, 12 px, white, with bullet separator.
- **Left-center:** "BUY TICKETS". ALL CAPS bold white. Acts as a secondary CTA in the header.
- **Right:** "LOGIN" | "CREATE ACCOUNT" text links, white, Regular.
- **Right:** cart icon with count "(0)". White icon, 24 × 24 px.
- **Right:** language selector "EN" with dropdown caret. White, 12 px.
- **Right:** "MENU" text + hamburger icon. White, bold 12 px.
- No horizontal rule below header. Red flows directly into content.

### Hamburger Menu (Full-Screen Overlay)

- Opens as a full-page or large panel overlay from the right. Dark navy (`--ec-dark-navy`) background.
- Animation: enter `--duration-slow` (250ms) `--ease-out`, slide-from-right + fade. Exit `--duration-base` (150ms) `--ease-in-out`, fade only.
- Four section columns: **EC12**, **THE FESTIVAL**, **EC WORLD**, **FESTIVAL ACCOUNT**.
- Section headers: `--type-label` ALL CAPS, white.
- Items: `--type-body` Regular, white. Listed vertically with `--space-2` gap.
- Close button: "×" top-right (same position as MENU button), 24 × 24 px hit target padded to 48 × 48 px.

### Ticket Page Tab Bar

- Full-width white or light gray bar below header.
- Four tabs: **FESTIVAL ACCESS** | **DAY TICKETS** | **CAMPING ACCESS** | **ACCOMMODATION**.
- Tab height: 48 px.
- Tab border-radius: 0.

### Active page indicator (new spec)

The audit PDF noted "No visible active indicator found." The brand spec going forward:

- **Active state:** 3 px EC Red (`--ec-red`) underline bar at the bottom of the active tab / nav item. Underline is full label width.
- **Hover (inactive):** 1 px EC Red underline at 50% opacity. Transitions in `--duration-base`.
- **Active label:** Bold weight (700+), other labels regular.
- This applies to:
  - Ticket page tab bar.
  - Main header navigation links (when present on desktop).
  - Mobile bottom nav: active tab uses EC Yellow icon fill + EC Yellow 3 px top border above the icon.

---

## 07. Layout System & Spacing

### Grid system

EC uses a fluid, content-driven layout with consistent margins.

- Desktop content max-width: 1280 px, centered.
- Page horizontal margins: 80 px (lg+), 40 px (md), 24 px (xs–sm).
- Two-column layouts: About page (text + visual), International page (directions + content).
- Four-column layouts: ticket pass cards (`gap: --space-5` 20 px), feature cards.
- Single-column: full-width hero, ticket list rows.
- Sections alternate between dark (`--ec-dark-navy`) and light (`--ec-gray-light` or `--ec-white`) backgrounds.

### Section rhythm & spacing (token-driven)

- Section background color changes mark section breaks (dark → light alternation).
- Vertical gap between sections: `--space-12` (48 px) mobile, `--space-16` (64 px) desktop.
- Section internal padding-y: `--space-10` (40 px) mobile, `--space-12` (48 px) desktop.
- Card grid gap: `--space-5` (20 px).
- List row height: 64–80 px for ticket rows.
- H1 margin-bottom before first paragraph: `--space-6` (24 px).
- Paragraph margin-bottom: `--space-4` (16 px).

### Key layout patterns by page

| Page | Layout pattern |
|---|---|
| Homepage | Full-bleed video hero → 4-col ticket cards → 3-col transport info → 2-col about → app + newsletter |
| Tickets page | Full-width tab bar → list rows (alternating color) → infinite scroll |
| About page | Text-first single column → stat blocks (3–4 per row) → logo scroll → values (2-col) |
| VIP Experience | Left sidebar nav → right content column (80% width) → feature tables |
| Music Stages | Anchor nav (8 stages) → each stage as full-width section |
| Sustainability | Tab navigation (2 tabs) → card grid (green-toned) → stat highlights → community section |
| EC Village | Hero video → text narrative → 4-col option cards → carousel → list → map |
| International | Hero text block → transport options → city guide → accommodation → news |
| Previous Editions | Vertical timeline (year + image + text). Single column. |
| Footer | 5-column link grid + newsletter form + social icons + legal + ANPC badges |

---

## 08. Iconography

### Icon style

EC uses a minimal, outline-style icon set. Light stroke weight that reads as confident without competing with photography or the chunky type.

| Property | Value |
|---|---|
| Style | Outline (stroked, not filled) |
| Stroke width | 1.5 px at 24 px size, 2 px at 32 px |
| Stroke join | Round (visual softness without rounded corners on UI surfaces) |
| Stroke cap | Round |
| Fill | None (outline only) by default; filled variant for active states |
| Color | Inherits text color (`currentColor` in SVG). Default white on dark, dark navy on light. |

### Icon sizes

| Size | Use |
|---|---|
| 16 px | Inline badges, small status indicators. |
| 20 px | Form input adornments (eye toggle, calendar). |
| 24 px | Default UI icon (header cart, menu items, nav buttons). |
| 32 px | Feature icons (transport options, sustainability bins). |
| 48 px | Empty-state hero illustrations, error page icons. |

### Icon set inventory (current)

The app currently ships:
- **Transport:** bus, train, car, plane.
- **Sustainability:** leaf, recycling arrows, water drop.
- **System:** menu (hamburger), close (×), chevron-down, chevron-right, eye (password toggle), info, warning, error, check, search.
- **Social:** instagram, facebook, x/twitter, tiktok, youtube (white in circular containers, see §09).
- **Player:** play, pause, volume, skip.
- **PrimeNG icons:** the project uses `primeicons` for default form-control adornments. These should be styled with the same EC color rules.

### Icon usage rules

- Icons sit inside a hit-target padding of at least 44 × 44 px (web) or 48 × 48 px (touch). The icon itself is centered.
- Inactive icons inherit `--ec-gray-mid`. Active icons use `--ec-yellow` on dark or `--ec-red` on light.
- Never combine an icon with text without `--space-2` (8 px) gap minimum.
- App store badges (Apple, Google Play) ship as standard pre-built images, never recolored.
- ANPC regulatory badges are Romanian regulatory icons, used as-is.

### Icon naming

When adding SVG icons to the Angular project:
- File name: `kebab-case-name.svg` (e.g. `arrow-right.svg`).
- Internal width/height set to viewBox (typically `0 0 24 24`).
- Strip `fill` attributes; rely on `currentColor`.

---

## 09. Imagery Style & Photography

### Photography Style

Electric Castle's photography is key to its visual identity. Imagery is always energetic, authentic, and emotionally charged.

- **Aerial / wide shots** of the festival grounds. Establish scale, density, the castle as backdrop against the landscape.
- **Crowd / people shots:** authentic emotional moments (raised hands, dancing, laughter). Never stock-photo-perfect. Always real.
- **Night photography with stage lighting:** dramatic, colorful stage lights against dark crowds. High contrast, vibrant.
- **Castle at dusk / dawn:** architectural photography framing the 15th-century castle as a magical backdrop.
- **Artist performance shots:** typically wide, showing both performer and crowd reaction.
- **Camping / community moments:** friends, tents, morning light, intimate gatherings.
- **Close-up wristband / crowd detail shots:** texture and belonging.

### Photography Post-Processing Style

- High contrast, slightly saturated. Rich blacks, vivid stage colors, warm skin tones.
- Night shots: preserve colored stage lighting (cyan, magenta, warm amber). No over-correction.
- Natural light shots: warm, golden-hour color grading with slight film grain.
- Drone shots: high saturation, sharp clarity.
- No heavy filters. No vintage desaturation. No pure black-and-white in current brand.
- Slight vignette on atmospheric shots to draw focus toward center.

### Image cropping & aspect ratios

| Surface | Aspect | Notes |
|---|---|---|
| Hero (desktop) | 16:9 | Full-bleed, no max-width. |
| Hero (mobile) | 4:5 (portrait) | Reframe, don't just crop. |
| Card image (4-col) | 3:2 | Top of card, edge-to-edge. |
| Artist card | 1:1 (square) | Faces near top third. |
| OG/social card | 1200 × 630 | See §23. |

### Photo overlay & text-over-image

When text sits over photography:
- Apply a bottom-up dark-to-transparent gradient (`linear-gradient(to top, rgba(15, 20, 40, 0.7) 0%, transparent 60%)`).
- Text in white. Minimum size `--type-body-lg` (16 px) for readability.
- Never place EC Red type on photography; the photo color competes. Use white or yellow.

---

## 10. Color-Coded Pass Identification System

Each festival pass type has a unique color, used as a left-border stripe on ticket pass cards and list rows.

| Pass | Token | Hex | Personality |
|---|---|---|---|
| General Access Pass | `--ec-pass-general` | `#AA3C32` | Warm red-orange. Approachable, entry-level. |
| VIP Pass | `--ec-pass-vip` | `#D25096` | Pink / magenta. Premium but fun. Nightlife energy. |
| Youth Pass U25 | `--ec-pass-youth` | `#4682C8` | Blue. Young, fresh, accessible. |
| Camping Pass | `--ec-pass-camping` | `#28B950` | Green. Nature, outdoors, community. |
| Ultra VIP Pass | `--ec-pass-ultravip` | `#823CC8` | Purple. Elevated, exclusive, aspirational. |
| Black Ticket | `--ec-dark-navy` | `#0F1428` | Dark navy / black. Ultimate luxury. |
| Day Ticket | `--ec-day-ticket` | `#DC6432` | Orange-red. Single-day energy burst. |

### Usage rule (scoped)

Pass colors are used **only on ticket card left-border stripes and ticket list rows**. They are NOT general-purpose accents. They do NOT appear as button colors, link colors, full backgrounds, text colors, or section dividers. (This scope reconciles the prior contradiction between the audit's "only as left-stripes" rule and the stage atmospheric-color use case below.)

---

## 11. Stage Identity System

Each music stage at Electric Castle has a distinct visual identity and naming convention. Sponsors are integrated using "by [Sponsor]" phrasing.

| Stage Name | Sponsor | Visual / Vibe Identity |
|---|---|---|
| **MAIN STAGE** | by Coca-Cola | Flagship stage. Full-width, open field. Largest visual presence. |
| **HANGAR** | by Banca Transilvania | Industrial warehouse aesthetic. Mixed electronic / alternative. Dark, atmospheric. |
| **BOOHA** | by glo | Iconic "first thing you see" stage. Industrial look. House / techno. High energy. |
| **HIDEOUT** | by #UNLOCKWONDER | Intimate, natural, bohemian. Afro house, organic. Exclusivity-coded. |
| **THE BEACH** | (no sponsor) | Underground, avant-garde. Caribbean / global sounds. Niche, curated. |
| **BACKYARD STAGE** | by MobilaDalin | Garden / nature setting. Fairy lights. Warm, intimate, world music. |
| **PING PONG STAGE** | by Burn | Guilty pleasures, oldies. High camp energy. Fan-favorite throwback sets. |
| **CAMPING STAGE** | (no sponsor) | Morning / daytime. Acoustic. EC Village, sunrise energy. |

Stage identity drives photography selection and editorial copy. Stage colors (e.g. Hangar's dark teal / Booha's neon orange) are atmospheric branding in editorial photography only; they do NOT enter the UI token palette and do NOT appear as button or stripe colors. The pass-color system in §10 is unrelated to stage atmospheric color.

---

## 12. Accessibility & Minimum Standards

| Pair | Ratio | Verdict |
|---|---|---|
| EC Yellow (`#FFE600`) on Dark Navy (`#0F1428`) | 15.3 : 1 | **AAA pass** |
| White on Dark Navy | 17.9 : 1 | **AAA pass** |
| EC Red (`#DC3529`) on White | 4.8 : 1 | **AA pass** (3:1 large text, 4.5:1 normal text) |
| EC Dark Navy on Off-white (`#F5F5F5`) | ~14 : 1 | **AAA pass** |
| EC Red on Off-white | ~4.6 : 1 | AA for large text. **Do not use for body copy at 14 px or smaller.** |
| EC Yellow on White | 1.6 : 1 | **FAILS.** Never use yellow text on white. Yellow is a fill color. |

### Focus management

- All interactive elements have a visible focus indicator using the focus-ring spec from §00.
- `outline: none` MUST be paired with a `box-shadow` ring; never strip focus without a replacement.
- Skip-to-content link is required on every page (`<a href="#main" class="sr-only focus:not-sr-only">Skip to content</a>`).
- Focus ring color: EC Red base; switch to EC Yellow on red surfaces.

### Hit targets (reconciled)

- Web (mouse + keyboard primary): 44 × 44 px minimum interactive area.
- Web touch / tablet: 48 × 48 px minimum.
- Native mobile (iOS / Android): 48 × 48 px minimum.
- CTA buttons across all platforms: 48 px height minimum.

### Other a11y rules

- Minimum text size: `--type-micro` (10 px) for legal disclaimers only; `--type-body-sm` (13 px) absolute minimum for any user-facing content.
- Language toggle accessible via dropdown labeled "Select language".
- Form inputs: every input has an explicit `<label>` (not placeholder-as-label).
- Images: `alt` text required. Decorative images use `alt=""`.
- Reduce motion: when `prefers-reduced-motion: reduce` is set, halve all transition durations and skip non-essential animations (skeleton pulse, hero parallax).
- Color is never the only carrier of meaning (e.g. error state always has an icon AND a color AND text).

---

## 13. EC Radio Player (Persistent UI)

Persistent bottom-of-page element on all marketing-site pages.

- **Position:** fixed to bottom of viewport (above footer).
- **Border-radius: 0.** Sharp rectangular bar.
- **Background:** `--ec-dark-navy`.
- **Z-index:** `--z-sticky` (100). Sits above content, below modals and toasts.
- **Height:** 64 px desktop, 56 px mobile.
- **EC Radio logo:** wordmark with EC monogram on the left.
- **Play / pause button:** circular `--radius-pill` (the lone reserved exception). Yellow fill, dark navy icon. 40 × 40 px.
- **Volume slider:** custom range input, yellow track, circular thumb.
- **Now Playing:** "NOW PLAYING: [Artist · Track]" in `--type-body-sm` white text. (Use middle-dot `·` separator instead of em-dash per [text-copy §03a](text-copy-design-language.md#03a-project-overrides-anti-ai-tells).)
- Always shows current station playing.

---

## 14. Form UI Patterns

EC form elements maintain the brand's clean, minimal aesthetic. Square corners, generous touch targets, EC Red focus rings.

### Input anatomy

- Field type: text, email, password, tel, number, search.
- **Border-radius: `--radius-none` (0).** No exceptions.
- Border: 1 px solid at `--ec-auth-input-border` rgba.
- Background: `--ec-white` (light card) or `--ec-off-white` (dark mode card).
- Text: `--ec-dark-navy`, `--type-body` (14 px).
- Padding: `--space-3` (12 px) horizontal, 10 px vertical (height target 40–44 px).
- Placeholder: 14 px Regular gray (`--ec-gray-mid` at 70% opacity), lowercase as per §09 text-copy.
- Label: `--type-label` (11 px, 700, ALL CAPS, letter-spacing 0.06em). Always above the input, never inside.

### Input state matrix

| State | Border | Background | Text | Focus ring | Cursor |
|---|---|---|---|---|---|
| default | 1px gray | white | dark navy | none | text |
| `:hover` | 1px EC Red @ 30% | white | dark navy | none | text |
| `:focus` (any focus) | 1px EC Red | white | dark navy | ring on `:focus-visible` only | text |
| `:focus-visible` | 1px EC Red | white | dark navy | `box-shadow: 0 0 0 3px rgba(220, 53, 41, 0.18)` | text |
| error | 1px EC Red | white | dark navy | ring inherits, plus red error text under field | text |
| success | 1px green (`--ec-success`) | white | dark navy | green ring at 18% alpha | text |
| `:disabled` | 1px gray | gray-light at `--opacity-disabled` | dark navy at `--opacity-disabled` | none | not-allowed |
| readonly | 1px gray | gray-light at `--opacity-readonly` | dark navy | none | default |

The audit-era ambiguity ("Active / focused input: Border color changes to EC Red or EC Yellow") is resolved: **EC Red, always.** Yellow rings are reserved for buttons that themselves are yellow (so the ring contrasts).

### Other form elements

- **Submit button:** primary CTA. Yellow fill, dark text, full-width by default in card forms.
- **Error text:** below field, `--type-body-sm` (13 px), `--ec-red`, 500 weight. Always paired with an icon (warning glyph) for color-blind support.
- **Success text:** below field, `--type-body-sm`, `--ec-success`, 500 weight, check icon.
- **Dropdown:** standard `<select>` with EC styling. `--radius-none`. Same border + focus rules.
- **Range slider:** custom CSS, yellow filled track, dark navy thumb (circular `--radius-pill`).
- **Checkbox:** square, 18 × 18 px, 1 px gray border. Checked = filled with `--ec-red` and white check icon.
- **Radio:** square (yes, square, not circular), 18 × 18 px. Checked = `--ec-red` fill with white inner square. This is intentionally counter to platform default; the brand is square.
- **Toggle / switch:** rectangular, 36 × 20 px, dark navy off-state, yellow on-state. Thumb: 14 × 14 px square. `--radius-sm` (2 px) thumb only.

### Character count display

For fields with max-length:
- Show "X / Y" indicator below field, right-aligned, `--type-body-sm`, gray.
- When over 80% of max: color shifts to `--ec-warning`.
- When at max: shifts to `--ec-red`. Input prevents further typing.

---

## 15. Dark Mode (EC's Primary Aesthetic)

EC is a dark-first design system. The primary experience is dark navy-based.

- Default page background: `--ec-dark-navy` for hero and immersive sections.
- Editorial / information sections: `--ec-gray-light` or `--ec-white` backgrounds.
- The page alternates between dark (immersive / experiential) and light (informational) sections.
- Text on dark: white (`--ec-white`) or yellow (`--ec-yellow`).
- Text on light: dark navy (`--ec-dark-navy`). **Never black `#000000`.** Maintains warmth.
- There is no user-togglable light / dark mode in the audit-period brand. The Angular app exposes a dark-mode-switcher; the EC palette respects both modes (see auth-page tokens in [src/styles.css](src/styles.css)).
- Festival night/day duality mirrored in design: dark sections = nighttime magic. Light sections = daytime clarity.

### EC Red behavior across light/dark sections

| Surface | EC Red use | Notes |
|---|---|---|
| Dark navy section | Accent dividers, focus rings, error glyph backgrounds | Red pops well against navy. |
| Light gray / white section | Accent lines (3 px or thinner), error icons, focus rings | Red on white is brand-correct but cannot carry body copy (see §12). |
| Photography overlay | Avoid. Red competes with image hues. Use white or yellow instead. | n/a |
| Inside light card on dark page | Accent under titles, error states. | Red ring + 1 px stroke ok. |

Symmetry confirmed: both modes use the same EC Red token; only the surrounding canvas changes.

---

## 16. Sustainability Visual Identity

The sustainability section has a distinct visual sub-identity within the EC brand.

- Primary color for sustainability: `--ec-pass-camping` green (`#28B950`). Used for section labels, icon accents, leaf glyphs.
- The "NATURE IS OUR DANCE FLOOR" wordmark is in hand-drawn / graffiti style. Same brush script as hero artist names.
- Sustainability banner: olive / muted green-toned photography with overlay text.
- Stats blocks: green number emphasis + white descriptive text on dark backgrounds.
- Community section uses warm, community-focused photography (people, camping, nature).
- The "Your Bit" section uses a simple list format with green bullet dots. Minimal, action-oriented.

---

## 17. EC App UI Patterns

### App Overview

The Electric Castle app (iOS & Android) extends the web design language into a mobile-native context.

| App section | Content |
|---|---|
| Home / Dashboard | Event info, lineup preview, quick actions, countdown |
| Lineup / Artists | Browse, filter, favorite, set reminders |
| Daily Schedule | Day-by-day program with time slots per stage |
| Stages & Venues | Stage information, current / upcoming acts |
| EC Map | Festival map with stage locations, services, vendors |
| Tickets / My Orders | Ticket display, wristband credit management |
| RFID / Top-up | Wristband balance, top-up, refund |
| Vendors / Food | Food and drink vendor discovery |
| EC Radio | Integrated radio player with live stream |
| Aftermovie / Content | Personalized aftermovie, media |
| Notifications | Pre-set alerts, lineup updates, news |
| Settings / Account | Profile, preferences, language |

### Mobile-Specific Design Rules

- **Bottom navigation:** 5 tabs (Home, Lineup, Schedule, Map, Tickets). Height 56 px (`--space-14`).
- **Tab bar:** `--ec-dark-navy` background. Active tab: `--ec-yellow` icon fill + 3 px yellow top-border (matching §06 active-indicator).
- **App bar (top):** `--ec-red` background. Matches header color for brand consistency. Height 56 px.
- **List items:** full-width touch targets, 48 px minimum height. Stage color left borders for artist rows.
- **Cards:** same off-white background as web. `--radius-none`. Pass-color left border for ticket cards.
- **CTA buttons:** full-width in mobile contexts. Yellow fill, dark text, `--radius-none`. 48 px minimum height.
- **Typography:** body 14 px minimum (16 px for editorial). Captions 11 px minimum.
- **Icons:** 24 × 24 px minimum touch-safe size, outline style per §08.
- **Images:** full-bleed for hero. Aspect ratio preserved for artist / stage photos.
- **Notifications:** red badge dots for unread. Dot 8 × 8 px on app icon.

### App vs. web copy tone

The app should mirror the web's tone but optimize for mobile context. Full voice rules in [text-copy-design-language.md](text-copy-design-language.md).

- **Shorter headlines:** max 3–4 words for app section titles.
- **Action-first:** "Build your schedule" instead of "You can build your schedule here".
- **Push notifications:** brief, brand-voiced. See [text-copy §22](text-copy-design-language.md#22-push-notification-archetypes).
- **Empty states:** friendly, encouraging. See [text-copy §14](text-copy-design-language.md#14-copy-templates-by-app-screen-type).
- **Error states:** same "OOOPS!" brand voice, with clear recovery action.
- **Onboarding:** 3–5 screens max, one key benefit per screen, "Let's go" final CTA.

---

## 18. Modal & Dialog System

EC modals are decisive: confirm or cancel, no half-states. Square corners (of course).

### Anatomy

- **Backdrop:** `rgba(15, 20, 40, var(--opacity-scrim))` = dark navy at 60% opacity. Tap closes modal (unless destructive variant blocks dismiss).
- **Container:** centered, `--radius-none`, `--shadow-3`. Background: `--ec-white` on light, `--ec-off-white` on dark.
- **Z-index:** backdrop `--z-modal-backdrop` (300), content `--z-modal` (400).

### Sizes

| Size | Width | Use |
|---|---|---|
| `sm` | 360 px | Confirmation, simple choice. |
| `md` (default) | 520 px | Standard form / details. |
| `lg` | 720 px | Multi-section content. |
| `fullscreen` | 100% (mobile) | Mobile-only. Animation: slide-up from bottom. |

### Content structure

- **Header:** title in `--type-h3` Bold, dark navy. Close button (×) top-right, 48 × 48 px hit area.
- **Body:** body copy in `--type-body`. Padding `--space-6` (24 px) all sides.
- **Footer:** action row, right-aligned (LTR), `--space-3` (12 px) gap between buttons.
- **Action ordering:** primary CTA on right, secondary (cancel) on left. Mobile: stack vertically with primary on top.

### Animation

- Enter: `--duration-slow` (250 ms) `--ease-out`. Fade backdrop in + container fade + 8 px slide-up.
- Exit: `--duration-base` (150 ms) `--ease-in-out`. Fade only.
- `prefers-reduced-motion: reduce`: drop the slide; fade only.

### Destructive variant

For destructive confirmations (delete, cancel order):
- Primary CTA uses EC Red fill instead of EC Yellow.
- Button label is the specific action ("Delete pass", "Cancel order"), never "OK".
- Backdrop click does NOT dismiss; user must explicitly choose.
- Escape key still dismisses (cancels).

---

## 19. Toast & Snackbar System

Brief in-context feedback that doesn't block.

### Position

- Desktop: top-right of viewport, `--space-6` (24 px) from each edge.
- Mobile: bottom-center, `--space-4` (16 px) from edges, above any persistent bottom-nav.
- Z-index: `--z-toast` (500). Floats above modals so error confirmations are visible.

### Severities

| Severity | Color (accent stripe + icon) | Auto-dismiss | Use |
|---|---|---|---|
| info | `--ec-info` (blue) | 4 s | Neutral status: "Schedule updated." |
| success | `--ec-success` (green) | 4 s | Positive completion: "Ticket added to wallet." |
| warning | `--ec-warning` (amber) | 6 s | Soft caution: "Battery is low, save your QR." |
| error | `--ec-error` (red) | 8 s + manual close required | Failure: "Couldn't load lineup. Tap retry." |

### Anatomy

- **Container:** `--ec-off-white` background, `--radius-none`, `--shadow-4`. Width 360 px desktop, 92% viewport mobile.
- **Severity stripe:** 4 px left vertical bar in severity color. Mirrors ticket-card pattern.
- **Icon:** 20 × 20 px, severity color, left of text.
- **Title:** `--type-body` Bold, optional.
- **Body:** `--type-body-sm`, max 2 lines (truncate / wrap with ellipsis after that).
- **Action (optional):** text link, EC Red, right-aligned.
- **Close (×):** 24 × 24 px hit target, top-right.

### Stack behavior

- Max 3 toasts visible at once. Newer toasts push older off the top of the stack (FIFO).
- Hover pauses auto-dismiss timer.

### Animation

- Enter: slide in 8 px from off-screen, `--duration-slow` (250 ms) `--ease-out`. Fade.
- Exit: fade + 4 px slide out, `--duration-base` (150 ms).

---

## 20. Skeleton Loading & Spinners

When data is loading, prefer skeletons over spinners. Skeletons preserve layout intent.

### Skeleton pattern

- **Shape:** rectangular blocks, `--radius-none`, that match the dimensions of the content they replace.
- **Color:** `--ec-gray-light` at 60% opacity on light backgrounds; `--ec-gray-mid` at 20% opacity on dark.
- **Animation:** subtle pulse via `opacity 0.6 ↔ 0.9` over `1200ms ease-in-out infinite`. NOT a shimmer slide (too 2018).
- **Duration before showing:** 200 ms minimum delay before showing skeleton. If content loads under 200 ms, skip the skeleton entirely.

### When to use skeleton vs spinner

| Use | Skeleton | Spinner |
|---|---|---|
| List view loading | ✓ | no |
| Card grid loading | ✓ | no |
| Full-page transition | no (use brand splash) | no |
| Button submit loading | no | ✓ inline |
| Quick action with no layout (e.g. saving toggle) | no | ✓ inline 16 × 16 px |

### Spinner spec

- Circular, 16 px (inline) / 24 px (button) / 40 px (full-page action).
- Stroke: 2 px, EC Yellow on dark surfaces, EC Red on light. Rotates `--duration-slow` (rotation period 1.5 s linear).
- Yellow on EC Yellow buttons: use EC Dark Navy stroke instead, so the spinner is visible on the button surface.

---

## 21. Empty State Pattern

When a view has no content (no favorites, no notifications, no search results), don't show a blank screen.

### Anatomy

- **Container:** centered, max-width 360 px.
- **Illustration / icon:** 48–72 px (svg, outline style per §08), gray-mid color.
- **Headline:** `--type-h3`, dark navy. Title Case. ~3–5 words.
- **Body:** `--type-body-sm`, gray-mid, 1–2 lines max. Explains what's missing and how to fill it.
- **Primary action (optional):** primary CTA. Direct verb ("Browse the line-up", "Find friends").

### Variants

| Variant | Trigger | Visual cue |
|---|---|---|
| Empty (first use) | User hasn't started yet | Friendly invitation. Action CTA prominent. |
| Empty (filtered) | Filter returns 0 results | Gray icon. Show filter chips + "Clear filters" link. |
| Empty (after action) | User cleared their list | Slightly celebratory; offer next thing. |
| Offline | No network | Cloud-off icon. "Try again" button + offline-fallback content. |
| Error (recoverable) | API failure | Warning icon. Retry CTA. See §22 for full-page errors. |

Copy patterns in [text-copy §14](text-copy-design-language.md#14-copy-templates-by-app-screen-type).

---

## 22. Error Page Templates

Full-page error states (404, 500, maintenance, offline).

### Layout

- Dark navy full-bleed background.
- Vertical center.
- **Hero number / glyph:** 96 px display type (e.g. "404"), EC Yellow.
- **Headline:** `--type-h1`, white, ~5 words.
- **Body:** `--type-body-lg`, white, 1–2 sentences, EC voice.
- **Primary CTA:** yellow button, label like "Back to the castle".
- **Secondary link:** white text-link below CTA ("Or contact support").

### Templates

| Page | Hero glyph | Title | CTA |
|---|---|---|---|
| 404 | "404" big | "You wandered off." | "Back to the castle" |
| 500 | "500" big | "Something broke at our end." | "Try again" |
| Maintenance | wrench icon, 96 px | "Quick stage check." | "Come back in a few" (link to status page) |
| Offline | cloud-off icon, 96 px | "Lost signal." | "Try again" |

EC voice copy lives in [text-copy §08](text-copy-design-language.md#08-error-empty--system-states).

---

## 23. Brand Asset Specs

### Favicon

| Size | File | Use |
|---|---|---|
| 16 × 16 | `favicon-16.png` | Browser tab |
| 32 × 32 | `favicon-32.png` | Browser tab (high-DPI) |
| 48 × 48 | `favicon-48.png` | Bookmark, taskbar |
| 180 × 180 | `apple-touch-icon.png` | iOS home screen |
| 192 × 192 | `icon-192.png` | PWA manifest |
| 512 × 512 | `icon-512.png` | PWA manifest, splash |

Each favicon is the EC Red square with white "EC" italic. `--radius-sm` (2 px) at large sizes; flush square at 16/32 to maximize legibility.

### OG / Social Card Template

- Dimensions: 1200 × 630 px (recommended Open Graph spec).
- Background: `--ec-dark-navy`.
- 60 px EC Red bar across the top.
- EC monogram top-left, 80 × 80 px.
- Headline: white, condensed bold, up to 72 px, max 6 words.
- CTA box: optional yellow 240 × 60 px rectangle, dark text, bottom-right.
- Optional brand photo crop on the right 50% (rocky / castle / crowd).

Per-page templates (homepage hero, ticket promo, lineup reveal) override the headline and image but keep the structural elements.

### App icons

- iOS: 1024 × 1024 px master; system generates downsamples. Solid EC Red background with white "EC".
- Android: adaptive icon. Foreground "EC" centered, background EC Red, no padding required.

### Print

- Logo: minimum 12 mm wide.
- Pantone equivalents:
  - EC Red ≈ Pantone 1797 C
  - EC Yellow ≈ Pantone 102 C (verify with print partner; neon yellows vary)
  - EC Dark Navy ≈ Pantone 2767 C
- CMYK approximations live with the print partner.

---

## Notes on this document

- Source PDF: `electric-castle-visual-design-language.pdf` (Volume II, 16 pages, May 2026).
- Internal audit: ui-designer agent, 2026-05.
- Authoring rule: em-dashes (`—`) are avoided in prose per project override. See [text-copy §03a](text-copy-design-language.md#03a-project-overrides-anti-ai-tells). EN-dashes (`–`) for numeric ranges are fine.
- Update path: if EC publishes a new visual guide, replace this file wholesale and bump the audit reference at the top.
