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

## Project structure

```text
insurance-api/
├── src/
│   └── Insurance.Api/
│       ├── Domain/            # Entities, enums, Result/Error types
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
- **Result objects instead of exceptions** for expected failures. Each `Error` has a type (Validation, NotFound,
  Conflict) mapped to 400/404/409 ProblemDetails in one place.
- **Entities guard their invariants.** Factory methods return `Result<T>`; an invalid entity can't be constructed.
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
- On Approve, a supplied `DecisionReason` is ignored. On Reject, it must be non-empty and not whitespace-only.
- `FullName` is required, not whitespace-only, max 250 characters.
- Enums are sent as strings; unknown values return 400.
- Create endpoints return `201 Created` with `Location`; `activate` and `decide` return `200 OK` with the updated resource.
