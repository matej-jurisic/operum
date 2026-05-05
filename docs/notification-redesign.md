# Notification System Redesign

## Context

The notification feature shipped today (2026-04-30) in a minimal form: one notification per tracker tied to a single Analytic, evaluated every 10 minutes, fires on a false→true edge, and pushes via Web Push. We want to expand it before any real users come to depend on it. The new model decomposes a notification into three orthogonal pieces:

- **Event** — when to consider firing: `Frequency` (Day / Week / Month with sub-options) or `Triggered` (state edge).
- **Value** — what to evaluate: `Entry` (the condition runs over the tracker's entries) or `Analytic` (over an analytic's single computed value).
- **Condition** — list of filters; for `Entry` mode each filter targets a `Field`, for `Analytic` mode each filter operates on the analytic's single value.

Confirmed design choices:
- Frequency events use the condition as a **scheduled gate** — on each scheduled tick, evaluate; fire only if the condition matches. Both event types have a Value+Condition.
- Per-user TZ on the `User` entity, captured from `Intl.DateTimeFormat().resolvedOptions().timeZone` on first authenticated load if null, editable in profile.
- Per-entry firing: track triggered entry IDs per notification; one grouped push per tick reports the count of newly matched entries (a future in-app list will show them individually).
- "Skip weekends" means skip the occurrence entirely (no shift to adjacent weekday).
- Month frequency offers a `LastDayOfMonth` toggle that overrides `DayOfMonth`.

No production data is at risk — a clean schema reset is acceptable.

---

## Backend Schema

### Entities

**`TrackerNotification`** (rewritten)
- Id, TrackerId (FK, Cascade), Name, IsEnabled, ViewIds (json string)
- LastEvaluatedAt (UTC, nullable) — scheduler uses this to decide if a tick is due
- LastFiredAt (UTC, nullable) — last actual push send, surfaced in UI
- IsTriggered (bool) — kept for Triggered+Analytic edge tracking only
- 1:1 → `NotificationEvent`, 1:1 → `NotificationCondition`, 1:n → `NotificationTriggeredEntry`
- Index: `(TrackerId, IsEnabled)`

**`NotificationEvent`** (single table, discriminator on `EventType`)
- `EventType` enum: `Day | Week | Month | Triggered`
- `TimeOfDay` (TimeOnly, nullable; null only for Triggered)
- Day cols: `IntervalDays` (1-365), `SkipWeekendsDay`
- Week cols: `IntervalWeeks` (1-365), `DaysOfWeek` (int bitmask Mon..Sun)
- Month cols: `DayOfMonth` (1-31), `LastDayOfMonth`, `SkipWeekendsMonth`
- 1:1 FK to TrackerNotification (unique)

**`NotificationCondition`** (rewritten)
- `ValueMode` enum: `Entry | Analytic`
- Analytic cols: `AnalyticCode`, `AnalyticResultType` (defaults `"Single Value"`)
- 1:n → `NotificationConditionFilter`, 1:n → `NotificationConditionPurposeField`
- 1:1 FK to TrackerNotification (unique)

**`NotificationConditionFilter`**
- `FieldId` (nullable — populated for Entry mode, null for Analytic mode), `Operator`, `Value`
- Replaces today's flat `Operator`/`Value` columns on `NotificationCondition`.

**`NotificationConditionPurposeField`** (Analytic mode only)
- `Purpose` (e.g. `"Value"`), `FieldId`
- Same shape as today's `NotificationConditionField`, scoped by ValueMode usage.

**`NotificationTriggeredEntry`** (per-entry edge tracking)
- NotificationId (Cascade), EntryId (Cascade), TriggeredAt
- Unique index `(NotificationId, EntryId)`

**`User.TimeZone`** — new nullable string (IANA identifier).

### Migration

Single forward migration `20260430xxxxxx_RedesignNotifications`:
1. Drop `NotificationConditionFields`, `NotificationConditions`, `TrackerNotifications` (`UserPushSubscriptions` stays).
2. Add `AspNetUsers.TimeZone` (nullable text).
3. Recreate `TrackerNotifications` with new columns.
4. Create `NotificationEvents`, `NotificationConditions`, `NotificationConditionFilters`, `NotificationConditionPurposeFields`, `NotificationTriggeredEntries`.

`Down()` recreates the previous schema (acceptable — the prior migrations are forward-only test data).

### `OperumContext` updates

In `backend/src/Operum.Model/OperumContext.cs`:
- Drop the existing `NotificationCondition`/`NotificationConditionField` configurations.
- Add `DbSet<NotificationEvent>`, `DbSet<NotificationConditionFilter>`, `DbSet<NotificationConditionPurposeField>`, `DbSet<NotificationTriggeredEntry>`.
- Configure 1:1 FKs from `TrackerNotification` to `NotificationEvent` and `NotificationCondition`.
- Cascade `NotificationTriggeredEntry` on both sides.

---

## Backend Services

### `NotificationsService` (CRUD)

`backend/src/Operum.Service/Services/Notifications/NotificationsService.cs`

- `Create` / `Update`: split validation into `ValidateEvent` (sub-type-aware required fields) and `ValidateCondition` (Entry: all filters require `FieldId` belonging to tracker; Analytic: `AnalyticCode` required + valid via `AnalyticDefinitionList.IsValidForType`, filters must have null `FieldId`, purpose fields validated against tracker fields).
- On `Update` with a substantive condition change: `ExecuteDeleteAsync` filters/purposeFields/triggeredEntries and clear `IsTriggered`.
- On `Toggle` to disabled: clear `IsTriggered` and `TriggeredEntries` so re-enabling does not silently swallow the next true tick.
- **On `Create`**: pre-populate `NotificationTriggeredEntries` with all currently matching entry IDs for Entry-mode notifications, so the first tick does not blast about pre-existing matches. (Decision applied as default — flag if you want creation to fire about existing matches instead.)

### `NotificationEvaluatorService` (rewritten)

`backend/src/Operum.Service/Services/Notifications/NotificationEvaluatorService.cs`

Per tick:
1. Load enabled notifications with `Event`, `Condition.Filters`, `Condition.PurposeFields`, `TriggeredEntries`, `Tracker`.
2. Resolve user TZ (tracker owner) — fall back to UTC if null.
3. **Due check** (`Triggered` always due; `Frequency` delegates to `NotificationScheduleResolver.IsDue`).
4. If due, evaluate condition by `ValueMode`:
   - `Entry` → `ConditionEntryEvaluator.GetMatchingEntryIds`
   - `Analytic` → `ConditionAnalyticEvaluator.Evaluate` (single bool)
5. Apply firing rules and queue grouped pushes.
6. Update `LastEvaluatedAt = nowUtc` regardless of fire outcome.
7. `SaveChangesAsync`, dispatch queued pushes.

**Firing rules**

| Event       | Value     | Fire when                                                                                  |
|-------------|-----------|--------------------------------------------------------------------------------------------|
| Frequency   | Analytic  | Tick due AND condition true. Schedule is the only gate.                                    |
| Frequency   | Entry     | Tick due AND `currentMatch \ TriggeredEntries` non-empty.                                  |
| Triggered   | Analytic  | `condition.IsTrue && !IsTriggered` (false→true edge, today's behavior).                    |
| Triggered   | Entry     | `currentMatch \ TriggeredEntries` non-empty (set membership is the edge tracker).          |

**Per-entry triggered-state maintenance** (Entry mode, both event types):

```
currentMatch = ConditionEntryEvaluator.GetMatchingEntryIds(...)
existing     = notification.TriggeredEntries.Select(t => t.EntryId).ToHashSet()
newlyMatched = currentMatch.Except(existing)   // queue push
dropped      = existing.Except(currentMatch)   // delete rows (re-fireable later)
// unchanged: do nothing
Insert rows for newlyMatched; ExecuteDeleteAsync rows for dropped.
```

For Triggered+Analytic: set `IsTriggered = currentBool`, push only on the false→true edge.

### `NotificationScheduleResolver` (new)

`backend/src/Operum.Service/Domain/Notifications/NotificationScheduleResolver.cs` (pure, unit-testable):

```csharp
public static bool IsDue(NotificationEvent ev, DateTime nowUtc, DateTime? lastEvaluatedAtUtc, TimeZoneInfo userTz);
public static DateTime? LastScheduledOccurrenceUtc(NotificationEvent ev, DateTime nowUtc, TimeZoneInfo userTz);

private static bool MatchesDayInterval(DateOnly localDate, int intervalDays, bool skipWeekends);
private static bool MatchesWeekRule(DateOnly localDate, int intervalWeeks, ISet<DayOfWeek> daysOfWeek);
private static bool MatchesMonthRule(DateOnly localDate, int? dayOfMonth, bool lastDayOfMonth, bool skipWeekends);
```

`IsDue` logic:
1. Convert `nowUtc` → user-local.
2. Compute the most recent local "scheduled instant" matching the rule.
3. If `lastEvaluatedAtUtc == null || lastEvaluatedAtUtc < scheduledInstantUtc` → due.
4. Skip-weekends drops (does not shift) candidate occurrences on Sat/Sun.
5. `LastDayOfMonth = true` → use `DateTime.DaysInMonth(year, month)` instead of `DayOfMonth`.

Catches up correctly across missed ticks (service restart, etc.) — fires once per scheduled instant, idempotent.

### Condition evaluators (new)

`backend/src/Operum.Service/Domain/Notifications/ConditionEntryEvaluator.cs`
- Build entries query scoped by tracker and `ViewIds` (reuse `ViewQueryBuilder.MergeViewFilters` + `ApplyViewFilters` at `backend/src/Operum.Service/Domain/Views/ViewQueryBuilder.cs:115`).
- Project the condition's `NotificationConditionFilter` into `ViewFilter` instances and feed them through the same `ApplyViewFilters` so all type-specific predicates are reused.
- `Select(e => e.Id).ToListAsync(ct)`.

`backend/src/Operum.Service/Domain/Notifications/ConditionAnalyticEvaluator.cs`
- Same view scoping as today's `ComputeConditionAsync`.
- Build an `Analytic { Code, ResultType }` + `FieldMap` from `PurposeFields`.
- Call `AnalyticResultBuilder.GetAnalyticResult(...)` (`backend/src/Operum.Service/Domain/Analytics/AnalyticResultBuilder.cs:26`), cast to `SingleValueAnalyticDto`.
- AND the condition's filters via existing `NotificationConditionEvaluator.Evaluate(value, op, value)` helper.

### Push payload

`WebPushService.SendToTrackerUsersAsync(...)` is unchanged. Build payload in evaluator:
- Analytic firing: `title = "{trackerName} — {notification.Name}"`, `body = "Condition met"`.
- Entry firing: `title = "{trackerName} — {notification.Name}"`, `body = "{n} new entries match"` (singularized for n=1).
- `data.url = "/trackers/{trackerId}"` (future in-app list will append query params).

---

## API Surface

`NotificationsController.cs` URL routes unchanged. New DTO shapes in `backend/src/Operum.Model/DTOs/Notifications/`:

```csharp
record CreateTrackerNotificationDto(
    string Name, bool IsEnabled, List<string> ViewIds,
    NotificationEventDto Event, NotificationConditionDto Condition);

record NotificationEventDto(
    string EventType, string? TimeOfDay,
    int? IntervalDays, bool? SkipWeekendsDay,
    int? IntervalWeeks, List<string>? DaysOfWeek,
    int? DayOfMonth, bool? LastDayOfMonth, bool? SkipWeekendsMonth);

record NotificationConditionDto(
    string ValueMode, string? AnalyticCode,
    List<NotificationConditionPurposeFieldDto>? PurposeFields,
    List<NotificationConditionFilterDto> Filters);

record NotificationConditionFilterDto(string? FieldId, string Operator, string? Value);
record NotificationConditionPurposeFieldDto(string Purpose, string FieldId);
```

`TrackerNotificationDto` mirrors the inputs and adds `LastFiredAt`, `LastEvaluatedAt`, `IsTriggered`.

**New endpoint** on `backend/src/Operum.API/Controllers/UsersController.cs`:

```csharp
[HttpPatch("me/timezone")]
public Task<IActionResult> UpdateTimeZone([FromBody] UpdateTimeZoneDto dto);
```

Validate via `TimeZoneInfo.FindSystemTimeZoneById` (reject unknown IANA values). Add `timeZone` to the `/users/me` response so the FE knows whether to capture from the browser.

---

## Frontend

### Notification form

`frontend/src/features/notifications/components/NotificationFormDialog.tsx` becomes a Mantine `Stepper`:

1. **Event step** — `SegmentedControl` Day/Week/Month/Triggered + sub-form:
   - Day: `TimeInput`, `NumberInput` (1-365), `Switch` skip weekends.
   - Week: `TimeInput`, `NumberInput` interval weeks, `MultiSelect` Mon-Sun.
   - Month: `NumberInput` 1-31 (disabled when `LastDayOfMonth`), `Switch` `LastDayOfMonth`, `TimeInput`, `Switch` skip weekends.
   - Triggered: nothing extra.
2. **Value step** — `SegmentedControl` Entry vs Analytic.
   - Analytic branch keeps existing analytic-code `Select` and purpose-mapping selects (logic from `NotificationFormDialog.tsx:226-248`).
   - Entry branch hides them.
3. **Condition step** — filter list:
   - Analytic mode: `{ operator, value }` rows; reuse `DynamicDateValueInput` for date analytics, otherwise `NumberInput`/`TextInput` driven by `getReturnType` (already present in the form, lines 39-42).
   - Entry mode: extract the filter-row JSX from `frontend/src/features/views/components/ViewFormDialog.tsx` into a shared component `EntryFilterListEditor.tsx` consumed by both Views and Notifications. Expose `ViewFilterTemplates.tsx` quick-add buttons.

`Scope to Views` `MultiSelect` stays at the top of the form (applies to both modes).

Replace the request DTO in `frontend/src/features/notifications/types/requests/CreateTrackerNotificationDto.ts` and `NotificationDto.ts` to mirror the backend DTOs above.

### Card surface

`NotificationCard.tsx` renders a one-line schedule summary ("Every 2 days at 09:00, weekdays only" / "On change") via a new `formatNotificationEvent(event)` helper.

### Timezone capture

In `frontend/src/shared/stores/GlobalStore.ts`:

```ts
async ensureTimezoneCaptured() {
  if (!this.currentUser || this.currentUser.timeZone) return;
  const tz = Intl.DateTimeFormat().resolvedOptions().timeZone;
  await api.patch("/users/me/timezone", { timeZone: tz });
  runInAction(() => { this.currentUser = { ...this.currentUser!, timeZone: tz }; });
}
```

Call it from the auth-success path that already populates `currentUser`. Profile page (`frontend/src/features/profile/pages/ProfilePage.tsx`) gets a `Select`/`Autocomplete` populated from `Intl.supportedValuesOf("timeZone")`.

### Service worker

No changes required — `frontend/src/sw.ts` already handles the `{title, body, data:{url}}` payload shape.

---

## Reuse Map

**Backend**
- `backend/src/Operum.Service/Domain/Views/ViewQueryBuilder.cs:34` `MergeViewFilters` — view-scope merge.
- `…/ViewQueryBuilder.cs:115` `ApplyViewFilters` — used for both view scope and Entry-mode condition (project `NotificationConditionFilter` into `ViewFilter`).
- `backend/src/Operum.Service/Domain/Constants/EntryFilterMatcher.cs:15` — fallback in-memory matcher (kept available; not the primary path).
- `…/Domain/Analytics/AnalyticResultBuilder.cs:26` `GetAnalyticResult` — Analytic mode evaluation, unchanged.
- `…/Services/Notifications/NotificationConditionEvaluator.cs` — operator+value comparison on the analytic single value, unchanged.
- `…/Services/Push/WebPushService.cs:46` `SendToTrackerUsersAsync` — push dispatch, unchanged.
- `…/Operum.Model/Constants/DynamicDateTokens.cs:27` `Resolve` — date token resolution.
- `AnalyticDefinitionList.IsValidForType` — analytic-code validation in `ValidateCondition`.

**Frontend**
- `frontend/src/shared/components/DynamicDateValueInput.tsx`
- `frontend/src/features/views/components/ViewFilterTemplates.tsx`
- Filter row JSX from `ViewFormDialog.tsx` — extract once into shared `EntryFilterListEditor.tsx`.
- `operatorTypes` from `frontend/src/shared/constants/DataTypesForSelect.ts`.
- `GetStringValue` from `entries/components/EntryFormDialog.tsx`.

---

## Critical Files to Modify

- `backend/src/Operum.Model/Models/TrackerNotification.cs` (rewrite)
- `backend/src/Operum.Model/Models/NotificationCondition.cs` (rewrite)
- `backend/src/Operum.Model/Models/NotificationConditionField.cs` (rename → `NotificationConditionPurposeField`)
- New: `NotificationEvent.cs`, `NotificationConditionFilter.cs`, `NotificationTriggeredEntry.cs`
- `backend/src/Operum.Model/Models/User.cs` (+ `TimeZone`)
- `backend/src/Operum.Model/OperumContext.cs`
- `backend/src/Operum.Service/Services/Notifications/NotificationsService.cs`
- `backend/src/Operum.Service/Services/Notifications/NotificationEvaluatorService.cs`
- New: `…/Domain/Notifications/NotificationScheduleResolver.cs`, `ConditionEntryEvaluator.cs`, `ConditionAnalyticEvaluator.cs`
- `backend/src/Operum.API/Controllers/UsersController.cs` (+ TZ endpoint)
- New EF migration `20260430xxxxxx_RedesignNotifications.cs`
- `frontend/src/features/notifications/components/NotificationFormDialog.tsx` (rewrite)
- `frontend/src/features/notifications/components/NotificationCard.tsx`
- `frontend/src/features/notifications/types/NotificationDto.ts`, `CreateTrackerNotificationDto.ts`
- New: `frontend/src/features/views/components/EntryFilterListEditor.tsx` (extract from ViewFormDialog)
- `frontend/src/shared/stores/GlobalStore.ts`
- `frontend/src/features/profile/pages/ProfilePage.tsx`

---

## Verification

1. **Migration**: drop dev DB, `dotnet ef database update`, confirm schema; or apply on existing dev DB and confirm `User.TimeZone` is null and notification tables are empty.
2. **TZ capture**: log in fresh user → `PATCH /users/me/timezone` fires once; reload → no second PATCH; profile edit persists.
3. **Frequency-Day-Entry, skip weekends**: IntervalDays=1, TimeOfDay=09:00, SkipWeekends=true, filter `Status = "Open"`. Set `LastEvaluatedAt` to a Friday 10:00 local; advance now to Monday 09:30 local. Confirm Sat/Sun skipped (not shifted), exactly one push fires.
4. **Frequency-Week**: IntervalWeeks=2, DaysOfWeek=[Tue, Thu]. Confirm fires on those days only and respects 2-week interval.
5. **Frequency-Month-LastDayOfMonth**: TimeOfDay=23:00, LastDayOfMonth=true, SkipWeekends=true. Verify Feb 28 vs Feb 29; verify months whose last day is Sat/Sun are skipped.
6. **Triggered-Analytic edge**: notification on `Count > 5`. Add 4 → no push. Add 5th → no push. Add 6th → push, `IsTriggered=true`. Add 7th → no push. Delete 2 → `IsTriggered=false`. Add new → push fires again.
7. **Triggered-Entry per-entry**: filter `Priority = "High"`. Add three matching entries in one tick → one grouped push "3 new entries match"; `NotificationTriggeredEntries` rows = 3. Next tick with no new entries → no push. Edit one entry to lower priority → row removed. Reset priority → push fires again ("1 new entry matches").
8. **Frequency-Entry**: schedule gate combined with per-entry. Between scheduled ticks, intermediate changes do not push; on next scheduled tick, only entries currently matching but not in `TriggeredEntries` push.
9. **Initialization**: create an Entry-mode notification on a tracker that already has 50 matching entries. Confirm `TriggeredEntries` is pre-populated on create and the first tick does NOT push.
10. **Push payload**: SW receives `{title, body, data:{url}}`; clicking opens `/trackers/{id}`.
11. **Authorization**: only owner / `CanEditSchema` collaborators can CRUD notifications.
12. **Cleanup on disable**: toggle a Triggered-Analytic notification off → `IsTriggered=false`, `TriggeredEntries` cleared. Re-enable does not silently swallow the next true tick.
