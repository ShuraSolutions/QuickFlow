# phase-06-settings — Settings

| Field | Value |
|---|---|
| Loop | backend-dev |
| Status | pending |
| Attempts | 0 / 3 |
| Depends on | phase-01-scaffold |
| Requirements covered | §4 Settings page, §8 Settings (profile info, notification behavior, default view), NFR Reliability (persist between sessions) |
| Requirements source | docs/Task_PRD.md |
| Started | — |
| Ended | — |

## Goal
A single persisted settings record (user profile + application preferences) that the Settings page can read and update.

## Tasks
<!-- Mark [x] only when the task is implemented AND covered by a passing check. -->
- [ ] T1 Domain: `UserSettings` (Id=1, DisplayName, Email, PlanStartNotificationsEnabled, NotificationLeadMinutes, DefaultView, Theme, UpdatedAt) + `DefaultView` (Dashboard, Tasks, Habits, LearningResources, TodoPlans, Settings) + `ThemePreference` (Light, Dark, System)
- [ ] T2 Domain rules: display name ≤100, email optional but valid format and ≤200, notification lead minutes 0..1440, enums defined
- [ ] T3 EF mapping + migration `phase-06-settings` with a seeded default row
- [ ] T4 `SettingsController`: `GET /api/settings`, `PUT /api/settings`

## Acceptance checks
<!-- Each check is concrete and observable. Tag one or more checks [smoke]; they are re-run as regression in later phases. -->
- [ ] C1 [smoke] `GET /api/settings` → `200` with defaults (`defaultView == "Dashboard"`, `planStartNotificationsEnabled == true`, `theme == "System"`)
- [ ] C2 `PUT /api/settings` valid body → `200` with the new values; a later `GET` returns them
- [ ] C3 `PUT /api/settings` with `email:"not-an-email"` → `400` `errors.Email`; `defaultView:"Nowhere"` → `400`; `notificationLeadMinutes: -1` → `400`
- [ ] C4 Settings persist across API restart

## Verification

_Not run yet._

## Unit tests (optional)
- Command: —
- Result: —

## Failure record
—

## Notes
—
