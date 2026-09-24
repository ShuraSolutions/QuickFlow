# Loops Configuration (project-specific)

This is the **only** file in `loops/` that contains project-specific values.
Both loops (`backend-dev`, `frontend-dev`) and the slash commands read their
stack, folders, ports, URLs and commands from the YAML block below, referring
to values by key path (e.g. `backend.urls.base`). To reuse the loops for
another application, edit this file only.

All paths are relative to the repository root.

```yaml
project:
  name: QuickFlow
  default_requirements: docs/Task_PRD.md        # used when no path is passed via $ARGUMENTS
  reference_docs:
    - docs/task-description.txt                 # assignment brief (context only, not requirements)

shell:
  # Windows host. Prefer the Bash tool (Git Bash) for curl and scripts.
  # In PowerShell, `curl` is an alias of Invoke-WebRequest: always call `curl.exe`.
  curl_binary: curl.exe
  timestamp_command: powershell -NoProfile -Command "Get-Date -Format o"
  token_usage_command: powershell -NoProfile -ExecutionPolicy Bypass -File loops/_shared/token-usage.ps1

backend:
  stack:
    language: C#
    framework: ASP.NET Core Web API
    target_framework: net8.0                    # built with the installed .NET 9 SDK; runs on the .NET 8 runtime
    orm: Entity Framework Core 8 (Microsoft.EntityFrameworkCore.Sqlite)
    database: SQLite
    api_docs: Swashbuckle.AspNetCore (Swagger / OpenAPI 3)
    unit_tests: xUnit (optional)
    package_version_rule: pin EF Core / Swashbuckle packages to versions compatible with net8.0 (EF Core 8.x)
  folder: backend/
  solution: backend/QuickFlow.sln
  projects:
    api: backend/src/QuickFlow.Api              # controllers, DTOs, Program.cs, EF DbContext
    domain: backend/src/QuickFlow.Domain        # entities + business rules, no ASP.NET dependency
    tests: backend/tests/QuickFlow.Tests        # optional unit tests
  database_file: backend/src/QuickFlow.Api/quickflow.db
  database_strategy: EF Core migrations applied at startup (Database.Migrate()); db file is git-ignored
  architecture_notes: >
    Keep business rules (validation, status transitions, rest-time and
    completion roll-up computations) in the domain project so they are
    unit-testable without HTTP. Controllers stay thin. Return RFC 7807
    ProblemDetails for validation (400) and not-found (404) errors.
    Serialize enums as strings.
  commands:
    restore: dotnet restore backend/QuickFlow.sln
    build: dotnet build backend/QuickFlow.sln -c Debug
    run: dotnet run --project backend/src/QuickFlow.Api --no-launch-profile -- --urls http://localhost:5080 --environment Development
    test: dotnet test backend/QuickFlow.sln
    install_tools: dotnet tool install dotnet-ef --version 8.* --create-manifest-if-needed
    add_migration: dotnet ef migrations add <Name> --project backend/src/QuickFlow.Api --output-dir Data/Migrations
    stop: powershell -NoProfile -Command "Get-NetTCPConnection -LocalPort 5080 -State Listen -ErrorAction SilentlyContinue | ForEach-Object { Stop-Process -Id $_.OwningProcess -Force }"
  port: 5080
  urls:
    base: http://localhost:5080
    health: http://localhost:5080/health
    swagger_json: http://localhost:5080/swagger/v1/swagger.json
    swagger_ui: http://localhost:5080/swagger
  startup_timeout_seconds: 60
  cors:
    allowed_origins:
      - http://localhost:5500
      - http://127.0.0.1:5500
  swagger_export_path: docs/api/swagger.json    # handed to frontend-dev by the orchestrator

frontend:
  stack:
    languages: HTML5, CSS3, JavaScript (ES2017+, no modules bundler)
    libraries: jQuery 3.7.1 only
    frameworks: none                            # no React/Vue/Angular/Bootstrap, no build step, no npm deps in frontend/
    unit_tests: optional, plain browser-runnable tests (e.g. frontend/tests/*.html with QUnit from vendor/) if added
  folder: frontend/
  entry_page: frontend/index.html
  structure_notes: >
    Single-page app with hash routing (#/dashboard, #/tasks, ...) and a
    persistent navigation bar. One JS file per page/feature under
    frontend/js/pages/, a shared API client under frontend/js/api.js built
    from the Swagger spec, shared helpers under frontend/js/lib/. Keep
    presentation separate from computation helpers (rest time, percentages).
  vendor:
    jquery: frontend/vendor/jquery-3.7.1.min.js   # download once from https://code.jquery.com/jquery-3.7.1.min.js (no CDN at runtime)
  api_config_file: frontend/js/config.js          # window.APP_CONFIG = { apiBaseUrl: "http://localhost:5080" }
  port: 5500
  url: http://localhost:5500
  commands:
    serve: npx --yes http-server frontend -p 5500 -c-1 --silent
    stop: powershell -NoProfile -Command "Get-NetTCPConnection -LocalPort 5500 -State Listen -ErrorAction SilentlyContinue | ForEach-Object { Stop-Process -Id $_.OwningProcess -Force }"
  startup_timeout_seconds: 30

verification:
  max_attempts: 3
  curl_timeout_seconds: 15
  unit_tests_required: false                    # optional extra verification in both loops
  playwright:
    mcp_server: playwright                      # name in .mcp.json (@playwright/mcp, --browser msedge; Chrome not installed)
    screenshot_dir: loops/frontend-dev/outputs/screenshots   # also the MCP server's --output-dir
    viewport: 1280x800
    fail_on_console_errors: true
```
