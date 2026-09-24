# phase-01-scaffold — Scaffold & infrastructure

| Field | Value |
|---|---|
| Loop | backend-dev |
| Status | completed |
| Attempts | 1 / 3 |
| Depends on | — |
| Requirements covered | NFR Reliability (persistence), NFR Maintainability (domain separated from presentation), config: CORS, Swagger, ProblemDetails, health |
| Requirements source | docs/Task_PRD.md |
| Started | 2026-09-24T12:31:37+03:00 |
| Ended | 2026-09-24T12:36:25+03:00 |

## Goal
An ASP.NET Core Web API solution (Api, Domain, Tests projects) that starts on the configured port, applies EF Core SQLite migrations at startup, exposes Swagger, a health endpoint, CORS for the frontend origins, and returns RFC 7807 ProblemDetails for errors.

## Tasks
<!-- Mark [x] only when the task is implemented AND covered by a passing check. -->
- [x] T1 Create solution `backend/QuickFlow.sln` with projects `QuickFlow.Api` (web), `QuickFlow.Domain` (class library, no ASP.NET dependency), `QuickFlow.Tests` (xUnit), all targeting net8.0; project references Api→Domain, Tests→Domain
- [x] T2 Add pinned NuGet packages: EF Core 8 Sqlite + Design, Swashbuckle.AspNetCore; local tool manifest with dotnet-ef 8.*
- [x] T3 `QuickFlowDbContext` on SQLite `quickflow.db` (content root), UTC DateTime value convention; initial migration `phase-01-scaffold` in `Data/Migrations`; `Database.Migrate()` at startup
- [x] T4 JSON options: camelCase, enums as strings (integer enum values rejected)
- [x] T5 Swagger/OpenAPI 3 at `/swagger` and `/swagger/v1/swagger.json`, with XML doc comments
- [x] T6 CORS policy allowing the configured origins (any header/method)
- [x] T7 Error handling: domain exceptions (validation → 400 ValidationProblemDetails, not found → 404, conflict → 409) mapped by an `IExceptionHandler`; `AddProblemDetails` + status code pages so empty 404/405 responses carry ProblemDetails
- [x] T8 `GET /health` returning 200 `{status:"Healthy", database:"Connected"}` (checks DB connectivity)
- [x] T9 Tests project builds and runs (domain exception test)

## Acceptance checks
<!-- Each check is concrete and observable. Tag one or more checks [smoke]; they are re-run as regression in later phases. -->
- [x] C1 [smoke] `GET /health` → `200`, body `status == "Healthy"`, `database == "Connected"`
- [x] C2 [smoke] `GET /swagger/v1/swagger.json` → `200`, valid JSON with `openapi` starting `3.`
- [x] C3 `GET /swagger/index.html` → `200` (Swagger UI)
- [x] C4 CORS preflight `OPTIONS /health` with `Origin: http://localhost:5500` → `204`, `Access-Control-Allow-Origin: http://localhost:5500`
- [x] C5 CORS preflight with `Origin: http://127.0.0.1:5500` → `Access-Control-Allow-Origin: http://127.0.0.1:5500`
- [x] C6 CORS preflight with `Origin: http://evil.example` → no `Access-Control-Allow-Origin` header
- [x] C7 `GET /api/does-not-exist` → `404` with `Content-Type: application/problem+json` and `status: 404`
- [x] C8 Database file `backend/src/QuickFlow.Api/quickflow.db` exists after startup and `__EFMigrationsHistory` contains the initial migration

## Verification

### Attempt 1 — 2026-09-24T12:34:53+03:00
**Build:** PASS (`dotnet build backend/QuickFlow.sln -c Debug`, 0 warnings, 0 errors)
**API start:** PASS (`dotnet run --project backend/src/QuickFlow.Api --no-launch-profile -- --urls http://localhost:5080 --environment Development` in background after the configured stop command; polling `/health` printed `health 200 after 0 s`; log: `Content root path: C:\Users\hm245\source\repos\QuickFlow\backend\src\QuickFlow.Api`)

Raw output of every check (commands prefixed with `$`):

```text
=== C1
$ curl.exe -s -i --max-time 15 http://localhost:5080/health
HTTP/1.1 200 OK
Content-Type: application/json; charset=utf-8
Date: Thu, 24 Sep 2026 09:34:53 GMT
Server: Kestrel
Transfer-Encoding: chunked

{"status":"Healthy","database":"Connected","serverTimeUtc":"2026-09-24T09:34:53.8338025Z"}

=== C2
$ curl.exe -s -i --max-time 15 http://localhost:5080/swagger/v1/swagger.json
HTTP/1.1 200 OK
Content-Type: application/json;charset=utf-8
Date: Thu, 24 Sep 2026 09:34:53 GMT
Server: Kestrel
Transfer-Encoding: chunked

{
  "openapi": "3.0.1",
  "info": {
    "title": "QuickFlow API",
    "description": "Tasks, habits, learning resources, todo plans, settings and dashboard for the QuickFlow personal productivity app.",
    "version": "v1"
  },
  "paths": {
    "/health": {
      "get": {
        "tags": [
          "Health"
        ],
        "summary": "Returns 200 when the API is up and the database is reachable, 503 otherwise.",
        "responses": {
          "200": {
            "description": "OK",
            "content": {
              "application/json":
$ node -e '<parse swagger.json, print openapi + path count>'
openapi=3.0.1 paths=1 [/health]

=== C3
$ curl.exe -s -i --max-time 15 http://localhost:5080/swagger/index.html | head -12
HTTP/1.1 200 OK
Content-Type: text/html;charset=utf-8
Date: Thu, 24 Sep 2026 09:34:54 GMT
Server: Kestrel
Transfer-Encoding: chunked

<!-- HTML for static distribution bundle build -->
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <title>Swagger UI</title>

=== C4
$ curl.exe -s -i --max-time 15 -X OPTIONS http://localhost:5080/health -H Origin: http://localhost:5500 -H Access-Control-Request-Method: GET
HTTP/1.1 204 No Content
Date: Thu, 24 Sep 2026 09:34:54 GMT
Server: Kestrel
Access-Control-Allow-Methods: GET
Access-Control-Allow-Origin: http://localhost:5500
Vary: Origin



=== C5
$ curl.exe -s -i --max-time 15 -X OPTIONS http://localhost:5080/health -H Origin: http://127.0.0.1:5500 -H Access-Control-Request-Method: POST -H Access-Control-Request-Headers: content-type
HTTP/1.1 204 No Content
Date: Thu, 24 Sep 2026 09:34:54 GMT
Server: Kestrel
Access-Control-Allow-Headers: content-type
Access-Control-Allow-Methods: POST
Access-Control-Allow-Origin: http://127.0.0.1:5500
Vary: Origin



=== C6
$ curl.exe -s -i --max-time 15 -X OPTIONS http://localhost:5080/health -H Origin: http://evil.example -H Access-Control-Request-Method: GET
HTTP/1.1 204 No Content
Date: Thu, 24 Sep 2026 09:34:54 GMT
Server: Kestrel



=== C7
$ curl.exe -s -i --max-time 15 http://localhost:5080/api/does-not-exist
HTTP/1.1 404 Not Found
Content-Type: application/problem+json
Date: Thu, 24 Sep 2026 09:34:54 GMT
Server: Kestrel
Transfer-Encoding: chunked

{"type":"https://tools.ietf.org/html/rfc9110#section-15.5.5","title":"Not Found","status":404}

=== C8
$ ls -la backend/src/QuickFlow.Api/quickflow.db
-rw-r--r-- 1 hm245 197609 4096 Sep 24 12:34 backend/src/QuickFlow.Api/quickflow.db
$ node -e '<read __EFMigrationsHistory via sqlite>'
$ python3 q.py backend/src/QuickFlow.Api/quickflow.db "SELECT MigrationId, ProductVersion FROM __EFMigrationsHistory"
('20260924093416_phase-01-scaffold', '8.0.11')
```

| Check | Expected | Result |
|---|---|---|
| C1 [smoke] | 200, status Healthy, database Connected | **PASS** |
| C2 [smoke] | 200, valid JSON, openapi 3.x | **PASS** (openapi=3.0.1) |
| C3 | Swagger UI 200 | **PASS** |
| C4 | 204 + ACAO http://localhost:5500 | **PASS** |
| C5 | ACAO http://127.0.0.1:5500 | **PASS** |
| C6 | no ACAO header for http://evil.example | **PASS** (header absent) |
| C7 | 404 application/problem+json, status 404 | **PASS** |
| C8 | db file exists, migration recorded | **PASS** (4096 bytes; `20260924093416_phase-01-scaffold`) |

#### Smoke regression
No previously completed phases.

**Swagger export:** PASS → `docs/api/swagger.json` (`valid JSON, openapi 3.0.1, 1 paths, 1 operations`)
**Unit tests:** `dotnet test backend/QuickFlow.sln` → `Passed!  - Failed:     0, Passed:     3, Skipped:     0, Total:     3`
**API stop:** configured stop command run → `API stopped`
**Result:** PASS 8/8

## Unit tests (optional)
- Command: `dotnet test backend/QuickFlow.sln`
- Result: Passed 3, Failed 0 (ValidatorTests)

## Failure record
—

## Notes
- Restore failed because of a machine-wide NuGet source that does not exist (`NU1301 ... Almesreya\Offline packages`). Added repo-local `backend/nuget.config` (clears sources, nuget.org only). Global NuGet config was not changed.
- `dotnet-ef` 8.0.31 is installed in a local tool manifest at `backend/.config/dotnet-tools.json` (not at the repo root) to respect folder ownership. Migrations are therefore run from `backend/`: `dotnet ef migrations add <phase-slug> --project src/QuickFlow.Api --output-dir Data/Migrations` (same as `config.backend.commands.add_migration`, but with paths relative to `backend/`).
- The SQLite path is resolved against the content root (`backend/src/QuickFlow.Api`) via `Database:File` in appsettings, so the DB lands at `config.backend.database_file` whatever the working directory.
- All DateTimes are stored and serialized as UTC (`Z` suffix). Calendar dates (`DateOnly`) use the server's local date for "today".
- Enums are serialized as strings; integer enum values are rejected (`allowIntegerValues: false`).
