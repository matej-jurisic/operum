# intervals.icu Integration — Implementation Plan

## Context

Operum has no inbound data path except the authenticated UI (manual entry + CSV upload). This plan builds the first real integration — intervals.icu daily wellness — and, more importantly, establishes the primitives every later integration reuses: a stored per-user connection, a user-defined field mapping, an idempotent write path, and a scheduled sync loop.

intervals.icu was chosen over Garmin Health as the first connector because its API is self-serve (an athlete generates a key in settings; no partner program approval), and because it already aggregates from Garmin, Strava, Wahoo, Zwift, Polar and Coros — one connector fans in from most vendors.

Confirmed design decisions:

- **Explicit user-defined mapping.** The user picks which intervals field maps to which tracker field. No auto-creation of fields, no name-guessing. Type compatibility is validated at mapping time against `DataTypes`.
- **No transform DSL.** The mapper does mechanical coercion only (`sleepSecs` seconds → `timespan`). Every derivation is left to existing calculated fields and `{FieldName}` formulas — a second expression language would overlap badly with the one that already exists.
- **Null is skipped, not zeroed.** Real wellness records return `null` for unlogged metrics. Writing `0` would silently poison averages and charts.
- **Upsert, not append.** A wellness record is a daily snapshot keyed by date, revised throughout the day as devices sync. Re-syncing must update, not duplicate.
- **v1 is wellness only.** Activities are a separate resource with a different shape; the schema below leaves room for them without committing to them.
- **Feature-flagged.** Ships dark behind `Features:Integrations`, mirroring how notifications shipped.

---

## Source Catalog

The wellness resource is a fixed schema, so the source side is a hardcoded, typed catalog rather than anything inferred at runtime. `id` is the record date (`YYYY-MM-DD`), one record per athlete per day.

| Source key | Operum type | Notes |
|---|---|---|
| `id` | `date` | Record key. Always mapped, not user-selectable. |
| `updated` | `datetime` | Last revision timestamp; used as the change cursor. |
| `sleepSecs` | `timespan` | Integer seconds — needs `SecondsToTimeSpan` coercion. |
| `menstrualPhase`, `menstrualPhasePredicted` | `string` | Enum-valued. |
| `comments` | `string` | |
| `locked`, `tempWeight`, `tempRestingHR` | `bool` | |
| `ctl`, `atl`, `rampRate`, `ctlLoad`, `atlLoad` | `number` | Computed training load. |
| `weight`, `restingHR`, `hrv`, `hrvSDNN`, `vo2max`, `bodyFat`, `abdomen` | `number` | |
| `sleepScore`, `sleepQuality`, `avgSleepingHR` | `number` | |
| `soreness`, `fatigue`, `stress`, `mood`, `motivation`, `injury` | `number` | Subjective 1–5 scales. |
| `spO2`, `systolic`, `diastolic`, `respiration`, `steps` | `number` | |
| `readiness`, `baevskySI`, `bloodGlucose`, `lactate` | `number` | |
| `hydration`, `hydrationVolume`, `kcalConsumed`, `carbohydrates`, `protein`, `fatTotal` | `number` | |
| `sportInfo` | — | **Excluded from v1.** Nested array (`type`/`eftp`/`wPrime`/`pMax`); fields are flat scalars. Revisit by flattening per sport type (`eFTP_Ride`, `eFTP_Run`). |

Lives in `backend/src/Operum.Model/Constants/Integrations/IntervalsWellnessCatalog.cs` as `record IntervalsSourceField(string Key, string Type, string Label, string? Coercion)`, with a `ByKey` lookup and `IsValid(key)` — same shape as `DataTypes`/`OperatorTypes`.

### Field count constraint

The catalog is ~45 entries; `DataLimits.MaxFieldCount` is 25. Selective mapping is therefore not just a UX nicety, it is a hard requirement. The mapping UI must show remaining field budget and block a mapping that would push the tracker past the cap.

---

## Backend Schema

### Entities

**`Integration`** — one per user per provider account.
- `Id`, `UserId` (FK, Cascade), `Provider` (`"intervals.icu"`), `ExternalAccountId` (athlete id)
- `CredentialCiphertext` (string) — API key, encrypted at rest
- `IsEnabled`, `CreatedAt`
- Unique index `(UserId, Provider, ExternalAccountId)`
- 1:n → `IntegrationTarget`

**`IntegrationTarget`** — one per (connection, tracker, resource). Modelled separately from `Integration` so activities can be added later as a second target against the same connection.
- `Id`, `IntegrationId` (FK, Cascade), `TrackerId` (FK, Cascade)
- `ResourceType` (`"wellness"`)
- `IsEnabled`, `BackfillFrom` (DateOnly), `LastSyncedAt` (UTC, nullable), `LastSyncStatus` (`Ok | Error | Never`), `LastSyncError` (string, nullable), `LastCursor` (UTC, nullable — max `updated` seen)
- Unique index `(IntegrationId, TrackerId, ResourceType)`
- 1:n → `IntegrationFieldMapping`

**`IntegrationFieldMapping`**
- `Id`, `TargetId` (FK, Cascade), `SourceKey`, `FieldId` (FK, Cascade)
- `SkipWhenNull` (bool, default `true`)
- Unique index `(TargetId, FieldId)` — one source per tracker field; a tracker field cannot be fed by two sources.

**`Entry`** — add `Source` (string, nullable) and `ExternalId` (string, nullable).
- Unique filtered index `(TrackerId, Source, ExternalId)` where `Source IS NOT NULL`.
- This is the idempotency key. Manually created entries leave both null and are unaffected.

### Migration

`20260824xxxxxx_AddIntegrations`:
1. `AspNetUsers` untouched.
2. Add `Entries.Source`, `Entries.ExternalId` (both nullable text) + the filtered unique index.
3. Create `Integrations`, `IntegrationTargets`, `IntegrationFieldMappings`.

No data migration needed — existing entries keep null source.

### `OperumContext`

Add `DbSet<Integration>`, `DbSet<IntegrationTarget>`, `DbSet<IntegrationFieldMapping>`. Cascade all three from `Integration` down. Configure the filtered unique index on `Entry` via `HasFilter`.

---

## Backend Services

### The write-path problem (solve this first)

`EntriesService.CreateEntry` (`backend/src/Operum.Service/Services/Entries/EntriesService.cs:24`) opens with `currentUserService.GetCurrentUser()`, and `CurrentUserService` reads from `IHttpContextAccessor` (`backend/src/Operum.Service/Services/Authorization/CurrentUserService.cs:22`). A `BackgroundService` has no HTTP context, so the sync loop **cannot call `EntriesService`**. The same applies to `ImportEntriesFromCsv`.

Extract a context-free writer before anything else:

`backend/src/Operum.Service/Domain/Entries/EntryWriter.cs`

```csharp
public sealed record EntryWriteResult(int Created, int Updated, int Skipped, List<string> Errors);

Task<EntryWriteResult> UpsertAsync(
    string trackerId,
    string source,
    IReadOnlyList<(string ExternalId, Dictionary<string, string?> ValuesByFieldId)> records,
    List<Field> fields,
    CancellationToken ct);
```

Responsibilities — and nothing else (no authorization, no current user):
1. Load existing entries for `(trackerId, source, externalId in batch)` with their `FieldValues`.
2. For each record: reuse the existing entry or create one.
3. Coerce via the existing `fieldValue.SetFieldValue(field, value)` (`backend/src/Operum.Model/Extensions/FieldValueExtensions.cs:27`) — it already handles every branch of `DataTypes` and returns `false` on unknown type.
4. Skip null values when the mapping says so; otherwise clear the `FieldValue`.
5. Enforce `DataLimits.MaxEntryCount` against *new* entries only (updates to existing rows must not be blocked by the cap).
6. `SaveChangesAsync`, then `formulaEvaluationService.EvaluateAndPersistCalculatedFields(trackerId, entryId, fieldValues, fields)` per touched entry — the same call `CreateEntry` makes at line 79.

Authorization stays where it belongs: the API layer checks it for user-driven calls, and the sync service resolves permission from `Integration.UserId` → tracker ownership before invoking the writer.

**Follow-up (not blocking v1):** point `CreateEntry` and `ImportEntriesFromCsv` at `EntryWriter` too. Until they do, CSV import and integration import can drift in their coercion behaviour — the exact failure mode this extraction exists to prevent.

### `IIntervalsClient`

`backend/src/Operum.Service/Integrations/Intervals/IntervalsClient.cs` — sits beside the existing `Integrations/MailSender/` folder, matching that layout.

```csharp
Task<Result<IntervalsAthleteDto>> GetAthleteAsync(string apiKey, string athleteId, CancellationToken ct);
Task<Result<List<IntervalsWellnessDto>>> GetWellnessAsync(
    string apiKey, string athleteId, DateOnly oldest, DateOnly newest, CancellationToken ct);
```

- HTTP Basic auth, username `API_KEY`, password = the athlete's key. **Verify against current docs before implementing** — this and the exact wellness route are the two facts in this plan taken from memory rather than from the codebase.
- Register via `AddHttpClient<IIntervalsClient, IntervalsClient>()` with a timeout and a small retry/backoff. Do **not** register as a singleton wrapping a raw `HttpClient`.
- `IntervalsWellnessDto` mirrors the payload with every numeric property nullable (`double?`, `int?`) — this is what makes the null-vs-zero distinction survive deserialization. Do not use non-nullable value types here.

### `IntegrationsService` (CRUD)

`backend/src/Operum.Service/Services/Integrations/IntegrationsService.cs` — follows the `Result<T>` / `ResultStatusCodes` conventions used throughout.

- `Connect(dto)` — validate the key by calling `GetAthleteAsync`, resolve the athlete id, encrypt and store. Reject with a useful message on 401 rather than storing an unverified key.
- `CreateTarget` / `UpdateTarget` — validate that the caller owns the tracker, that every `SourceKey` exists in the catalog, that every `FieldId` belongs to the tracker and is **not** `IsCalculated`, and that source and target types are compatible.
- `ValidateMapping` — the shared rule:

| Source type | Accepts tracker field types |
|---|---|
| `number` | `number` |
| `timespan` | `timespan`, `number` (raw seconds) |
| `string` | `string` |
| `bool` | `bool` |
| `date` / `datetime` | `date`, `datetime` |

  Also reject mapping a nullable source onto a `Required` field with `SkipWhenNull = true` — those writes would fail on every record missing that metric.
- `DeleteTarget` / `Disconnect` — cascade; leave already-imported entries in place.
- `SyncNow(targetId)` — manual trigger, shares the executor below.

### Credential storage

Use ASP.NET Core Data Protection — already available, no new dependency:

```csharp
services.AddDataProtection().PersistKeysToDbContext<OperumContext>(); // or a mounted volume
var protector = provider.CreateProtector("Operum.Integrations.Credentials");
```

Keys must persist across container restarts or every stored credential becomes undecryptable. `.env` gets `DataProtection__KeyPath` and docker-compose gets a volume for it. The API key must never appear in a DTO returned to the frontend — expose a masked suffix (`…a91f`) only.

### `IntegrationSyncService` (BackgroundService)

`backend/src/Operum.Service/Services/Integrations/IntegrationSyncService.cs` — copy the structure of `NotificationEvaluatorService` (`PeriodicTimer`, `CreateAsyncScope` per tick, try/catch around the load, config-driven interval):

```csharp
private TimeSpan Interval => TimeSpan.FromMinutes(
    configuration.GetValue<int>("Integrations:SyncIntervalMinutes", 60));
```

Per tick, per enabled target:
1. Decrypt the credential.
2. Determine the window: `LastSyncedAt == null` → `BackfillFrom`..today; otherwise `today - ReconciliationDays`..today (default 7, so late device syncs and manual edits are picked up).
3. `GetWellnessAsync` for the window.
4. Drop records whose `updated <= LastCursor` **and** whose date is outside the reconciliation window.
5. Project each record through the target's mappings into `(externalId: record.id, valuesByFieldId)`, skipping nulls per `SkipWhenNull`.
6. `EntryWriter.UpsertAsync(trackerId, source: "intervals.icu", …)`.
7. Update `LastSyncedAt`, `LastCursor`, `LastSyncStatus`, `LastSyncError`.

Failures are per-target: one athlete's revoked key must not abort the tick for everyone else. Log and record the error on the target, then continue.

**Backfill and the entry cap.** `DataLimits.MaxEntryCount` is 1000, so daily wellness saturates a tracker in ~2.7 years. `BackfillFrom` must default to something modest (12 months) rather than "all history", and the UI must say plainly what happens at the cap — otherwise sync silently stops working one day with no explanation.

### DI registration

In `ServiceConfiguration.RegisterBusinessServices`, mirroring the notifications block at lines 60-66:

```csharp
services.AddScoped<IIntegrationsService, IntegrationsService>();
services.AddHttpClient<IIntervalsClient, IntervalsClient>();

if (configuration.GetValue("Features:Integrations", false))
{
    services.AddHostedService<IntegrationSyncService>();
}
```

Add `Integrations` to `FeatureSettings` and a `RequiresIntegrationsAttribute` copied from `backend/src/Operum.API/Filters/RequiresNotificationsAttribute.cs`.

---

## API Surface

New `backend/src/Operum.API/Controllers/IntegrationsController.cs`, decorated `[RequiresIntegrations]`:

| Method | Route | Purpose |
|---|---|---|
| `GET` | `/api/integrations/providers` | Provider list + the source-field catalog for each |
| `GET` | `/api/integrations` | Current user's connections (credential masked) |
| `POST` | `/api/integrations` | Connect — validates the key before storing |
| `DELETE` | `/api/integrations/{id}` | Disconnect |
| `GET` | `/api/integrations/{id}/targets` | Targets + mappings |
| `POST` | `/api/integrations/{id}/targets` | Create target + mappings |
| `PUT` | `/api/integrations/{id}/targets/{targetId}` | Update mappings / backfill / enabled |
| `DELETE` | `/api/integrations/{id}/targets/{targetId}` | Remove target |
| `POST` | `/api/integrations/{id}/targets/{targetId}/sync` | Sync now |

DTOs in `backend/src/Operum.Model/DTOs/Integrations/`:

```csharp
record ConnectIntegrationDto(string Provider, string ApiKey, string? AthleteId);
record IntegrationDto(string Id, string Provider, string ExternalAccountId,
    string MaskedCredential, bool IsEnabled, List<IntegrationTargetDto> Targets);
record IntegrationTargetDto(string Id, string TrackerId, string ResourceType, bool IsEnabled,
    DateOnly BackfillFrom, DateTime? LastSyncedAt, string LastSyncStatus, string? LastSyncError,
    List<IntegrationFieldMappingDto> Mappings);
record IntegrationFieldMappingDto(string SourceKey, string FieldId, bool SkipWhenNull);
record SourceFieldDto(string Key, string Type, string Label);
```

---

## Frontend

New feature folder `frontend/src/features/integrations/`, matching the existing `features/*` layout.

- **`pages/IntegrationsPage.tsx`** — connection list, "Connect intervals.icu" flow (API key input, validate on submit, show the resolved athlete).
- **`components/TargetFormDialog.tsx`** — the mapping editor. Two columns: source field on the left (from `/providers`), tracker field `Select` on the right, filtered to type-compatible, non-calculated, unmapped fields. Per-row `SkipWhenNull` switch. Header shows `n / 25 fields used`.
  - Reuse the analytics field-picker pattern: analytics already restricts pickers to types valid for the role, which is the same interaction. Pull that filtering into a shared helper rather than writing a second version.
- **`components/IntegrationCard.tsx`** — last sync time, status, error text, "Sync now".
- **`stores/IntegrationsStore.ts`** — MobX, following the existing feature stores.
- Gate the route on a build-time flag (`VITE_REACT_INTEGRATIONS_ENABLED`) exactly as notifications does, so the frontend never has to ask the backend whether the feature exists.

---

## Reuse Map

- `backend/src/Operum.Model/Extensions/FieldValueExtensions.cs:27` `SetFieldValue` — string → typed coercion, all six `DataTypes` branches. The mapper must not reimplement this.
- `backend/src/Operum.Service/Interfaces/IFormulaEvaluationService.cs:7` `EvaluateAndPersistCalculatedFields` — calculated fields after every upsert.
- `backend/src/Operum.Service/Services/Notifications/NotificationEvaluatorService.cs:17-32` — `BackgroundService` + `PeriodicTimer` + scoped-per-tick structure to copy.
- `backend/src/Operum.API/Filters/RequiresNotificationsAttribute.cs` — feature-gate filter to copy.
- `backend/src/Operum.API/Configuration/ServiceConfiguration.cs:60-66` — conditional hosted-service registration pattern.
- `backend/src/Operum.Service/Integrations/MailSender/` — folder layout and `IOptions<T>` config binding for an external client.
- `backend/src/Operum.Model/Constants/Fields/DataTypes.cs` — type validation for mappings.
- `backend/src/Operum.Service/Services/Entries/EntriesService.cs:277-448` `ImportEntriesFromCsv` — the required-field validation and batch-insert shape that `EntryWriter` generalizes.

---

## Critical Files

**New**
- `backend/src/Operum.Model/Models/Integration.cs`, `IntegrationTarget.cs`, `IntegrationFieldMapping.cs`
- `backend/src/Operum.Model/Constants/Integrations/IntervalsWellnessCatalog.cs`
- `backend/src/Operum.Model/DTOs/Integrations/*`
- `backend/src/Operum.Service/Domain/Entries/EntryWriter.cs`
- `backend/src/Operum.Service/Integrations/Intervals/IntervalsClient.cs`, `IntervalsWellnessDto.cs`
- `backend/src/Operum.Service/Services/Integrations/IntegrationsService.cs`, `IntegrationSyncService.cs`
- `backend/src/Operum.API/Controllers/IntegrationsController.cs`
- `backend/src/Operum.API/Filters/RequiresIntegrationsAttribute.cs`
- EF migration `20260824xxxxxx_AddIntegrations.cs`
- `frontend/src/features/integrations/*`

**Modified**
- `backend/src/Operum.Model/Models/Entry.cs` (+ `Source`, `ExternalId`)
- `backend/src/Operum.Model/OperumContext.cs`
- `backend/src/Operum.Model/Configuration/FeatureSettings.cs` (+ `Integrations`)
- `backend/src/Operum.API/Configuration/ServiceConfiguration.cs`
- `.env.example`, `docker-compose.yml` (Data Protection key volume, `Features__Integrations`, `VITE_REACT_INTEGRATIONS_ENABLED`)
- `README.md`

---

## Build Order

1. `EntryWriter` + `Entry.Source`/`ExternalId` + migration. Unit-testable with no HTTP and no external service; everything else depends on it.
2. `IntervalsClient` + catalog. Verifiable against a real API key in isolation.
3. `IntegrationsService` + controller + DTOs.
4. `IntegrationSyncService`.
5. Frontend.

Steps 1 and 2 are independent and can be built in either order.

---

## Verification

1. **Migration** — apply to a dev DB with existing entries; confirm they keep null `Source`/`ExternalId` and the filtered unique index permits many such rows.
2. **Upsert idempotency** — sync the same date range twice. Second run reports `Updated`, not `Created`; entry count unchanged.
3. **Revision handling** — sync a day, edit that day's `restingHR` in intervals.icu, sync again inside the reconciliation window. The existing entry updates; no duplicate.
4. **Null handling** — an athlete with no HRV logged. With `SkipWhenNull = true` the field stays empty and a `Count`/`Average` analytic ignores it. Flip to `false` and confirm the value is explicitly cleared, not set to 0.
5. **Timespan coercion** — map `sleepSecs` to a `timespan` field; confirm `28800` becomes `08:00:00` and that a calculated field `{Sleep.hours}` evaluates to 8.
6. **Calculated fields** — a tracker with a calculated field over two mapped sources recomputes on sync, both on insert and on update.
7. **Type rejection** — attempt to map `comments` (string) to a `number` field: rejected at save with a clear message, not at sync time.
8. **Required-field guard** — attempt to map a nullable source to a `Required` field with `SkipWhenNull = true`: rejected at mapping time.
9. **Field cap** — a tracker with 24 fields accepts one more mapping and blocks the 26th with a message naming `MaxFieldCount`.
10. **Entry cap** — a tracker at 999 entries syncing 10 new days: writes 1, reports the cap clearly, and **still applies updates** to existing entries on the next tick.
11. **Bad credential** — revoke the key mid-life. Target records `LastSyncStatus = Error` with a readable message, other targets in the same tick sync normally, and no entries are lost.
12. **Credential secrecy** — `GET /api/integrations` returns only the masked suffix. Confirm the raw key appears in no response, no log line, and no Swagger example.
13. **Restart durability** — restart the API container and confirm stored credentials still decrypt (this is the Data Protection key-persistence check; it fails loudly if the volume is missing).
14. **Feature flag off** — endpoints answer 404, the hosted service never starts, the frontend route is absent.
15. **Authorization** — user A cannot create a target against user B's tracker; a collaborator with `CanEditData` but not ownership is handled per the decision below.

---

## Open Questions

1. **Collaborator permissions.** Can a collaborator with `CanEditData` attach their own intervals connection to someone else's tracker? Simplest v1 answer: owner-only, matching how tracker metadata and collaborator management are already owner-only.
2. **Reconciliation window length.** 7 days is a guess. Longer costs API calls; shorter risks missing late device syncs.
3. **Activities resource.** The schema supports it via a second `IntegrationTarget`, but the payload is much wider than wellness and would need its own catalog. Out of scope here; worth confirming the target model holds up before building it.
4. **Sync history.** No `IntegrationSyncRun` table in v1 — only last-status on the target. Add one if debugging user reports proves painful.
