# FAQ Ingest & KNN Retrieval — Design Spec
Date: 2026-05-23
Branch: feature/agent

## Summary

Expose two anonymous HTTP endpoints on a new `FaqController` for:
1. Ingesting new Q&A pairs with category tags
2. KNN semantic search over ingested questions, with optional user-context category filtering

The service layer (`IIngestService`, `IVectorSearchService`) is already implemented. This spec covers only the HTTP surface, validation, and wiring.

---

## Endpoints

| Method | Route | Body | Response | Auth |
|---|---|---|---|---|
| `POST` | `/api/v1/faq` | `IngestQARequest` | `204 No Content` | Anonymous |
| `POST` | `/api/v1/faq/search` | `QuestionSearchFilter` | `200 IReadOnlyList<RetrievedQA>` | Anonymous |

---

## Request Shapes

### Ingest — `POST /api/v1/faq`

Uses the existing `IngestQARequest` record from `Core.Dtos.VectorSearch` directly as `[FromBody]`.

```json
{
  "questionText": "Where can I park my car?",
  "answers": [
    { "answerText": "Use lot B on the north side.", "categoryValues": null }
  ],
  "questionCategoryValues": { "Transport": ["Car"] }
}
```

- `questionCategoryValues` is optional; omit to make the question unfiltered (always returned).
- `answers[].categoryValues` is optional per-answer category tagging.
- Duplicate question text (same SHA-256 hash) is silently skipped — ingest is idempotent.

### Search — `POST /api/v1/faq/search`

Uses the existing `QuestionSearchFilter` record from `Core.Dtos.VectorSearch` directly as `[FromBody]`.

```json
{
  "queryText": "parking",
  "userContext": { "Transport": ["Car"] },
  "topK": 6
}
```

- `userContext` is optional. Omit or pass `null` to search all questions regardless of category.
- `topK` defaults to 6; valid range 1–50.
- Response is `IReadOnlyList<RetrievedQA>` — each item has `questionText`, `answers` (list of `{ text, score }`), and `questionScore`.

---

## Validation

Two new validators in `VectorDb/Validators/`, registered via assembly-scan in `VectorDbModule` (same pattern as `LiveFeedModule`).

### `IngestQARequestValidator`
- `QuestionText`: not empty, max 2000 chars
- `Answers`: non-empty list
- Each `Answer.AnswerText`: not empty, max 4000 chars

### `QuestionSearchFilterValidator`
- `QueryText`: not empty, max 2000 chars
- `TopK`: between 1 and 50 (inclusive)

Validation errors produce `400 validation-failed` via the existing `FluentValidationFilter`.

---

## Architecture — Files Changed

| File | Change |
|---|---|
| `Presentation/Controllers/FaqController.cs` | New — `[AllowAnonymous]` controller, two action methods |
| `VectorDb/Validators/IngestQARequestValidator.cs` | New — FluentValidation validator |
| `VectorDb/Validators/QuestionSearchFilterValidator.cs` | New — FluentValidation validator |
| `VectorDb/VectorDbModule.cs` | Update — add validator auto-scan registration |

No new DTOs, no new service methods, no migrations.

---

## Error Handling

| Case | Behaviour |
|---|---|
| Blank `questionText` / `queryText` | `400 validation-failed` |
| `answers` list empty on ingest | `400 validation-failed` |
| `topK` out of range | `400 validation-failed` |
| Duplicate question text on ingest | `204` — `IngestService` silently skips by hash |
| No matching results on search | `200` with empty list |
| Invalid enum value in `userContext` key | `400` from JSON model binding |

---

## Testing Notes

- Ingest: POST with valid body → 204; POST same body again → 204 (idempotent); POST with empty `questionText` → 400.
- Search: POST with `queryText` and no `userContext` → 200 list (all categories); POST with `userContext` → 200 list filtered; POST with empty `queryText` → 400.
