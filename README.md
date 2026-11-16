# Insurance API

REST API for a simplified insurance domain: Customers → Policies → Claims. .NET 10, ASP.NET Core controllers,
in-memory storage (data is lost on restart).

## Running

Requires the .NET 10 SDK.

```bash
dotnet run --project src/Insurance.Api
```

- Swagger UI: <http://localhost:5222/swagger>
- OpenAPI document: <http://localhost:5222/openapi/v1.json>

`src/Insurance.Api/Insurance.Api.http` walks through the main flow (create customer → create policy → activate → file
claim → decide) and runs from Visual Studio, Rider or the VS Code REST Client.

## Tests

```bash
dotnet test
```

With coverage (OpenCover report under `coverage/`):

```bash
dotnet test --collect:"XPlat Code Coverage" --settings coverlet.runsettings --results-directory ./coverage
```

- **Unit tests** (`tests/Insurance.Api.Tests/Unit/`, folders mirror `src/`) cover entity invariants, services,
  repositories and the error-to-status mapping. Every business rule is tested on both sides of its boundary
  (e.g. an incident on the first and last day of a policy, policies that touch on one day vs. are one day apart).
- **Integration tests** (`tests/Insurance.Api.Tests/Integration/`) run the API in-process with
  `WebApplicationFactory`, so no ports, Docker or database are needed. They check every endpoint's status codes
  (201/200/400/404/409), the ProblemDetails body and error code, and the JSON format of dates and enums.
- **Concurrency tests** start simultaneous activations and decisions together through a barrier and assert that
  exactly one succeeds. They fail when the write lock is removed.

## API

| Method | Path | Success | Errors |
|---|---|---|---|
| POST | `/customers` | 201 | 400 |
| GET | `/customers/{id}` | 200 | 400, 404 |
| POST | `/policies` | 201 (Draft) | 400 |
| POST | `/policies/{id}/activate` | 200 (Active) | 400, 404, 409 |
| GET | `/policies/{id}` | 200 | 400, 404 |
| POST | `/claims` | 201 (New) | 400, 409 |
| POST | `/claims/{id}/decide` | 200 (Approved/Rejected) | 400, 404, 409 |
| GET | `/claims/{id}` | 200 | 400, 404 |

Example requests:

```json
POST /policies
{ "customerId": "…", "productType": "Auto", "startDate": "2026-01-01", "endDate": "2026-12-31", "premium": 100.50 }

POST /claims/{id}/decide
{ "decision": "Reject", "decisionReason": "Not covered" }
```

Dates are `yyyy-MM-dd`. Enums are their exact, case-sensitive names (`Auto | Property | Travel`, `Approve | Reject`).

### Errors

All errors are `application/problem+json`:

- **400** – invalid input: malformed JSON, missing or wrongly typed fields, invalid values (e.g. `endDate` not after
  `startDate`), a referenced customer or policy that doesn't exist, a rejection without a reason.
- **404** – the resource in the URL doesn't exist.
- **409** – a business rule blocks the action: wrong status, overlapping active policy, claim on a non-active policy,
  incident outside the policy period.

Business-rule and not-found errors carry a stable `code`:

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
│       ├── Domain/            # Entities, enums, Result/DomainError types; no ASP.NET Core dependency
│       ├── Application/       # Use-case services, write lock, repository interfaces
│       ├── Infrastructure/    # In-memory repositories
│       └── Controllers/       # Endpoints, error mapping; request/response DTOs in Contracts/
├── tests/
│   └── Insurance.Api.Tests/
│       ├── Unit/              # Mirrors src/ folders
│       ├── Integration/       # In-process HTTP tests
│       └── TestSupport/       # Shared test helpers
├── .editorconfig              # Code style and analyzer settings
├── Directory.Build.props      # Shared build settings (warnings as errors)
├── coverlet.runsettings       # Coverage settings
└── Insurance.slnx             # Solution file
```

## Design notes and tradeoffs

- **Single project, layered by folder.** Clean Architecture with one project per layer and CQRS were considered and
  rejected for this scope due to added complexity. Layering is kept as a folder convention (`Domain/` has no ASP.NET
  Core dependency); splitting into projects later is mechanical.
- **Entities guard their invariants.** Factory methods return `Result<T>`; an invalid entity can't be constructed.
  Entities are immutable records: `Activate` and `Decide` return new instances, so stored state can't be changed by
  accident and repositories can hand out stored instances without copying.
- **Result objects instead of exceptions** for expected failures. Each `DomainError` has a type (Validation, NotFound,
  Conflict) that `ApiControllerBase` maps to 400/404/409 ProblemDetails in one place. Exceptions are reserved for
  bugs and surface as a 500 ProblemDetails without a stack trace.
- **Two validation levels.** Controllers reject malformed requests (shape, types, missing fields) before any service
  runs; entities enforce business invariants regardless of who calls them.
- **Stable error codes** (`Policy.NotDraft`, `Claim.IncidentOutsidePolicyPeriod`, …) let clients branch on the error
  without parsing messages.
- **`DateOnly` over `DateTimeOffset`.** Policy periods and incident dates are calendar dates; time-of-day and time
  zones would only add ambiguity at period boundaries. "Today" is not part of any rule, which keeps the rules
  deterministic and testable.
- **Concurrency.** Storage is `ConcurrentDictionary`, but check-then-act operations (the overlap check on
  activation, the status check on decision) run under a single global write lock, so two concurrent requests can't
  both pass the same check. Per-entity locks would allow more parallelism but aren't worth the complexity here.
- **Strict enums.** Only exact, case-sensitive member names are accepted. The built-in converter would also accept
  numbers (making member order part of the contract), other casings, and comma-separated names that it silently
  combines (`"Approve, Reject"` would become `Reject`), so a small custom converter replaces it.
- **Not included:** authentication, persistence, pagination and search endpoints, deactivating policies. They are
  outside the required scope.

## Assumptions

Interpretations of points the requirements leave open:

- Money is `decimal` in a single implicit currency (EUR). `Premium` and `AmountRequested` must be > 0 with at most
  2 decimal places.
- `EndDate` must be after `StartDate` (not equal): a single-day policy is invalid.
- Policy periods are inclusive on both ends. Policies ending and starting on the same day overlap.
- A request body referencing a non-existent customer or policy returns 400; 404 is used only for the resource in the
  URL.
- Claims are allowed for any `Active` policy regardless of today's date; only `IncidentDate` is checked against the
  period.
- On Approve, a supplied `DecisionReason` is ignored. On Reject, it must be non-empty, not whitespace-only and at most
  1000 characters.
- `FullName` is required, not whitespace-only, max 250 characters. It and `DecisionReason` are stored trimmed.
- When a request breaks both a validation rule and a business rule, 400 is returned rather than 409.
- Enum values must match a member name exactly (case-sensitive); anything else, including numbers, returns 400.
- A malformed id in the URL (not a GUID) returns 400; a well-formed id that doesn't exist returns 404.
- Create endpoints return `201 Created` with `Location`; `activate` and `decide` return `200 OK` with the updated
  resource.
