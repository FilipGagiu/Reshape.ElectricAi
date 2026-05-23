# Electric Castle: Text & Copy Design Language

> Comprehensive reference for EN + RO app copy. Based on full audit of `electriccastle.ro` and an internal UX-writing review.
>
> **This file is the source of truth for Electric Castle's brand voice and copy.** Every frontend copy decision (microcopy, CTAs, headlines, errors, notifications, EN+RO labels) MUST conform to the rules here. Re-read before any FE edit. See [CLAUDE.md](../CLAUDE.md) §6b.
>
> Companion file: [visual-design-language.md](visual-design-language.md) (colors, typography, components, layout, tokens).
>
> **Project overrides** (see [§03a](#03a-project-overrides-anti-ai-tells)) supersede source PDF guidance where they conflict. Two key overrides: **no em-dashes**, and **minimize "it's not X, it's Y" phrasing**.
>
> Sources:
> - `electric-castle-text-copy-design-language.pdf` (22 pages, Version 1.0, May 2025, AI-generated brand audit).
> - Internal designer audit (ux-designer agent, 2026-05).

---

## Table of Contents

01. [Brand Overview & Personality](#01-brand-overview--personality)
01b. [Voice Attribute Matrix](#01b-voice-attribute-matrix)
02. [Tone of Voice: 6 Core Principles](#02-tone-of-voice-6-core-principles)
03. [Writing Style Rules](#03-writing-style-rules)
03a. [Project Overrides: Anti-AI-Tells](#03a-project-overrides-anti-ai-tells)
03b. [Romanian Linguistic Notes](#03b-romanian-linguistic-notes)
03c. [Character-Count Budgets Per Surface](#03c-character-count-budgets-per-surface)
03d. [Accessibility of Language](#03d-accessibility-of-language)
04. [Core Vocabulary Dictionary (EN + RO)](#04-core-vocabulary-dictionary-en--ro)
05. [Navigation & Menu Labels](#05-navigation--menu-labels-en--ro)
06. [CTA Patterns & Action Copy](#06-cta-patterns--action-copy)
07. [Ticket & Commerce Copy](#07-ticket--commerce-copy)
08. [Error, Empty & System States](#08-error-empty--system-states)
09. [Form Labels & Microcopy](#09-form-labels--microcopy)
10. [Headlines, Taglines & Hero Copy](#10-headlines-taglines--hero-copy)
11. [VIP & Premium Copy Register](#11-vip--premium-copy-register)
12. [Do's and Don'ts](#12-dos-and-donts)
13. [Sustainability & Values Copy](#13-sustainability--values-copy)
14. [Copy Templates by App Screen Type](#14-copy-templates-by-app-screen-type)
15. [Community & Onboarding Copy](#15-community--onboarding-copy)
16. [Full EN / RO Glossary](#16-full-en--ro-glossary)
17. [News & Editorial Copy Style](#17-news--editorial-copy-style)
18. [Rules & Legal Copy Patterns](#18-rules--legal-copy-patterns)
19. [Toast & Snackbar Copy Library](#19-toast--snackbar-copy-library)
20. [Modal & Dialog Copy Patterns](#20-modal--dialog-copy-patterns)
21. [Permission Dialog Copy](#21-permission-dialog-copy)
22. [Push Notification Archetypes](#22-push-notification-archetypes)
23. [Email Subject & Transactional Copy](#23-email-subject--transactional-copy)
24. [GDPR & Consent Copy](#24-gdpr--consent-copy)
25. [Loading State Copy](#25-loading-state-copy)
26. [Pluralization Rules](#26-pluralization-rules)

---

## 01. Brand Overview & Personality

### Who is Electric Castle?

Electric Castle (EC) is Romania's premier international music and arts festival, held annually at Bánffy Castle in Transylvania. Founded in 2013, it has grown from a boutique gathering to one of Europe's most celebrated festivals, consistently earning international recognition for its blend of music, art installations, and immersive experiences.

EC is not merely a festival. It positions itself as a cultural movement, a temporary city, and a community of free-spirited individuals united by music, creativity, and shared values.

### Brand Personality Pillars

- **BOLD & DARING.** EC pushes creative boundaries. Copy should surprise, delight, and occasionally shock. Never bland. Never corporate.
- **WARM & INCLUSIVE.** Despite its scale, EC speaks like a friend. The tone is personal, never hierarchical. "You" is more common than "the audience".
- **AUTHENTIC & HONEST.** EC copy never overpromises. It speaks plainly about what the experience is, warts and all (including honest disclaimers about camping).
- **IRREVERENT & PLAYFUL.** Puns, wordplay, and humor are central to the brand. Rules pages still have personality.
- **ROMANIAN PRIDE.** EC celebrates its Romanian roots while welcoming the world. Local references (Transylvania, Bánffy Castle, Clujeni) ground the brand in place.

### Brand Promise (EN + RO)

**Homepage Hero**
- EN: *The greatest party on Earth. Come as you are.*
- RO: *Cel mai tare festival din România. Vino așa cum ești.*

**About Page Core (canonical tagline; grandfathered exception to the §03a "it's not X, it's Y" rule)**
- EN: *Electric Castle is a state of mind, not just a festival.*
- RO: *Electric Castle este o stare de spirit, nu doar un festival.*

### Audience Persona

- **Primary:** 18–35 year olds, music-passionate, internationally minded, value experiences over possessions.
- **Secondary:** families (specific Family Pass exists), international tourists (dedicated `/international` section), corporate guests (VIP Experience).

The copy always respects audience intelligence. EC never condescends. It assumes the reader knows what a good festival looks like and is here because EC is better.

---

## 01b. Voice Attribute Matrix

The five pillars (BOLD, WARM, AUTHENTIC, IRREVERENT, ROMANIAN PRIDE) are not always equally dialed. Different surfaces require different mixes. The matrix below is the default; deviation needs justification.

Scale: ▰▰▰▰▱ = high, ▰▰▱▱▱ = medium, ▱▱▱▱▱ = off / muted.

| Context | BOLD | WARM | AUTHENTIC | IRREVERENT | RO PRIDE |
|---|---|---|---|---|---|
| Hero / campaign | ▰▰▰▰▰ | ▰▰▱▱▱ | ▰▰▰▱▱ | ▰▰▰▱▱ | ▰▰▰▱▱ |
| Onboarding | ▰▰▰▱▱ | ▰▰▰▰▱ | ▰▰▰▱▱ | ▰▰▱▱▱ | ▰▰▱▱▱ |
| Commercial / ticket purchase | ▰▰▰▱▱ | ▰▰▰▱▱ | ▰▰▰▰▰ | ▰▱▱▱▱ | ▰▰▱▱▱ |
| Success confirmation | ▰▰▰▱▱ | ▰▰▰▰▱ | ▰▰▰▱▱ | ▰▰▱▱▱ | ▰▰▱▱▱ |
| Recoverable error | ▰▱▱▱▱ | ▰▰▰▰▰ | ▰▰▰▰▱ | ▰▰▱▱▱ | ▰▱▱▱▱ |
| Hard error (payment failed) | ▰▱▱▱▱ | ▰▰▰▰▰ | ▰▰▰▰▰ | ▱▱▱▱▱ | ▱▱▱▱▱ |
| Empty state | ▰▰▱▱▱ | ▰▰▰▱▱ | ▰▰▰▱▱ | ▰▰▰▱▱ | ▰▱▱▱▱ |
| Push notification (lineup drop) | ▰▰▰▰▰ | ▰▰▱▱▱ | ▰▰▱▱▱ | ▰▰▰▱▱ | ▰▰▰▱▱ |
| Push notification (sustainability nudge) | ▰▰▱▱▱ | ▰▰▰▰▱ | ▰▰▰▰▱ | ▰▰▱▱▱ | ▰▱▱▱▱ |
| Transactional email | ▰▱▱▱▱ | ▰▰▰▰▱ | ▰▰▰▰▰ | ▰▱▱▱▱ | ▰▰▱▱▱ |
| Legal / Terms / Privacy | ▱▱▱▱▱ | ▰▰▱▱▱ | ▰▰▰▰▰ | ▱▱▱▱▱ | ▰▱▱▱▱ |
| Community / loyalty (Casteller) | ▰▰▰▱▱ | ▰▰▰▰▰ | ▰▰▰▰▱ | ▰▰▰▱▱ | ▰▰▰▰▰ |
| Sustainability call-to-action | ▰▰▰▱▱ | ▰▰▰▱▱ | ▰▰▰▰▰ | ▰▰▱▱▱ | ▰▰▰▱▱ |
| VIP / Premium upgrade | ▰▰▰▰▱ | ▰▰▰▱▱ | ▰▰▰▰▱ | ▰▰▱▱▱ | ▰▰▱▱▱ |

When in doubt, default to: WARM medium, AUTHENTIC high, BOLD as needed for emphasis, IRREVERENT only when context is light.

---

## 02. Tone of Voice: 6 Core Principles

### 1. Conversational, not formal

EC speaks like a knowledgeable friend, not a press release. Short sentences. Contractions welcome. Direct address.

- EN: *Got questions? We've got answers.*
- RO: *Ai întrebări? Avem răspunsuri.*

### 2. Energetic, not shouty

Energy is conveyed through word choice and rhythm, not ALL CAPS or excessive exclamation marks. One "!" per page maximum.

- EN: *Four days of pure, unfiltered magic.*
- NOT: *FOUR AMAZING DAYS!!!*

### 3. Playful, not silly

Wit and wordplay are encouraged. Puns work when they're clever. Avoid slapstick or juvenile humor in primary copy.

- EN: *The greatest party on Earth.* Confident, playful, without self-parody.

### 4. Inclusive, not exclusive

EC is for everyone. Copy avoids jargon that excludes. Accessibility-forward language. Avoid gatekeeping references.

- EN: *Whether it's your first or fifth EC, welcome home.*
- RO: *Fie că e primul sau al cincilea EC, bun venit acasă.*

### 5. Informative, not overwhelming

Practical info is essential (camping rules, ticket types) but presented in digestible chunks. Use headers, short paragraphs, plain language.

- EN: *No glass bottles. No exceptions. Keep the magic safe.* (Rules page)

### 6. Bilingual by design

EN is primary for international reach. RO is primary for local community. Copy should feel native in both, not translated. Key emotional phrases maintain the same rhythm and feeling.

- EN: *Come as you are.* ↔ RO: *Vino așa cum ești.* Same heartbeat, different language.

### Reading-level target

- **EN:** Flesch-Kincaid Grade 7 or lower for marketing copy and microcopy. Grade 9 acceptable for editorial long-form (About, Sustainability essay-length content).
- **RO:** equivalent informal register. Avoid academic vocabulary and complex subordinate clauses. Use ordinary present-tense verbs.
- **Both:** sentence length median ≤ 15 words. Paragraph length ≤ 4 sentences.

---

## 03. Writing Style Rules

### Sentence & Paragraph Length

Headlines: 2–6 words preferred. Sub-headlines: 8–15 words. Body paragraphs: 2–4 sentences maximum. Use line breaks generously. White space is part of the design.

### Capitalisation

- **Product names:** Always capitalised. Electric Castle, EC Village, EC Radio, Creative Camp, EC Talks, EC App, EC One.
- **Pass names:** Capitalised. General Access Pass, VIP Experience, Youth Pass U25, Family Pass, Day Pass.
- **Brand-owned concepts:** Capitalised. The Castle, The Great Hall.

DO: *Book your General Access Pass* / *Descarcă EC App*
DON'T: *book your general access pass* / *descarcă ec app*

### Numbers & Dates

- Spell out 1–9 in body copy. Use numerals for 10+.
- Dates follow local format: "12 iulie 2025" (RO), "July 12, 2025" (EN).
- Festival years use ordinal: "13th edition", "ediția a 13-a".
- Time: 24-hour clock for RO ("21:30"), 12-hour for EN ("9:30 PM"). Both acceptable in EN if context is informal.
- Currency: "450 RON" (EN: space, three-letter code after), "450 RON" (RO: same). For EUR: "€250" or "250 EUR", consistent per surface. No mixed forms.

### Punctuation

- **Ellipsis (`...`)** for dramatic effect: *"The wait is almost over..."*
- **Em-dashes (`—`):** **BANNED.** See [§03a Project Overrides](#03a-project-overrides-anti-ai-tells). Use comma, period, parentheses, or colon instead. (This reverses earlier guidance; em-dashes signal AI authorship.)
- **EN-dashes (`–`)** are fine for numeric ranges: "10–20 RON", "16–19 July 2026".
- **Ampersand (`&`)** only in UI labels / navigation, never in body copy.
- **Exclamation marks:** maximum one per section, only for genuine celebration.

### List formatting

- **EN:** use the Oxford comma (serial comma) in lists of three or more. *"Music, art, and people."* Not *"Music, art and people."*
- **RO:** standard Romanian convention. Use "și" before the last item, no comma before "și": *"Muzică, artă și oameni."*
- For three-item lists with clear parallelism, drop conjunctions for rhythm: *"Music. Art. People."* (EN) / *"Muzică. Artă. Oameni."* (RO).

### Active vs. Passive Voice

Always prefer active voice. EC is the doer, and so is the user.

**Active (preferred)**
- EN: *Your adventure starts here.*
- RO: *Aventura ta începe aici.*

**Passive (avoid)**
- EN: *An adventure will be experienced by attendees.*

### Contractions & Informal Grammar

- **English:** contractions are standard ("you're", "we've", "it's").
- **Romanian:** informal register is used but grammatically correct. Avoid overly academic or formal constructions in either language.

---

## 03a. Project Overrides: Anti-AI-Tells

> These overrides apply across **all** EC copy (web, app, notifications, error states, onboarding) and supersede any conflicting guidance in earlier sections. They exist because the patterns below scream "AI authored" and undermine EC's authentic, human voice.

### Rule 1: No em-dashes (`—`)

Em-dashes are banned in all user-facing copy and internal docs that may be paraphrased into the UI. They are the single most common AI tell.

**Replace em-dashes with one of:**
- Comma (default for short asides): *"Four stages, one incredible weekend."*
- Period (default for longer asides or two punchy beats): *"More than a festival. A world of its own."*
- Parentheses (for genuine asides): *"Our VIP lounge (air-conditioned, with bar service) opens at 17:00."*
- Colon (for definitions or expansions): *"The Castle: home of the main stage and the Great Hall."*

**Forbidden examples (rewrite these on sight):**
- ❌ *"Four stages — one incredible weekend."* → ✅ *"Four stages, one incredible weekend."*
- ❌ *"Welcome back, Casteller — the castle missed you."* → ✅ *"Welcome back, Casteller. The castle missed you."*
- ❌ *"EN: Come as you are. — Same heartbeat, different language."* → ✅ *"EN: Come as you are. Same heartbeat, different language."*

**Scope:** any em-dash, regardless of spacing (`—`, ` — `, `—`). EN-dashes (`–`) for numeric ranges like "10–20" or "16–19 July" are allowed.

### Rule 2: Minimize "it's not X, it's Y" phrasing

Constructions of the form "it's not just X, it's Y" / "this isn't X, it's Y" / "X is not merely Y, it's Z" are AI tells. They imply false depth and signal machine-generated copy.

**Allowed only when the contrast is genuinely load-bearing.** Never as a default rhetorical move.

**Grandfathered exception:** *"Electric Castle is a state of mind, not just a festival."* This is a canonical brand tagline defined in the source PDF. It stays. New copy should avoid the pattern.

**Forbidden examples (rewrite these on sight):**
- ❌ *"This isn't just a music festival, it's a cultural movement."* → ✅ *"A cultural movement. A temporary city. A community."*
- ❌ *"It's not about the lineup, it's about the people."* → ✅ *"The lineup is great. The people are what you remember."*
- ❌ *"This isn't your average ticket, it's your passport to magic."* → ✅ *"Your passport to four days of magic."*

**Watch for these surface variations of the same pattern:**
- "Not just X but Y"
- "More than X, it's Y"
- "X is not Y, X is Z"

### Enforcement

- Every PR that touches EC copy must be checked for em-dashes and "it's not X, it's Y" constructs.
- Review-agent dispatches MUST include: *"Flag any em-dashes (`—`) and any 'it's not X, it's Y' constructs in changed copy."*
- UX / UI / frontend-developer agent prompts MUST include this rule (link to this section).

---

## 03b. Romanian Linguistic Notes

### Diacritics: comma-below, not cedilla

Modern Romanian uses **ș** (U+0219 LATIN SMALL LETTER S WITH COMMA BELOW) and **ț** (U+021B LATIN SMALL LETTER T WITH COMMA BELOW).

The legacy cedilla forms **ş** (U+015F) and **ţ** (U+0163) are **forbidden** in EC content. They are an encoding artifact from older fonts and silently break:

- Visual: depending on the rendering font, cedilla forms display as a hook, not the correct comma. Looks unprofessional.
- Search & accessibility: search engines and screen readers treat the two forms as different characters, fragmenting indexing and assistive tech behavior.
- Sortability: cedilla forms sort differently in some locales.

**Rule:** every RO string in en.json equivalents, ro.json, source code comments, design files, and all UI surfaces must use the comma-below forms exclusively. Grep verification:

```
grep -nE "(ş|ţ)" frontend/public/i18n/ro.json
```

Expected output: empty.

If you find legacy cedilla in any imported text (from PDFs, third-party APIs, partner content), replace before committing.

### Pluralization

Romanian pluralization has a quirky rule that catches non-native developers:

- 1: singular: `un artist` (an artist)
- 2–19: plural without "de": `doi artiști`, `trei artiști`, ..., `nouăsprezece artiști`
- 20+: plural with "de": `douăzeci de artiști`, `400 de artiști`, `o mie de artiști`

**Why:** Romanian uses "de" before plural nouns when the quantifier is a multi-digit number ending in 00 or a noun-like quantifier ("mie", "milion"). For 20–99 the rule is colloquially extended to all multi-digit cardinals.

When building dynamic strings with `{{count}}` placeholders, use a Transloco ICU plural rule. See [§26 Pluralization Rules](#26-pluralization-rules) for the ICU pattern.

### Grammatical gender in dynamic copy

Romanian adjectives agree with the noun's grammatical gender (masculine, feminine, neuter). When personalizing copy (e.g. "Welcome, you're ready") and the user's gender is unknown, EC defaults to the **/ă suffix pattern**:

- *Sunt Pregătit/ă* (I'm Ready, masculine/feminine form).
- *Bine ai venit, Casteller-ule/Casteller-o.*

This is an acceptable compromise. The alternative (using only masculine, the unmarked default in Romanian) excludes; the alternative (asking for gender) is invasive. The /ă pattern is the accepted RO convention for gender-inclusive UI copy.

When the user's gender IS known (e.g. from a "Salutation" field), drop the /ă and use the specific form.

### Currency, time, and date

- **Currency:** Romanian convention is "450 RON" (numeral, space, code). NOT "RON 450". For EUR: "250 EUR" or "€250"; use one consistently per surface. Group thousands with comma in EN (`5,000`) and with period in RO (`5.000`). Decimal separator is the opposite (`.` in EN, `,` in RO).
- **Time:** Romanian uses 24-hour clock. EN may use 12-hour or 24-hour. Default to **24-hour in RO** ("21:30") and **12-hour in EN** ("9:30 PM").
- **Timezone:** when displaying event times, label as "Romania time" / "ora României" or "EEST" / "EET". Never assume the user's locale.
- **Date format:** EN uses "July 12, 2025" or "12 July 2025". RO uses "12 iulie 2025" (lowercase month, no preposition).

---

## 03c. Character-Count Budgets Per Surface

| Surface | Max chars | Notes |
|---|---|---|
| iOS push notification title | 50 | Truncates at lock screen. |
| iOS push body | 110 | Expanded view shows ~178 but design for 110. |
| Android push title | 65 | Truncates depending on launcher. |
| Android push body | 110 | |
| In-app notification banner | 80 | Top-of-app banner format. |
| SMS | 160 | One message segment. Above this triggers multi-part. |
| Twitter / X post | 280 | |
| Instagram caption | 2200 hard limit, 125 reads-without-tap | First 125 must hook. |
| Facebook OG description | 200 recommended | |
| Email subject line | 50 desktop, 30 mobile | Hook in the first 30. |
| Email preview text (preheader) | 90 | Appears next to subject line. |
| Meta description (HTML) | 155 | Google truncates around 155. |
| OG title | 60 | |
| OG description | 110 | |
| Button label | 24 | Two words preferred. |
| Badge / tag label | 14 | One word ideal. |
| Form field placeholder | 35 | Brief example or hint. |
| Toast message body | 80 | Two lines maximum at default font size. |
| Form error message | 80 | Single line under input. |

**Rule:** if copy exceeds budget, rewrite to fit. Never let truncation be the design.

---

## 03d. Accessibility of Language

### Alt-text guidance

- **Decorative images:** `alt=""` (empty string). Screen readers skip them.
- **Functional images** (buttons, icons that convey action): `alt="action verb"`. E.g. `alt="Close menu"`.
- **Informational images** (photos that add meaning beyond captions): `alt="short description"`. E.g. `alt="Crowd raising hands at sunset on the Main Stage"`.
- **Logo:** `alt="Electric Castle"`. Not `alt="EC logo"`.
- Length: 125 characters maximum per WebAIM convention. Longer descriptions use a `longdesc` or visible figure caption.
- No "Image of...", "Photo of...". The user knows it's an image. Just describe the content.

### Screen-reader specific copy

- **Hidden labels:** when a button has only an icon (e.g. close ×), provide `aria-label="Close"` for screen readers. Visible icon stays.
- **Live regions:** dynamic content (toast, validation messages) uses `role="status"` (polite) for info / success, `role="alert"` (assertive) for errors.
- **Skip links:** "Skip to main content" / "Sari direct la conținut" must be the first focusable element on every page.

### Inclusive language defaults

- **Gender-neutral defaults:** EN uses "they" as the default singular pronoun when gender is unknown. Avoid "guys" / "ladies and gentlemen" / "his or her" constructions.
- **Disability:** person-first when context is clinical ("a person with a disability"), identity-first when community-preferred ("a disabled person"). When in doubt, ask. Avoid euphemisms like "differently abled".
- **Age:** "U25" pass is fine because it's a pass name. Otherwise avoid "the elderly", "kids" in legal contexts.
- **Cultural references:** test idioms against the bilingual rule. If an EN idiom doesn't carry to RO, find a different one rather than literal-translating.

---

## 04. Core Vocabulary Dictionary (EN + RO)

These are EC's owned terms. They must be used consistently across all app screens, notifications, and UI elements. Never paraphrase brand-owned concepts.

| English Term | Romanian Term | Usage Note |
|---|---|---|
| Electric Castle / EC | Electric Castle / EC | The festival brand. "EC" acceptable after first full mention. |
| EC Village | EC Village / Satul EC | The festival's own creative zone with food, art, activities. |
| General Access Pass | Bilet General Access | The standard festival ticket type. |
| VIP Experience | VIP Experience | Premium pass. "VIP" capitalised, never lowercase. |
| Youth Pass U25 | Youth Pass U25 / Bilet Tineret U25 | Discounted pass for under-25s. |
| Family Pass | Family Pass / Bilet Familie | Pass for families with children. |
| Day Pass | Day Pass / Bilet de Zi | Single-day entry pass. |
| Creative Camp | Creative Camp | Annual workshop & creative residency program. |
| EC Talks | EC Talks | TEDx-style talk series held at the festival. |
| EC Radio | EC Radio | The festival's own radio station, year-round. |
| EC App | EC App / Aplicația EC | The official mobile application. |
| The Great Hall | Sala Mare | The main / VIP indoor stage inside Bánffy Castle. |
| Bánffy Castle | Castelul Bánffy | The historic venue. Always spell with accent. |
| Campsite / Camping | Campsite / Camping | Where attendees sleep on-site. |
| Line-up | Line-up / Lineup | Artist programme. Hyphen optional but consistent. |
| Edition | Ediție | Festival year: "13th edition" / "ediția a 13-a". |
| Castellers | Castellers | EC's term for regular / returning festival-goers. |
| The Realm | Tărâmul | Poetic reference to the festival grounds. |

---

## 05. Navigation & Menu Labels (EN + RO)

Navigation labels must be concise (1–3 words), capitalised in Title Case, and consistent across web and app.

| English | Romanian |
|---|---|
| Home | Acasă |
| Line-Up | Line-Up |
| Tickets | Bilete |
| Camping | Camping |
| EC Village | EC Village |
| VIP Experience | VIP Experience |
| Sustainability | Sustenabilitate |
| Music Stages | Scenele de Muzică |
| Creative Camp | Creative Camp |
| EC Talks | EC Talks |
| EC Radio | EC Radio |
| EC App | EC App |
| International | Internațional |
| Goodies | Goodies |
| Careers | Cariere |
| Press | Presă |
| Volunteers | Voluntari |
| Ambassadors | Ambasadori |
| Partners | Parteneri |
| Contact | Contact |
| Previous Editions | Ediții Anterioare |
| About | Despre |
| Daily Schedule | Program Zilnic |
| Youth Pass U25 | Youth Pass U25 |
| Food Vendors | Vânzători Alimente |
| Fashion & Fair | Fashion & Fair |
| Rules & Regulations | Regulament |
| Sponsors | Sponsori |

---

## 06. CTA Patterns & Action Copy

Call-to-action copy must be action-oriented, specific, and create forward momentum. EC CTAs are never generic ("Click here"). They use verbs that carry the brand's energy.

### Primary CTA Patterns

| Action | EN CTA | RO CTA | Note |
|---|---|---|---|
| BUY / GET TICKETS | Get Your Pass | Ia-ți Biletul | Strong ownership language. "Your" makes it personal. |
| EXPLORE | Discover EC Village | Descoperă EC Village | Use "Discover" over "Explore" for new features. |
| REGISTER | Join the Adventure | Alătură-te Aventurii | Avoid "Sign Up". Too generic for EC brand. |
| LEARN MORE | Find Out More | Află Mai Multe | Preferred over "Read More" or "Learn More". |
| DOWNLOAD APP | Download the EC App | Descarcă EC App | Always include "the" in EN. Drop article in RO short form. |
| NAVIGATE | View Line-Up | Vezi Line-Up | Short, direct. Never "Browse". |
| CONTACT | Reach Out | Contactează-ne | More personal than "Contact Us". |
| BACK | Back to Top | Înapoi Sus | Navigation micro-copy. |
| CONFIRM | I'm Ready | Sunt Pregătit/ă | Used in booking flow confirmation states. |
| ERROR RETRY | Try Again | Încearcă din Nou | System state. Friendly, not clinical. |

### CTA Tone in Context

**Ticket Purchase Flow**
- EN: *Claim Your Spot at EC 2025*
- RO: *Rezervă-ți Locul la EC 2025*

**App Download Screen**
- EN: *Your festival, in your pocket. Download now.*
- RO: *Festivalul tău, în buzunar. Descarcă acum.*

**Newsletter Sign-Up**
- EN: *Stay in the loop. No spam, promise.*
- RO: *Rămâi conectat. Fără spam, promitem.*

---

## 07. Ticket & Commerce Copy

Ticket and purchase copy must balance enthusiasm with clarity. Buyers need to know exactly what they're getting while feeling the excitement of the decision.

### Pass Type Naming Convention

| EN | RO | Description |
|---|---|---|
| **General Access Pass** | Bilet General Access | Standard 4-day entry + camping. Base tier. |
| **General Access + Camping** | General Access + Camping | Explicit camping bundle. List inclusions. |
| **VIP Experience** | VIP Experience | Premium experience. Never "VIP Ticket". |
| **Youth Pass U25** | Bilet Tineret U25 | Under-25 discount. Requires age verification. |
| **Family Pass** | Bilet Familie | 1 adult + 1–3 minors. State age restrictions. |
| **Day Pass** | Bilet de Zi | Single-day access. State which day. |
| **EC One (Fan Club)** | EC One | Loyalty / fan club membership tier. |

### Price & Availability Copy

- **Prices:** always include currency (RON or EUR). "From 450 RON" for phase pricing.
- **Phases:** "Phase 1 / Faza 1", "Phase 2 / Faza 2". Communicate urgency: *"Phase 2, act fast"*.
- **Sold out:** "Fully Booked" / "Epuizat". Never "SOLD OUT" in ALL CAPS.
- **On sale:** "Available Now" / "Disponibil Acum". Never "BUY NOW" in isolation.

### Ticket Card CTA

- EN: *General Access Pass. From 450 RON. Get Your Pass.*
- RO: *Bilet General Access. De la 450 RON. Ia-ți Biletul.*

### Sold-Out State

- EN: *This pass is fully booked. Join the waitlist.*
- RO: *Acest bilet este epuizat. Intră pe lista de așteptare.*

---

## 08. Error, Empty & System States

Error messages must be honest, helpful, and on-brand. EC never uses sterile technical language in user-facing states. Every error is an opportunity to reinforce brand personality.

### Error Message Framework

1. **Acknowledge:** state what went wrong in plain language.
2. **Apologise briefly:** one short phrase. Don't grovel.
3. **Direct:** tell them exactly what to do next.
4. **Stay warm:** match the EC voice even in failure states.

### 404 Not Found
- EN: *Oops! This page wandered off into the forest. Let's get you back to the castle.*
- RO: *Oops! Această pagină s-a rătăcit în pădure. Hai să te întoarcem la castel.*

### Network Error (festival-aware)
- EN: *Signal's playing hide and seek. Check your connection and try again.*
- RO: *Semnalul joacă de-a v-ați ascunselea. Verifică conexiunea și încearcă din nou.*

### Payment Failed
- EN: *Something went wrong with your payment. Your card hasn't been charged. Try again.*
- RO: *Ceva n-a mers cu plata. Cardul nu a fost debitat. Încearcă din nou.*

### Session Expired
- EN: *Your session timed out for security. Sign in again to continue.*
- RO: *Sesiunea ta a expirat din motive de securitate. Autentifică-te din nou pentru a continua.*

### Empty Search
- EN: *No results for that search. Try different keywords or browse below.*
- RO: *Niciun rezultat pentru această căutare. Încearcă alte cuvinte cheie sau navighează mai jos.*

### Form Validation (drop "Please")
- EN: *Fill in the highlighted fields to continue.*
- RO: *Completează câmpurile evidențiate ca să continui.*

### Upload Error
- EN: *That file didn't upload. Make sure it's under 10MB and try again.*
- RO: *Fișierul nu s-a încărcat. Asigură-te că are mai puțin de 10MB și încearcă din nou.*

### Permission Denied (generic)
- EN: *We need permission for this. Open settings to enable it.*
- RO: *Avem nevoie de permisiune pentru asta. Deschide setările ca să o activezi.*

### Server / Generic Backend Error
- EN: *Something broke on our side. We're on it. Try again in a moment.*
- RO: *Ceva s-a stricat la noi. Lucrăm la asta. Mai încearcă în câteva momente.*

### Offline
- EN: *You're offline. Some things won't work until you reconnect.*
- RO: *Ești offline. Unele funcții nu vor merge până când te reconectezi.*

---

## 09. Form Labels & Microcopy

Form microcopy is the unsung hero of UX writing. EC's form labels are clear, minimal, and occasionally warm. Placeholder text provides guidance without being verbose.

### Standard Form Field Labels

| EN | RO | Placeholder / Hint |
|---|---|---|
| First Name | Prenume | "Your first name" / "Prenumele tău" |
| Last Name | Nume de familie | "Your surname" |
| Email Address | Adresă de e-mail | "your@email.com" |
| Phone Number | Număr de telefon | Format hint: "+40 7xx xxx xxx" |
| Date of Birth | Data nașterii | Format: DD/MM/YYYY |
| Password | Parolă | Hint: "Min. 8 characters, 1 number" |
| Confirm Password | Confirmă parola | "Repeat your password" |
| Country | Țară | Default: "Select your country" |
| Order Number | Numărul comenzii | "e.g. EC-2025-XXXXX" |
| Promo Code | Cod promoțional | "Enter code" / "Introdu codul" |
| Message | Mesaj | "Your message..." |
| Accept Terms | Accept Termenii și Condițiile | Checkbox label |
| Subscribe | Abonează-te la newsletter | Checkbox label for email opt-in |

### Help Text Patterns

- "Required field" → "Required" / "Obligatoriu" (shorter, cleaner in app context).
- "This field is invalid" → **never**. Use specific guidance instead: *"Enter a valid email address."*
- **Success state:** *"You're all set."* (EN) / *"Totul e gata."* (RO). See §19 for the toast variant.
- **Loading state:** *"Getting things ready..."* / *"Pregătim totul..."*. See §25 for the full library.

### Character count display

When a field has a max-length, surface the count under the field, right-aligned:

- Default (under 80% of max): *"42 of 280"* / *"42 din 280"*.
- Warning (80–99%): same format, color shifts to warning.
- Limit reached: same format, color shifts to error, input prevents further typing.

---

## 10. Headlines, Taglines & Hero Copy

EC headlines are punchy, poetic, and often use rhythm. They position the festival as a transformative experience rather than a product.

### Actual EC Headlines by Page

| Page | Headline |
|---|---|
| Homepage | *Electric Castle. Where music meets magic.* |
| About | *We are Electric Castle. We are a state of mind.* |
| Tickets | *Your adventure starts with the right pass.* |
| VIP Experience | *Because some moments deserve to be extraordinary.* |
| Sustainability | *We believe in a festival that loves the planet back.* |
| EC Village | *More than a festival. A world of its own.* |
| Camping | *Under the stars, among friends.* |
| Creative Camp | *Where creativity has no limits.* |
| EC Talks | *Ideas worth spreading, in the shadow of a castle.* |
| EC Radio | *The music never stops. Even when the festival does.* |
| International | *Transylvania is calling. Can you hear it?* |
| Volunteers | *Be part of the magic. Join our team.* |
| Ambassadors | *Share the love. Earn your place in the legend.* |
| Careers | *Help us build the greatest party on Earth.* |
| Sustainability (RO) | *Credem într-un festival care iubește planeta înapoi.* |
| About (RO) | *Suntem Electric Castle. Suntem o stare de spirit.* |
| 404 Page | *Oops! You've wandered off into the enchanted forest.* |

### Headline Structure Patterns

- **Pattern A, "X meets Y":** *Where music meets magic* / *Unde muzica întâlnește magia*.
- **Pattern B, Identity statement:** *We are [X]. We are [emotion].*
- **Pattern C, Invitation with poetry:** *Transylvania is calling. Can you hear it?*
- **Pattern D, Contrast / Escalation:** *More than a festival. A world of its own.*
- **Pattern E, Promise + reassurance:** *Your adventure starts here.*

### Countdown copy tiers

The home-screen countdown should escalate in voice as the festival approaches:

| Days out | EN | RO |
|---|---|---|
| 30+ | *[X] days to Electric Castle. The pull is real.* | *Mai sunt [X] zile până la Electric Castle. Te simte chemarea.* |
| 8–30 | *[X] days. Time to pack.* | *[X] zile. Timpul să-ți faci bagajul.* |
| 4–7 | *[X] days to the castle. Almost time.* | *[X] zile până la castel. Aproape.* |
| 2–3 | *[X] days to go. Are you ready?* | *Încă [X] zile. Ești gata?* |
| 1 | *Tomorrow. We'll see you at the gates.* | *Mâine. Ne vedem la porți.* |
| Day 0 | *Today. The castle is waking up.* | *Astăzi. Castelul se trezește.* |

---

## 11. VIP & Premium Copy Register

VIP Experience copy operates in a distinct register: elevated, exclusive, and detail-oriented. It names specific benefits and creates a sense of privilege without being snobbish.

### VIP Register Characteristics

- Use sensory language: "savour", "exclusive access", "bespoke", "curated".
- Name specific benefits: don't say "special privileges". Say "dedicated entrance lane".
- Contrast with standard: position VIP as the thoughtful upgrade, not a status symbol.
- Romanian VIP copy mirrors the tone but stays warm. Romanians respond to warmth over coldness in luxury contexts.

### VIP Copy Examples

**VIP Pass Card Headline**
- EN: *The VIP Experience. For those who want more.*
- RO: *VIP Experience. Pentru cei care vor mai mult.*

**Benefit Description**
- EN: *Dedicated entrance. Skip the queue, start the magic sooner.*
- RO: *Intrare dedicată. Sari peste coadă, începe magia mai repede.*

**Lounge Description**
- EN: *Your private retreat in the heart of the castle grounds.*
- RO: *Refugiul tău privat în inima domeniului castelului.*

**Upgrade CTA**
- EN: *Upgrade Your Experience*
- RO: *Îmbunătățește-ți Experiența*

### VIP Feature Copy Templates

**Dedicated Lounge**
- EN: *Relax in our air-conditioned VIP lounge with premium seating and bar service.*
- RO: *Relaxează-te în lounge-ul nostru VIP cu aer condiționat, locuri premium și serviciu de bar.*

**Priority Access**
- EN: *Skip the general entrance. Your dedicated VIP gate means less waiting, more castle.*
- RO: *Treci de intrarea generală. Poarta ta VIP dedicată înseamnă mai puțin timp de așteptare și mai mult castel.*

**Exclusive Viewing**
- EN: *Premium stage viewing areas reserved exclusively for VIP pass holders.*
- RO: *Zone de vizualizare premium a scenei, rezervate exclusiv deținătorilor de VIP.*

**Gourmet Food**
- EN: *Curated food vendors and upgraded dining options, steps from the main stage.*
- RO: *Furnizori de alimente selectați și opțiuni de masă îmbunătățite, la câțiva pași de scena principală.*

---

## 12. Do's and Don'ts

These guardrails protect the EC brand voice. Apply them to every piece of copy written for the app.

| DO | DON'T |
|---|---|
| Use specific, vivid language ("four stages", "400+ artists", "Bánffy Castle") | Don't use ALL CAPS for emphasis (max one word, sparingly) |
| Address users directly: "you", "your", "yours" | Don't use multiple exclamation marks (max one per section) |
| Use active voice and strong verbs | Don't say "click here" or "learn more" without context |
| Keep sentences short in CTAs and labels (2–5 words) | Don't use "user" or "customer" in user-facing copy |
| Include both EN and RO in all customer-facing contexts | Don't use jargon: "leverage", "synergy", "utilise" |
| Use brand-specific terms (EC Village, Castellers, The Realm) | Don't write passive: "tickets can be purchased" |
| Celebrate Romanian identity and Transylvanian setting | Don't truncate brand names: "EC app" (lowercase is wrong) |
| Use ellipsis (`...`) for deliberate dramatic pauses | Don't use generic placeholder copy without brand voice |
| Let personality show in error messages and empty states | Don't translate literally from EN to RO without review |
| Translate emotion, not just words, when localising to RO | Don't use "SOLD OUT". Use "Fully Booked" / "Epuizat" |
| Use commas, periods, parens, or colons for asides | **Don't use em-dashes (`—`).** AI tell. See [§03a Rule 1](#03a-project-overrides-anti-ai-tells) |
| Build contrast through two punchy sentences when needed | **Don't lean on "it's not X, it's Y" phrasing.** AI tell. See [§03a Rule 2](#03a-project-overrides-anti-ai-tells) |
| Use comma-below ș, ț in all Romanian copy | **Don't use cedilla ş, ţ.** Forbidden per [§03b](#03b-romanian-linguistic-notes). |
| Drop "Please" / "te rog" from form validation | Don't be corporate-deferential. EC is direct, not pleading. |

---

## 13. Sustainability & Values Copy

Sustainability is a core brand value, not a marketing afterthought. EC copy in this domain is sincere, specific (never vague "we care about the planet"), and action-oriented for the user.

### Sustainability Copy Principles

- **Be specific.** Name the actual initiatives (waste sorting, reusable cups, solar power).
- **Invite participation.** *"Join us in making EC greener"* rather than *"EC is green"*.
- **Avoid greenwashing language.** No "eco-friendly" without proof.
- **Use second-person:** *"You can help"* / *"Tu poți ajuta"*.
- **Celebrate progress.** Acknowledge where EC has room to grow.

### Core Sustainability Terms (EN + RO)

| EN | RO |
|---|---|
| Zero Waste Goal | Obiectiv Zero Deșeuri |
| Reusable Cup | Pahar Reutilizabil |
| Waste Sorting | Sortarea Deșeurilor |
| Recycling Station | Punct de Reciclare |
| Eco Pass | Eco Pass |
| Green Camping | Camping Verde |
| Carbon Footprint | Amprentă de Carbon |
| Solar Power | Energie Solară |
| Water Refill Station | Punct de Reumplere Apă |
| Leave No Trace | Lasă Natura Curată |

### Sustainability Copy Examples

**App Banner**
- EN: *EC is going greener. Here's how you can help.*
- RO: *EC devine mai verde. Iată cum poți ajuta.*

**Push Notification**
- EN: *Remember to use the reusable cup programme. Save the planet, save money.*
- RO: *Nu uita de programul paharelor reutilizabile. Salvează planeta, economisește bani.*

**Sorting Guide**
- EN: *4 bins. Which one is yours? Glass | Plastic | Paper | General Waste*
- RO: *4 coșuri. Care e al tău? Sticlă | Plastic | Hârtie | Gunoi General*

---

## 14. Copy Templates by App Screen Type

These templates provide the exact copy structure for each major screen type in the EC app. Use them as starting points, adapting specific details (event name, date, pass type) as needed.

### Onboarding Screen 1: Welcome
- EN Headline: *Welcome to Electric Castle.*
- EN Sub: *Your festival, your adventure. Let's get started.*
- EN CTA: *Let's Go* / *Skip*
- RO Titlu: *Bun venit la Electric Castle.*
- RO Sub: *Festivalul tău, aventura ta. Să începem.*
- RO CTA: *Hai!* / *Sari peste*

### Onboarding Screen 2: Features
- EN Headline: *Everything you need, in one place.*
- EN Sub: *Schedule. Maps. Tickets. EC Radio. All here.*
- EN CTA: *Next*
- RO Titlu: *Tot ce ai nevoie, într-un singur loc.*
- RO Sub: *Program. Hărți. Bilete. EC Radio. Totul aici.*
- RO CTA: *Următor*

### Home Screen: Before Festival
Uses the countdown tier copy from §10.

### Home Screen: During Festival
- EN Headline: *Electric Castle is LIVE!*
- EN Sub: *What's happening right now*
- EN Quick Actions: *Now Playing*, *My Schedule*, *Find Friends*
- RO Titlu: *Electric Castle e LIVE!*
- RO Sub: *Ce se întâmplă chiar acum*
- RO Acțiuni Rapide: *Se Cântă Acum*, *Programul Meu*, *Găsește Prieteni*

### Ticket Screen: Pass Overview
- EN Headline: *Your Pass*
- EN: Pass Name + Type + Days valid
- EN CTA: *Show QR* | *Add to Wallet*
- RO Titlu: *Biletul Tău*
- RO: Nume Bilet + Tip + Zile valabile
- RO CTA: *Arată QR* | *Adaugă la Portofel*

### Line-Up Screen
- EN Headline: *This Year's Line-Up*
- EN Filters: *All* | *Stage* | *Day* | *Genre*
- EN Empty state (filter): *No artists match. Try a different filter.*
- EN Empty state (favorites): *No favorites yet. Tap the heart on any artist to start your shortlist.*
- RO Titlu: *Line-Up-ul de Anul Acesta*
- RO Filtre: *Toți* | *Scenă* | *Zi* | *Gen*
- RO Stare goală (filtru): *Niciun artist nu se potrivește. Încearcă alt filtru.*
- RO Stare goală (favoriți): *Niciun favorit încă. Apasă inima la orice artist ca să începi lista.*

### Map Screen
- EN Headline: *Festival Map*
- EN Categories: *Stages*, *Food*, *Camping*, *Medical*, *Info*
- EN Search placeholder: *Find a place...*
- RO Titlu: *Harta Festivalului*
- RO Categorii: *Scene*, *Mâncare*, *Camping*, *Medical*, *Info*
- RO Placeholder căutare: *Caută un loc...*

### Profile / My EC Screen
- EN Headline: *My EC*
- EN Sections: *My Tickets*, *My Schedule*, *My Badges*, *Settings*
- EN Welcome: *Hey [Name], ready for the castle?*
- RO Titlu: *EC-ul Meu*
- RO Secțiuni: *Biletele Mele*, *Programul Meu*, *Insignele Mele*, *Setări*
- RO Bun venit: *Hei [Nume], pregătit/ă pentru castel?*

---

## 15. Community & Onboarding Copy

EC has a passionate community. Community-facing copy reinforces belonging, celebrates loyalty, and uses inclusive language that makes newcomers feel welcome and veterans feel special.

### Community Copy Principles

- **"Castellers"** is the term for loyal EC returnees. Use it to make long-timers feel seen.
- **First-timer language:** *"First time at EC? Here's what you need to know."*
- **Post-festival:** lean on the place, not the generic festival tropes.
- **Community milestones:** *"Your 3rd EC! You're officially a Casteller."*

### Community Copy Examples (EN + RO)

**Returning User Greeting**
- EN: *Welcome back, Casteller. The castle missed you.*
- RO: *Bun venit înapoi, Casteller. Castelul ți-a dus dorul.*

**New User Onboarding**
- EN: *First EC? You're in for a ride. Here's everything you need.*
- RO: *Primul EC? Ai parte de ceva special. Iată tot ce ai nevoie.*

**Post-Festival Message (place-specific punch-up)**
- EN: *The forest is quiet again. See you next year, Castellers.*
- RO: *Pădurea s-a liniștit din nou. Ne vedem anul viitor, Castelleri.*

**Badge Unlock (realm-metaphor punch-up)**
- EN: *3-Year Casteller unlocked. The castle keeps a room for you.*
- RO: *Casteller de 3 Ani deblocat. Castelul îți păstrează o cameră.*

**Referral CTA**
- EN: *Bring a friend to EC. The more the merrier.*
- RO: *Adu un prieten la EC. Cu cât mai mulți, cu atât mai bine.*

### Volunteer & Ambassador Copy

- **Volunteers:** *"Join our army of magic makers."* / *"Alătură-te armatei noastre de creatori de magie."*
- **Ambassadors:** *"Spread the EC word. Earn rewards. Be legendary."* / *"Răspândește vorba EC. Câștigă recompense. Fii legendar."*
- **Careers:** *"Help us build the greatest party on Earth."* / *"Ajută-ne să construim cel mai tare festival."*

---

## 16. Full EN / RO Glossary

Complete bilingual reference for all terms used across the EC app and communications. Listed alphabetically by English term.

| EN | RO |
|---|---|
| About | Despre |
| Access | Acces |
| Adventure | Aventură |
| Ambassadors | Ambasadori |
| App | Aplicație |
| Artists | Artiști |
| Back | Înapoi |
| Badge | Insignă |
| Bánffy Castle | Castelul Bánffy |
| Booking | Rezervare |
| Campsite | Loc de Camping |
| Cancel | Anulează |
| Careers | Cariere |
| Castle | Castel |
| Close | Închide |
| Community | Comunitate |
| Confirm | Confirmă |
| Contact | Contact |
| Continue | Continuă |
| Creative Camp | Creative Camp |
| Daily Schedule | Program Zilnic |
| Day Pass | Bilet de Zi |
| Discover | Descoperă |
| Download | Descarcă |
| EC App | Aplicația EC |
| EC Radio | EC Radio |
| EC Talks | EC Talks |
| EC Village | EC Village |
| Edition | Ediție |
| Email | E-mail |
| Error | Eroare |
| Explore | Explorează |
| Family Pass | Bilet Familie |
| Festival | Festival |
| Filter | Filtrează |
| Find | Găsește |
| Food | Mâncare |
| General Access Pass | Bilet General Access |
| Get Tickets | Ia-ți Biletul |
| Goodies | Goodies |
| Help | Ajutor |
| Home | Acasă |
| Info | Info |
| International | Internațional |
| Language | Limbă |
| Line-Up | Line-Up |
| Loading | Se Încarcă |
| Location | Locație |
| Log In | Autentificare |
| Log Out | Deconectare |
| Map | Hartă |
| Medical | Medical |
| Menu | Meniu |
| Music | Muzică |
| My Schedule | Programul Meu |
| My Tickets | Biletele Mele |
| Next | Următor |
| No results | Niciun rezultat |
| Notifications | Notificări |
| Order | Comandă |
| Parking | Parcare |
| Partners | Parteneri |
| Password | Parolă |
| Payment | Plată |
| Phase | Fază |
| Press | Presă |
| Previous | Anterior |
| Profile | Profil |
| QR Code | Cod QR |
| Radio | Radio |
| Register | Înregistrează-te |
| Rules | Regulament |
| Save | Salvează |
| Schedule | Program |
| Search | Caută |
| Security | Securitate |
| Settings | Setări |
| Share | Distribuie |
| Show QR | Arată QR |
| Skip | Sari peste |
| Sold Out | Epuizat |
| Sponsors | Sponsori |
| Stage | Scenă |
| Submit | Trimite |
| Sustainability | Sustenabilitate |
| Tickets | Bilete |
| Transport | Transport |
| Update | Actualizează |
| Venue | Locație |
| VIP Experience | VIP Experience |
| Volunteers | Voluntari |
| Welcome | Bun venit |
| Youth Pass U25 | Youth Pass U25 |

---

## 17. News & Editorial Copy Style

EC news and editorial content (announcements, lineup reveals, press releases) follows a distinctive style that blends journalistic structure with the brand's warm, energetic personality.

### News Headline Formulas

- **Artist Announcement:** *"[Artist] is coming to Electric Castle [Year]!"* / *"[Artist] vine la Electric Castle [An]!"*
- **Lineup Reveal:** *"The Electric Castle [Year] Lineup is Here!"* / *"Line-Up-ul Electric Castle [An] a sosit!"*
- **Ticket Sale Open:** *"Phase [X] Tickets Are Now Available"* / *"Biletele din Faza [X] sunt disponibile acum"*
- **Festival Recap:** *"Electric Castle [Year]: The Numbers, The Memories"* / *"Electric Castle [An]: Cifrele, Amintirile"*

### Article Structure (EC Blog Style)

- **Lead:** 1–2 sentences max. The most important info first, with a hook.
- **Body:** 3–5 short paragraphs. Each paragraph = one idea. Sub-headlines every 3 paragraphs.
- **Closing:** call to action or forward-looking statement. *"See you at the castle."*

### Lineup Announcement Lead

- EN: *The wait is over. Electric Castle 2025 is bringing its biggest lineup yet, with 400+ artists across 8 stages.*
- RO: *Așteptarea s-a terminat. Electric Castle 2025 vine cu cel mai mare lineup din istorie, cu 400+ artiști pe 8 scene.*

---

## 18. Rules & Legal Copy Patterns

Even in legal and rules contexts, EC maintains its voice. Rules are presented clearly and firmly, but the language never becomes cold or bureaucratic. The brand personality softens the hard edges of regulations.

### Rules Copy Principles

- **Lead with the reason, not just the rule:** *"No glass, to keep everyone safe."*
- **Use "we" to share ownership:** *"We ask that you..."* instead of *"It is prohibited to..."*.
- **Affirmative when possible:** *"Bring reusable containers"* instead of *"Don't bring disposable"*.
- **Reserve firm language for safety rules:** *"No exceptions"* is appropriate for glass, drugs, weapons.

### Rules Copy Examples

**No Glass Rule**
- EN: *Glass-free zone. Your safety is our priority. No glass bottles anywhere on site.*
- RO: *Zonă fără sticlă. Siguranța ta este prioritatea noastră. Nicio sticlă pe teren.*

**Age Verification**
- EN: *U25 Youth Pass requires proof of age. Have your ID ready at the gate.*
- RO: *Biletul Tineret U25 necesită dovada vârstei. Ai-ți actul de identitate pregătit la poartă.*

**No-Drone Policy**
- EN: *Drones are not permitted without prior written authorisation from EC. We want to keep the skies beautiful for everyone.*
- RO: *Dronele nu sunt permise fără autorizare scrisă prealabilă de la EC. Vrem să păstrăm cerul frumos pentru toți.*

### Terms & Conditions Microcopy

- EN: *By continuing, you agree to our Terms & Privacy Policy.*
- RO: *Continuând, ești de acord cu Termenii și Politica de Confidențialitate.*

- EN: *This ticket is non-transferable.*
- RO: *Acest bilet este netransferabil.*

- EN: *EC reserves the right to refuse entry.*
- RO: *EC își rezervă dreptul de a refuza accesul.*

---

## 19. Toast & Snackbar Copy Library

Toast / snackbar visual spec lives in [visual-design-language.md §19](visual-design-language.md#19-toast--snackbar-system). Copy patterns below match the four severity levels.

### Success

| Trigger | EN | RO |
|---|---|---|
| Ticket added to wallet | *Ticket added to your wallet.* | *Biletul a fost adăugat în portofel.* |
| Profile saved | *Profile saved.* | *Profilul a fost salvat.* |
| Friend added | *Friend added. They'll see you at EC.* | *Prieten adăugat. Vă vedeți la EC.* |
| QR copied to clipboard | *QR copied. Paste anywhere.* | *QR copiat. Lipește oriunde.* |
| Lineup synced to calendar | *Lineup added to your calendar.* | *Line-up-ul a fost adăugat în calendar.* |
| Schedule item favorited | *Saved to your schedule.* | *Salvat în programul tău.* |

### Info

| Trigger | EN | RO |
|---|---|---|
| Schedule updated by organizer | *Schedule updated. Take a look.* | *Programul s-a actualizat. Aruncă un ochi.* |
| New lineup added | *New artists added. See the lineup.* | *Artiști noi adăugați. Vezi line-up-ul.* |
| Connected (after offline) | *You're back online.* | *Ești înapoi online.* |

### Warning

| Trigger | EN | RO |
|---|---|---|
| Battery low (festival app) | *Battery low. Save your QR offline.* | *Baterie scăzută. Salvează QR-ul offline.* |
| Phase 2 ends in 24h | *Phase 2 ends in 24 hours.* | *Faza 2 se închide în 24 de ore.* |
| Limited stock | *Only a few of these left.* | *Au mai rămas doar câteva.* |

### Error

| Trigger | EN | RO |
|---|---|---|
| Network error | *Couldn't load. Tap to retry.* | *Nu s-a încărcat. Apasă să încerci din nou.* |
| Failed to save | *Couldn't save. Try again.* | *Nu s-a salvat. Încearcă din nou.* |
| Action denied | *That didn't work. Try again or contact us.* | *N-a mers. Încearcă din nou sau scrie-ne.* |

### Action link in toast

Some toasts ship with a one-action link (e.g. "Undo"):

| EN action | RO action |
|---|---|
| *Undo* | *Anulează* |
| *Retry* | *Încearcă* |
| *View* | *Vezi* |
| *Dismiss* | *Închide* |

---

## 20. Modal & Dialog Copy Patterns

Modal visual spec lives in [visual-design-language.md §18](visual-design-language.md#18-modal--dialog-system). Copy uses the structure: **headline + body + primary CTA + secondary CTA**.

### Neutral confirmation (e.g. continue without saving)

| Slot | EN | RO |
|---|---|---|
| Headline | *Leave without saving?* | *Pleci fără să salvezi?* |
| Body | *You'll lose the changes you made.* | *O să pierzi modificările pe care le-ai făcut.* |
| Primary CTA | *Stay* | *Rămâi* |
| Secondary CTA | *Leave* | *Pleacă* |

### Destructive confirmation (e.g. cancel order)

| Slot | EN | RO |
|---|---|---|
| Headline | *Cancel this order?* | *Anulezi această comandă?* |
| Body | *Your refund will be processed in 5–10 business days. This can't be undone.* | *Rambursarea va fi procesată în 5–10 zile lucrătoare. Această acțiune nu poate fi anulată.* |
| Primary CTA (red fill) | *Cancel order* | *Anulează comanda* |
| Secondary CTA | *Keep it* | *Renunț* |

### Destructive (delete profile)

| Slot | EN | RO |
|---|---|---|
| Headline | *Delete your account?* | *Îți ștergi contul?* |
| Body | *Your tickets, badges, and schedule will be gone forever.* | *Biletele, insignele și programul tău vor dispărea pentru totdeauna.* |
| Primary CTA (red fill) | *Delete account* | *Șterge contul* |
| Secondary CTA | *Keep my account* | *Păstrează-l* |

### Soft prompt (e.g. enable feature)

| Slot | EN | RO |
|---|---|---|
| Headline | *Add to your calendar?* | *Adaugi în calendar?* |
| Body | *Stay on top of every set. We'll never spam you.* | *Nu pierzi niciun moment. N-o să te plictisim.* |
| Primary CTA | *Add* | *Adaugă* |
| Secondary CTA | *Not now* | *Mai târziu* |

### Action label convention

- Primary CTA always names the action ("Delete account", "Cancel order"), never "OK", "Confirm", "Yes".
- Secondary CTA mirrors: "Keep my account", "Keep it", "Stay", "Not now". Never just "Cancel" alone.

---

## 21. Permission Dialog Copy

When the OS prompts for permissions (location, camera, notifications), EC controls the **pre-prompt** copy (in-app rationale shown before triggering the OS dialog).

### Location

| Slot | EN | RO |
|---|---|---|
| Title | *Show what's near you?* | *Îți arătăm ce e aproape?* |
| Body | *With your location, the map highlights stages, food, and friends close by.* | *Cu locația ta, harta îți arată scenele, mâncarea și prietenii din apropiere.* |
| Allow CTA | *Allow location* | *Permite locația* |
| Skip CTA | *Maybe later* | *Mai târziu* |

### Camera (for QR scanning)

| Slot | EN | RO |
|---|---|---|
| Title | *Scan your QR?* | *Scanezi codul QR?* |
| Body | *We use the camera to read tickets at the gate. Nothing is stored.* | *Folosim camera ca să citim biletele la poartă. Nu păstrăm nimic.* |
| Allow CTA | *Open camera* | *Deschide camera* |
| Skip CTA | *Not now* | *Nu acum* |

### Push notifications

| Slot | EN | RO |
|---|---|---|
| Title | *Get the heads-up?* | *Vrei un semn?* |
| Body | *Lineup drops, sale openings, day-of reminders. No spam, promise.* | *Anunțuri de line-up, deschideri de bilete, reminders din zi. Fără spam, promitem.* |
| Allow CTA | *Turn on notifications* | *Pornește notificările* |
| Skip CTA | *No thanks* | *Nu, mulțumesc* |

### Bluetooth (for RFID wristband pairing)

| Slot | EN | RO |
|---|---|---|
| Title | *Pair your wristband?* | *Conectezi brățara?* |
| Body | *Bluetooth lets us link your wristband balance to the app. Tap, top-up, done.* | *Bluetooth ne lasă să legăm soldul brățării de aplicație. Atingi, încarci, gata.* |
| Allow CTA | *Pair* | *Conectează* |
| Skip CTA | *Later* | *Mai târziu* |

---

## 22. Push Notification Archetypes

Push notification character budgets live in [§03c](#03c-character-count-budgets-per-surface).

### Countdown push (uses §10 tier copy)

| Days out | EN title | EN body | RO title | RO body |
|---|---|---|---|---|
| 7 | *7 days to EC* | *Time to pack. Have you got your QR ready?* | *7 zile până la EC* | *Timpul să-ți faci bagajul. Ai pregătit QR-ul?* |
| 3 | *3 days to go* | *Are you ready? Last chance for camping passes.* | *Mai sunt 3 zile* | *Ești gata? Ultima șansă pentru biletele de camping.* |
| 1 | *Tomorrow.* | *We'll see you at the gates from 12:00.* | *Mâine.* | *Te așteptăm la porți de la 12:00.* |
| 0 (morning) | *Today. Doors open at noon.* | *The castle is awake.* | *Astăzi. Porțile se deschid la prânz.* | *Castelul s-a trezit.* |

### Lineup drop

| EN title | EN body | RO title | RO body |
|---|---|---|---|
| *New names on the lineup.* | *Tap to see who just joined EC 2026.* | *Nume noi pe line-up.* | *Apasă să vezi cine vine la EC 2026.* |

### Day-of stage reminder

| EN title | EN body | RO title | RO body |
|---|---|---|---|
| *Chase & Status in 30.* | *Main Stage. Don't miss the opening.* | *Chase & Status în 30.* | *Main Stage. Nu pierde deschiderea.* |

### Sustainability nudge

| EN title | EN body | RO title | RO body |
|---|---|---|---|
| *Thirsty?* | *Refill stations are free. The closest one is 50m away.* | *Sete?* | *Punctele de reumplere sunt gratuite. Cel mai apropiat e la 50m.* |

### Badge unlock

| EN title | EN body | RO title | RO body |
|---|---|---|---|
| *Casteller status: 3 years.* | *The castle keeps a room for you.* | *Casteller: 3 ani.* | *Castelul îți păstrează o cameră.* |

### Post-festival recall

| EN title | EN body | RO title | RO body |
|---|---|---|---|
| *EC 2027 calling.* | *Early-bird passes drop next week.* | *EC 2027 te cheamă.* | *Biletele early-bird vin săptămâna viitoare.* |

---

## 23. Email Subject & Transactional Copy

### Subject conventions

- Lead with the most important info (action, status, or news).
- Use Title Case for marketing emails; Sentence case for transactional.
- Maximum 50 characters; hook in the first 30.
- One subject = one purpose. No "Order confirmed AND new lineup!".
- Avoid spam triggers: "FREE!!!", "URGENT", excessive exclamation.

### Transactional templates

**Booking confirmation**
- EN Subject: *Your EC pass is confirmed*
- EN Preheader: *See you 16–19 July. Save your QR.*
- RO Subject: *Biletul tău EC este confirmat*
- RO Preheader: *Ne vedem 16–19 iulie. Salvează QR-ul.*

**Password reset**
- EN Subject: *Reset your EC password*
- EN Preheader: *This link expires in 30 minutes.*
- RO Subject: *Resetează parola EC*
- RO Preheader: *Acest link expiră în 30 de minute.*

**Refund initiated**
- EN Subject: *Refund is on the way*
- EN Preheader: *Allow 5–10 business days.*
- RO Subject: *Rambursarea e pe drum*
- RO Preheader: *Așteaptă 5–10 zile lucrătoare.*

**Refund completed**
- EN Subject: *Refund completed*
- EN Preheader: *The amount is back on your card.*
- RO Subject: *Rambursare finalizată*
- RO Preheader: *Suma a fost restituită pe card.*

**Newsletter (lineup reveal)**
- EN Subject: *The EC 2026 lineup is here*
- EN Preheader: *400+ artists. 8 stages. 4 days.*
- RO Subject: *Line-up-ul EC 2026 a sosit*
- RO Preheader: *400+ artiști. 8 scene. 4 zile.*

**Newsletter (early bird)**
- EN Subject: *Early-bird passes drop tomorrow*
- EN Preheader: *Set an alarm. They go fast.*
- RO Subject: *Biletele early-bird vin mâine*
- RO Preheader: *Setează-ți o alarmă. Se duc repede.*

---

## 24. GDPR & Consent Copy

EC operates under GDPR. Consent copy is brand-voiced but legally precise.

### Cookie banner

- EN Title: *We use cookies.*
- EN Body: *Some keep the site working. Others help us understand what you love. You choose.*
- EN Buttons: *Accept all* | *Manage* | *Reject non-essential*
- RO Titlu: *Folosim cookie-uri.*
- RO Corp: *Unele țin site-ul în viață. Altele ne ajută să înțelegem ce-ți place. Tu alegi.*
- RO Butoane: *Acceptă tot* | *Gestionează* | *Refuză opționale*

### Consent withdrawal confirmation

- EN: *Consent withdrawn. We'll only use what we need to keep the site working.*
- RO: *Consimțământul a fost retras. Vom folosi doar ce e necesar ca site-ul să meargă.*

### Data deletion request acknowledgment

- EN: *Request received. Your account and data will be deleted within 30 days.*
- RO: *Cererea a fost primită. Contul și datele tale vor fi șterse în 30 de zile.*

### Newsletter unsubscribe

- EN Subject: *You're unsubscribed.*
- EN Body: *We get it. The door is open if you ever want back in.*
- RO Subject: *Te-ai dezabonat.*
- RO Corp: *Înțelegem. Ușa rămâne deschisă oricând vrei să te întorci.*

### Privacy policy link microcopy

- EN: *Read our [Privacy Policy](...).*
- RO: *Citește [Politica de Confidențialitate](...).*

---

## 25. Loading State Copy

Loading visual spec lives in [visual-design-language.md §20](visual-design-language.md#20-skeleton-loading--spinners). Copy below is for full-page or full-section loading where text accompanies a spinner / skeleton.

### Default (no specific context)

- EN: *Loading...*
- RO: *Se încarcă...*

### Context-specific

| Context | EN | RO |
|---|---|---|
| Fetching lineup | *Fetching the lineup...* | *Aducem line-up-ul...* |
| Loading map | *Loading the map...* | *Se încarcă harta...* |
| Generating QR | *Generating your QR...* | *Generăm QR-ul...* |
| Processing payment | *Processing payment. Don't refresh.* | *Procesăm plata. Nu reîmprospăta.* |
| Saving | *Saving...* | *Se salvează...* |
| Almost there | *Almost there...* | *Aproape gata...* |
| Searching | *Searching...* | *Căutăm...* |
| Connecting wristband | *Pairing your wristband...* | *Conectăm brățara...* |

### Long-running loader (over 5 seconds)

- EN: *This is taking longer than usual. Hang tight.*
- RO: *Asta durează mai mult ca de obicei. Mai rezistă puțin.*

---

## 26. Pluralization Rules

EC i18n uses Transloco with ICU MessageFormat for pluralization.

### English

EN has two plural categories: `one` and `other`.

```
{count, plural,
  one {1 artist}
  other {# artists}
}
```

| Count | Output |
|---|---|
| 1 | 1 artist |
| 2 | 2 artists |
| 100 | 100 artists |

### Romanian

RO has three plural categories: `one`, `few`, `many`.

- `one`: count = 1
- `few`: 2 ≤ count ≤ 19 (no "de" before the noun)
- `many`: count = 0 OR count ≥ 20 (uses "de" before the noun)

```
{count, plural,
  one {1 artist}
  few {# artiști}
  many {# de artiști}
}
```

| Count | Output |
|---|---|
| 0 | 0 de artiști |
| 1 | 1 artist |
| 2 | 2 artiști |
| 19 | 19 artiști |
| 20 | 20 de artiști |
| 100 | 100 de artiști |

### Pattern in i18n JSON

```json
{
  "lineup": {
    "count": "{count, plural, one {1 artist} other {# artists}}"
  }
}
```

For RO:

```json
{
  "lineup": {
    "count": "{count, plural, one {1 artist} few {# artiști} many {# de artiști}}"
  }
}
```

### Common pluralized strings

| Concept | EN one | EN other | RO one | RO few | RO many |
|---|---|---|---|---|---|
| Artist | 1 artist | # artists | 1 artist | # artiști | # de artiști |
| Day | 1 day | # days | 1 zi | # zile | # de zile |
| Ticket | 1 ticket | # tickets | 1 bilet | # bilete | # de bilete |
| Stage | 1 stage | # stages | 1 scenă | # scene | # de scene |
| Hour | 1 hour | # hours | 1 oră | # ore | # de ore |
| Minute | 1 minute | # minutes | 1 minut | # minute | # de minute |
| Friend | 1 friend | # friends | 1 prieten | # prieteni | # de prieteni |

### Mixed-language fallback

When showing counts in the user's language, never mix locales. If the user's lang is unknown and the count is critical, default to numerals + EN.

---

## Notes on this document

- Source PDF: `electric-castle-text-copy-design-language.pdf` (22 pages, Version 1.0, May 2025).
- Internal audit: ux-designer agent, 2026-05.
- Authoring rule: this file follows [§03a](#03a-project-overrides-anti-ai-tells). No em-dashes in prose. No "it's not X, it's Y" patterns (except the grandfathered brand tagline in §01).
- Romanian diacritics: comma-below forms only (ș, ț). Cedilla forms (ş, ţ) are forbidden per [§03b](#03b-romanian-linguistic-notes).
- Update path: if EC publishes a new copy guide, replace this file wholesale and bump the audit reference at the top.
