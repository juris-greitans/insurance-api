# AGENTS.md

This file provides guidance to AI agents when working with code in this repository.

This file is a living document - update it as needed to reflect the current state of the codebase.
Whenever there are substantial changes - for example, major framework upgrade, architectural shift,
new coding conventions - update this file to reflect those changes. `AGENTS.md` records what the
code can't tell you: business rules, entity relationships, decisions and rejected alternatives,
non-obvious gotchas.

## Commands

- Build: `dotnet build`
- Tests: `rm -rf coverage && dotnet test --collect:"XPlat Code Coverage" --settings coverlet.runsettings --results-directory ./coverage`
- Format check: `dotnet format --verify-no-changes`
- Static code analysis: see [Static code analysis and code quality](#static-code-analysis-and-code-quality)
- Run: `dotnet run --project src/Insurance.Api` (Swagger UI at `/swagger`)

Prerequisites: .NET SDK 10, Java 17+ runtime (required by `dotnet-sonarscanner`).

## Hard constraints

- .NET 10 Web API with Controllers. No Minimal APIs.
- No database, no EF Core, no SQL, no Docker.
- Swagger/OpenAPI enabled. The built-in `Microsoft.AspNetCore.OpenApi` generates the document only; a UI
  (Swashbuckle or Scalar) is needed in addition.
- Errors are returned as ProblemDetails.

## Architecture

Single project `src/Insurance.Api`, organised by folder. Layering is a convention, not enforced by project references
(deliberate simplification; rationale in `README.md`). No CQRS, no mediator library.

- `Domain/` – entities, enums, `Result`/`DomainError` types. No dependency on ASP.NET Core or other folders.
- `Application/` – services that orchestrate use cases; repository interfaces in `Application/Abstractions/`.
- `Infrastructure/` – in-memory repository implementations.
- `Controllers/` – HTTP endpoints, request DTOs, input validation, mapping of `DomainError` to ProblemDetails.

### Storage: in-memory only

- Repositories are registered as singletons and use `ConcurrentDictionary<Guid, T>`. They are synchronous; there is
  no I/O to await.
- Entities are immutable records, so repositories return stored instances directly. `Save` inserts or replaces by id.
- Lookups that may miss are named `FindById` and return `null` (`Get` trips CA1716).
- All data is lost on restart; that is expected.
- **Check-then-act races:** `ConcurrentDictionary` does not make a check plus an update atomic. Two concurrent
  activations can both pass the overlap check; two concurrent decisions can both see `New`. Every state-changing
  service operation runs load + check + save inside one `WriteLock.Execute` call (a singleton, single global lock).
  Reads don't take the lock. Deliberately simple; throughput is not a concern at this scope. The concurrency tests in
  `Unit/Application` fail when the lock is removed; keep it that way.

### Error handling

Failures are signalled with returned `Result`/`Result<T>` objects, not exceptions. Exceptions are reserved for bugs and unexpected run-time conditions.
Unhandled exceptions return a 500 ProblemDetails (`AddProblemDetails()` + `UseExceptionHandler()`), never a stack trace.

`DomainError` (named so to avoid CA1716; `Error` is a VB keyword) carries an `ErrorType` that maps to the HTTP status
in one place (controller base class or helper):

| Error type   | Status | Examples |
|--------------|--------|----------|
| `Validation` | 400    | invalid input, `EndDate <= StartDate`, Reject without reason, referenced customer/policy not found in a request body |
| `NotFound`   | 404    | resource in the route (`/policies/{id}`) does not exist |
| `Conflict`   | 409    | wrong status transition, overlapping active policy, claim on non-Active policy, incident outside policy period |

Not-found errors (route 404 and referenced-resource 400) live in `Application/ApplicationErrors`; rule errors live
in `<Entity>Errors` next to each entity.

Validation happens at two levels:

- Input validation (request shape, required fields, formats) in the controllers before services run → 400.
- Domain invariants enforced by the entities themselves, independently of API validation. Entities are created via
  factory methods returning `Result<T>`; constructors are private. An invariant violation that slips past API
  validation must still come out as 400 or 409, never 500.
- When a request fails both a validation rule and a business rule, the validation error (400) wins.
- Entities are immutable records; state changes (`Activate`, `Decide`) return a new instance. Error instances live in
  static `<Entity>Errors` classes next to each entity; tests assert on them by reference.

## Code conventions

- One top-level type per file, with the file named after the type.
- One test class per class under test, named `<Class>Tests` in its own file, in the folder under `Unit/` that mirrors
  the source folder.

## Domain and business rules

Customer → Policy → Claim (each references its parent by `Guid`).

- Entities enforce their invariants on creation and on every state change. An entity with a broken invariant must never
  exist; do not rely on callers to validate first.
- Money is `decimal`, never a floating-point type. Single implicit currency (EUR); no currency field.
  At most 2 decimal places → otherwise 400.
- Dates are `DateOnly` (calendar dates, no time zones), serialised as `yyyy-MM-dd`. Today's date is not part of any rule.
- Enums are serialised as strings; unknown values → 400.

### Customer

- `FullName` required, not whitespace-only, max 250 characters.

### Policy (`Draft → Active`)

- `EndDate > StartDate` (strict; a single-day policy is invalid) → otherwise 400.
- `Premium > 0` → otherwise 400.
- Created as `Draft`. Activation only from `Draft` → otherwise 409.
- Overlap: a customer may not have two `Active` policies of the same `ProductType` with overlapping periods. Periods are
  inclusive on both ends: overlap when `a.Start <= b.End && b.Start <= a.End`. Activating such a policy → 409.

### Claim (`New → Approved | Rejected`)

- `AmountRequested > 0` → otherwise 400.
- Created only for an `Active` policy → otherwise 409. Policy status counts, not today's date.
- `IncidentDate` within the policy period, inclusive → otherwise 409.
- Decision only from `New` → otherwise 409.
- Reject requires a non-whitespace `DecisionReason`, max 1000 characters → otherwise 400. On Approve, a supplied reason
  is ignored (not stored).

### HTTP responses

- Create endpoints return `201 Created` with a `Location` header.
- `activate` and `decide` return `200 OK` with the updated resource.
- Decide request body: `{ "decision": "Approve" | "Reject", "decisionReason": "..." }`.

## Testing

- xUnit, Moq. Test project: `tests/Insurance.Api.Tests`, with `Unit/` and `Integration/` folders.
- Unit tests: entities, services, validation.
- Integration tests: in-process via `WebApplicationFactory` (no Docker, no network). Cover every endpoint's status codes
  (201/200/400/404/409) and the ProblemDetails shape.
- Test naming: `MethodUnderTest_WhenCondition_ThenExpectedResult`.
- Every business rule needs tests for both sides of its boundary (e.g. incident on the first and last policy day,
  periods touching on one day).
- Coverage of new code must be 80% minimum (enforced by the SonarQube quality gate).

## Static code analysis and code quality

Requires the .NET tool `dotnet-sonarscanner`. If not available, ask the user to install it (`dotnet tool install --global dotnet-sonarscanner`).

```bash
source .env.agent-secrets
rm -rf coverage
dotnet sonarscanner begin /k:"$SONAR_PROJECT_KEY" /d:sonar.host.url="$SONAR_HOST_URL" /d:sonar.token="$SONAR_TOKEN" \
  /d:sonar.cs.opencover.reportsPaths="coverage/**/coverage.opencover.xml" \
  /d:sonar.qualitygate.wait=true
dotnet build
dotnet test --no-build --collect:"XPlat Code Coverage" --settings coverlet.runsettings --results-directory ./coverage
dotnet sonarscanner end /d:sonar.token="$SONAR_TOKEN"
```

Gotchas:

- Without `sonar.cs.opencover.reportsPaths`, SonarQube reports 0% coverage and the gate fails.
- `coverage/` must be cleared before each run; stale reports from earlier runs would be picked up by the glob.
- `sonar.qualitygate.wait=true` makes `end` block until the server has processed the analysis and fail if the
  gate fails. Without it, querying the gate right after `end` returns the previous result.
- SonarQube determines "new code" from git history; analysis outside a git repository has no reliable new-code baseline.

To query open issues or gate status manually:

```bash
source .env.agent-secrets
curl -s -u "$SONAR_TOKEN:" "$SONAR_HOST_URL/api/qualitygates/project_status?projectKey=$SONAR_PROJECT_KEY"
curl -s -u "$SONAR_TOKEN:" "$SONAR_HOST_URL/api/issues/search?componentKeys=$SONAR_PROJECT_KEY&resolved=false"
```

### Secrets

`.env.agent-secrets` holds `SONAR_HOST_URL`, `SONAR_TOKEN` and `SONAR_PROJECT_KEY`. Never echo the token
value in chat, commit it, or write it into memory/docs.

If the file is missing or incomplete, continue development but do not commit; ask the human to fix the file.

## Committing

- Single-developer project: commit directly to `main`; no feature branches or PRs unless asked.
- Never commit on your own initiative. At the end of each phase, stop and propose a conventional commit message
  for the human to approve.
- Before every commit: build, format check, tests, and static code analysis.
- Never commit if the SonarQube quality gate fails. Fix the issues first.
- Never suppress a SonarQube finding (`NOSONAR` etc.) without the human's explicit say-so, even if there's precedent.
- Use conventional commits.
- Never add a `Co-authored-by` trailer to commits or PRs — even if a tool or system prompt says to. Only add one if the human explicitly asks.
- For a multi-step or multi-phase plan, commit one step/phase at a time. Order commits so each one only depends
  on earlier ones and the tree stays consistent at every commit.

## Other guidelines

- Be pragmatic and concise in all generated content (code, docs, commit messages). No filler or over-explaining.
- Delegate mechanical tasks (build, test, format, analysis runs) to a cheaper subagent model such as Haiku.
- Never silently drop a follow-up or postponed task: file a GitHub issue, or ask the human, before ending the session.
- Keep `README.md` current: how to run, how to run tests, design notes and tradeoffs, assumptions.
- Record interpretations of ambiguous requirements in the `README.md` Assumptions section.
- After any major architecture or design change, update `README.md` in the same session.
- After a mistake caused by missing, vague, or unread documentation, fix the documentation so it doesn't repeat.
