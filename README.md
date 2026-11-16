# Insurance API

REST API for a simplified insurance domain: Customers → Policies → Claims. In-memory storage.

## Running

Requires .NET SDK 10.

```bash
dotnet run --project src/Insurance.Api
```

Swagger UI: `/swagger` on the URL printed at startup.

## Tests

```bash
dotnet test
```

Unit tests cover entities and services; integration tests run the API in-process via `WebApplicationFactory`.

## API

| Method | Path | Success | Errors |
|---|---|---|---|
| POST | `/customers` | 201 | 400 |
| GET | `/customers/{id}` | 200 | 400, 404 |
| POST | `/policies` | 201 (Draft) | 400 |
| POST | `/policies/{id}/activate` | 200 | 400, 404, 409 |
| GET | `/policies/{id}` | 200 | 400, 404 |
| POST | `/claims` | 201 (New) | 400, 409 |
| POST | `/claims/{id}/decide` | 200 | 400, 404, 409 |
| GET | `/claims/{id}` | 200 | 400, 404 |

Example requests:

```json
POST /policies
{ "customerId": "…", "productType": "Auto", "startDate": "2026-01-01", "endDate": "2026-12-31", "premium": 100.50 }

POST /claims/{id}/decide
{ "decision": "Reject", "decisionReason": "Not covered" }
```

All errors are `application/problem+json`. Business-rule and not-found errors carry a stable `code`:

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.10",
  "title": "Conflict",
  "status": 409,
  "detail": "Only a draft policy can be activated.",
  "code": "Policy.NotDraft",
  "traceId": "…"
}
```

Malformed JSON, wrong field types and missing non-text fields return a standard validation problem with an `errors`
object keyed by field. Text fields (`fullName`, `decisionReason`) are validated by the domain and return a `code`.

## Project structure

```text
insurance-api/
├── src/
│   └── Insurance.Api/
│       ├── Domain/            # Entities, enums, Result/DomainError types
│       ├── Application/       # Use-case services
│       ├── Infrastructure/    # In-memory repositories
│       └── Controllers/       # Endpoints, request DTOs, input validation
├── tests/
│   └── Insurance.Api.Tests/   # xUnit: Unit/ and Integration/
├── .editorconfig              # Code style and analyzer settings
├── Directory.Build.props      # Shared build settings (warnings as errors)
├── coverlet.runsettings       # Coverage settings
└── Insurance.slnx             # Solution file
```

## Design notes and tradeoffs

- **Single project, layered by folder.** Clean Architecture with one project per layer and CQRS were considered and
  rejected for this scope due to added complexity. Layering is kept as a folder convention (`Domain/` has no ASP.NET
  Core dependency); splitting into projects later is mechanical.
- **Result objects instead of exceptions** for expected failures. Each `DomainError` has a type (Validation, NotFound,
  Conflict) mapped to 400/404/409 ProblemDetails in one place.
- **Entities guard their invariants.** Factory methods return `Result<T>`; an invalid entity can't be constructed.
  Entities are immutable: `Activate` and `Decide` return new instances, so stored state can't be changed by accident.
  Controllers validate input shape first, so most bad requests never reach the domain.
- **`DateOnly` over `DateTimeOffset`.** Policy periods and incident dates are considered calendar dates; time-of-day and time zones
  would add ambiguity at period boundaries.
- **Concurrency.** Storage is `ConcurrentDictionary`, but check-then-act operations (activation overlap check, claim
  decision) run under a single global lock so concurrent requests can't both pass a check. Per-entity locks would
  allow more parallelism but aren't worth the complexity here.

## Assumptions

Interpretations of points the requirements leave open:

- Money is `decimal` in a single implicit currency (EUR). `Premium` and `AmountRequested` must be > 0 with at most
  2 decimal places.
- `EndDate` must be after `StartDate` (not equal): a single-day policy is invalid.
- Policy periods are inclusive on both ends. Policies ending and starting on the same day overlap.
- A request body referencing a non-existent customer or policy returns 400; 404 is used only for the resource in the route.
- Claims are allowed for any `Active` policy regardless of today's date; only `IncidentDate` is checked against the period.
- On Approve, a supplied `DecisionReason` is ignored. On Reject, it must be non-empty, not whitespace-only and at most
  1000 characters.
- `FullName` is required, not whitespace-only, max 250 characters. It and `DecisionReason` are stored trimmed.
- When a request breaks both a validation rule and a business rule, 400 is returned rather than 409.
- Enum values must match a member name exactly (case-sensitive); anything else, including numbers, returns 400.
- A malformed id in the route (not a GUID) returns 400; a well-formed id that doesn't exist returns 404.
- Create endpoints return `201 Created` with `Location`; `activate` and `decide` return `200 OK` with the updated resource.
