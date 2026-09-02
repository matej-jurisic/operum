# Integrations — Implementation Plan

Supersedes `intervals-integration-plan.md`, which was written for intervals.icu alone.

## Context

Operum has no inbound data path except the authenticated UI (manual entry + CSV upload). This plan builds that path, generalized across providers from the start rather than retrofitted after the first one.

Two providers are in scope, chosen because they stress opposite ends of the design:

- **intervals.icu** — cloud-hosted, self-serve API key, **pull**. A daily wellness snapshot keyed by date, flat schema, revised in place. It already fans in from Garmin, Strava, Wahoo, Zwift, Polar and Coros, so one connector covers most vendors.
- **Firefly III** — self-hosted, per-user instance, **push** (webhooks). Transactions are events, not snapshots; one API object fans out to several entries; deletions are real.

Building both against one abstraction is the point. If the abstraction only fits intervals.icu, it isn't one.

### Decisions

- **Explicit user-defined mapping.** The user picks which source field maps to which tracker field. No auto-creation of fields, no name-guessing. Type compatibility validated at mapping time against `DataTypes`.
- **No transform DSL.** Providers do mechanical coercion only (`sleepSecs` seconds → `timespan`). Every derivation is left to existing calculated fields and `{FieldName}` formulas — a second expression language would overlap badly with the one that already exists.
- **Null is skipped, not zeroed.** Real records return `null` for unlogged metrics. Writing `0` would silently poison averages and charts.
- **Upsert on a provider-stable id, not append.** Re-ingesting must update, not duplicate.
- **Read-only.** Data flows provider → Operum. No write-back in either provider.
- **No currency type.** Deferred deliberately. A Firefly amount is a `number` field plus a
  separate `string` field for the currency code, mapped by hand — fine for a single-currency
  ledger, lossy otherwise. Revisit only if multi-currency users ask; see the open question.
- **Feature-flagged.** Ships dark behind `Features:Integrations`, mirroring how notifications shipped.
- **Caps are raised, not worked around.** See [Data limits](#data-limits) — this is prerequisite work, not a follow-up.

---

## The common pipeline

Every provider, push or pull, converges on the same three stages. Only stage 1 differs.

```
  ingest  ──────────────►  project  ──────────────►  write
  (per provider)           (shared)                  (shared)

  pull:  HTTP fetch        SourceRecord              EntryWriter
         windowed by       + target mappings         upsert / delete
         cursor            → values by FieldId       on (TrackerId,
                                                       Source, ExternalId)
  push:  webhook POST
         verified,
         parsed
```

The unit that crosses both seams:

```csharp
public enum SourceOperation { Upsert, Delete }

/// One provider record, already coerced to the strings SetFieldValue consumes.
/// ExternalId is the provider's stable id for this row -- NOT necessarily a date.
public sealed record SourceRecord(
    string ExternalId,
    SourceOperation Operation,
    DateTime? UpdatedAt,
    IReadOnlyDictionary<string, string?> ValuesBySourceKey);
```

`ValuesBySourceKey` is `string?` because `FieldValueExtensions.SetFieldValue` (`backend/src/Operum.Model/Extensions/FieldValueExtensions.cs:28`) takes `string?` and already handles every branch of `DataTypes`, returning `false` on an unknown type. That gives a clean two-part seam:

- **the provider** owns source → string (JSON number `28800` → `"08:00:00"`, per its catalog's declared coercion)
- **`SetFieldValue`** owns string → typed column

Neither reimplements the other.

---

## Provider abstraction

**Built.** Contracts in `backend/src/Operum.Model/Integrations/`, interfaces and registry in `backend/src/Operum.Service/Integrations/`, the two pure stages in `backend/src/Operum.Service/Domain/Integrations/`.

`IIntegrationProvider` carries what every provider has — `Key`, `DisplayName`, `Capabilities` (`[Flags] Pull | Push`), `RequiresBaseUrl`, `ResourceTypes`, `Catalog(resourceType)`. Pull and push are then separate interfaces so a provider implements only what it does:

- `IPullIntegrationProvider` — `ValidateCredentialAsync(connection, ct)` and `FetchAsync(connection, resourceType, window, ct)`. Fetch returns `IAsyncEnumerable<SourceRecord>` so a paginated source streams instead of materialising a backfill; intervals.icu will yield one page, a Firefly backfill many.
- `IPushIntegrationProvider` — `VerifyAndParse(resourceType, secret, rawBody, headers)`, returning `Result<IReadOnlyList<SourceRecord>>`. Forbidden on a bad signature, with nothing applied.

Four decisions worth recording, each a departure from the sketch this section replaced:

- **`SourceField` has no `Path`.** The provider reads its own payload, so how a key is resolved — a flat JSON key, a path into a nested split — stays inside the provider. `Key` is an opaque stable id that saved mappings store, and the catalog carries only what mapping and the UI need: `Key`, `Type`, `Label`, optional `Description`. No `Coercion` either: providers emit strings, so unit conversion happens before the value ever leaves them.
- **Webhook headers are `IReadOnlyDictionary<string, string>`, not `IHeaderDictionary`.** Keeps providers testable without a request; the controller adapts. `rawBody` stays a string because signatures are over exact bytes.
- **`FieldMapping` is a plain record, not the EF entity.** The stored `IntegrationFieldMapping` projects onto it, which is what lets the projector and the validator be pure and tested with no database at all.
- **The registry throws on a duplicate key.** Two providers under one key would make which one runs depend on registration order, and the loser's stored connections would quietly start syncing from somewhere else. Better to fail at startup.

**Registration.** `ServiceConfiguration.RegisterIntegrationProviders` — providers are singletons (they hold a catalog and parsing logic, nothing per-request; everything per-call arrives in the `ProviderConnection` argument). Adding a provider is one class, one catalog, one line there.

### The projector

`SourceRecordProjector` applies a target's mappings to turn a `SourceRecord` into an `EntryWriteRecord`. It is where `SkipWhenNull` is resolved and the only place that knows the rule:

| Provider said | `SkipWhenNull` | Projector emits | Writer does |
|---|---|---|---|
| a value | either | the key, with the value | writes it |
| explicit null | `true` | nothing | leaves the field as it was |
| explicit null | `false` | the key, with null | clears the field |
| nothing at all | either | nothing | leaves the field as it was |

The last row is the distinction that matters: a provider saying nothing about a key is not the same as it saying the value is empty, and only the second is what `SkipWhenNull` is about.

### The validator

`MappingValidator.Validate(mappings, catalog, trackerFields)` returns null or the first problem, matching the `string?` convention the constants validator already uses. It rejects: an unknown source key, a field from another tracker, a calculated target, the same field mapped twice, an incompatible type pair, and an empty mapping set.

One rule is worth calling out: **a `Required` field mapped with `SkipWhenNull = true` is refused.** With skipping on, the projector omits the key whenever the provider reports nothing, and the writer refuses to create an entry missing a required field — so every record without that metric would be dropped, silently and forever. Clearing instead (`SkipWhenNull = false`) is the only coherent pairing, and the message says so.

Type compatibility is exact except for two pairs: date/datetime (one storage column, already interchangeable everywhere else in the app) and timespan into number (raw seconds, for anyone who would rather do arithmetic on a plain number).

---

## Backend schema

**`Integration`** — one per user per provider account.
- `Id`, `UserId` (FK, Cascade), `Provider`, `ExternalAccountId` (nullable — push-only providers may not have one)
- `BaseUrl` (nullable) — **required for self-hosted providers**, unused by intervals.icu. Present from the first migration so adding a self-hosted provider is not a schema change.
- `CredentialCiphertext` (nullable) — encrypted at rest; null for webhook-only connections that need no outbound credential
- `IsEnabled`, `CreatedAt`
- Unique index `(UserId, Provider, ExternalAccountId)`
- 1:n → `IntegrationTarget`

**`IntegrationTarget`** — one per (connection, tracker, resource).
- `Id`, `IntegrationId` (FK, Cascade), `TrackerId` (FK, Cascade)
- `ResourceType` (`"wellness"`, `"transactions"`)
- `Mode` (`Pull | Push`) — which ingest path drives this target
- `IsEnabled`, `BackfillFrom` (DateOnly), `LastSyncedAt`, `LastSyncStatus` (`Ok | Error | Never`), `LastSyncError`, `LastCursor` (UTC, nullable)
- `WebhookToken` (nullable, unique) — the unguessable path segment for push targets
- `WebhookSecret` (nullable, encrypted) — shared secret for signature verification
- Unique index `(IntegrationId, TrackerId, ResourceType)`
- 1:n → `IntegrationFieldMapping`

**`IntegrationFieldMapping`**
- `Id`, `TargetId` (FK, Cascade), `SourceKey`, `FieldId` (FK, Cascade)
- `SkipWhenNull` (bool, default `true`)
- Unique index `(TargetId, FieldId)` — a tracker field cannot be fed by two sources.

**`Entry`** — add `Source` (nullable) and `ExternalId` (nullable).
- Unique filtered index `(TrackerId, Source, ExternalId)` where `Source IS NOT NULL`.
- The idempotency key. Manually created entries leave both null and are unaffected.

### Migration

`AddIntegrations`:
1. Add `Entries.Source`, `Entries.ExternalId` + the filtered unique index.
2. Create `Integrations`, `IntegrationTargets`, `IntegrationFieldMappings`.
3. Add the `FieldValues` composite indexes from [Data limits](#data-limits).

No data migration — existing entries keep null source.

---

## EntryWriter — the shared write path

**Built.** `backend/src/Operum.Service/Services/Entries/EntryWriter.cs`, interface in `Interfaces/IEntryWriter.cs`, types in `backend/src/Operum.Model/Common/EntryWrite.cs`.

Why it exists: `EntriesService.CreateEntry` (`backend/src/Operum.Service/Services/Entries/EntriesService.cs:26`) opens with `currentUserService.GetCurrentUser()`, and `CurrentUserService` reads `IHttpContextAccessor`. A `BackgroundService` has no HTTP context, so the sync loop **cannot call `EntriesService`**. The same applies to `ImportEntriesFromCsv`. A webhook request does have an HTTP context, but no authenticated user — same problem.

```csharp
Task<EntryWriteResult> ApplyAsync(
    string trackerId,
    string source,
    IReadOnlyList<EntryWriteRecord> records,
    List<Field> fields,
    TimeZoneInfo timeZone,
    CancellationToken ct = default);
```

Responsibilities — and nothing else (no authorization, no current user):

1. Deduplicate the batch by external id, last one wins; two revisions of one record in one payload must not both insert.
2. Load existing entries for `(trackerId, source, externalId in batch)` with their `FieldValues`, **tracked** — the context is no-tracking by default, which also skips identity resolution.
3. `Delete` → remove the matching entry; a miss is a no-op, not an error.
4. `Upsert` → reuse the existing entry or create one.
5. Coerce via `fieldValue.SetFieldValue(field, value)`; a parse failure is reported and the rest of the record still writes.
6. Enforce `DataLimits.MaxEntryCount` against **new** entries only — updates to existing rows are never blocked by the cap.
7. Enforce `Required` on create only; an update inherits what the entry already holds.
8. `SaveChangesAsync`, then `EvaluateAndPersistCalculatedFields(...)` per touched entry, wrapped so one bad formula cannot cost the batch.

### Two contracts worth knowing

**Presence is the instruction.** In `ValuesByFieldId`, a key that is present is written and writing `null` clears that field; a key that is absent leaves the field untouched. That is what lets a mapping's `SkipWhenNull` be resolved entirely by the projector — omit the key to skip, include it with `null` to clear — without the writer knowing the rule.

**The timezone is explicit.** `FormulaEvaluationService` resolved constants using `currentUserService.GetCurrentUserTimeZone()`, which returns UTC rather than throwing when there is no HTTP context — so a background sync would have silently evaluated every date-based constant condition in the wrong zone. `EvaluateAndPersistCalculatedFields` now takes an optional `TimeZoneInfo`, defaulting to the ambient user for request callers; `IEntryWriter` requires one, because it has no ambient user to fall back to.

**Not transactional.** The `(tracker, source, external id)` key makes a batch idempotent instead, so a partially applied batch is repaired by running it again rather than rolled back.

Authorization stays where it belongs: the API layer checks it for user-driven calls; the sync service resolves permission from `Integration.UserId` → tracker ownership; the webhook endpoint authenticates by token + signature, not by user.

**Follow-up (not blocking v1):** point `CreateEntry` and `ImportEntriesFromCsv` at `EntryWriter` too. Until they do, CSV import and integration import can drift in their coercion behaviour — the exact failure mode this extraction exists to prevent.

---

## Ingest: the pull path

**Built**, and split in two rather than one class as sketched:

- **`IntegrationSyncExecutor`** (scoped) runs one target: resolve provider, decrypt credential, compute the window, stream, cursor-filter, project, write in batches, record the outcome.
- **`IntegrationSyncService`** (hosted) is only the timer: `PeriodicTimer`, a scope per target, failures contained per item — the same structure as `NotificationEvaluatorService`.

The split exists so a "sync now" endpoint drives exactly the code the scheduled tick does, instead of a second implementation that can drift from it.

Per target:

1. Refuse a push-mode target, an uninstalled provider, or a tracker no longer owned by the connection's user — each recorded on the target, none thrown.
2. Window: `LastSyncedAt == null` → `BackfillFrom`..today; otherwise `today - ReconciliationDays`..today (default 7, so late device syncs and edits to past days are picked up).
3. Stream `FetchAsync`, skipping any record whose `UpdatedAt <= LastCursor` — nothing changed upstream, so there is no reason to write it again. A record with no `UpdatedAt` is always considered fresh.
4. Project through the target's mappings, writing every `BatchSize` records so a long backfill does not build one enormous change set.
5. Update `LastSyncedAt`, `LastCursor` (max `UpdatedAt` seen), `LastSyncStatus`, `LastSyncError`.

Two things worth knowing:

**Failure messages are deliberately shallow.** An exception message can carry a URL with a credential in it, so only a short summary reaches `LastSyncError` — the detail stays in the log. There is a test asserting a secret in an exception message does not reach the stored error.

**Cancellation is not a failure.** An `OperationCanceledException` during shutdown rethrows without touching the target's status, so the next tick resumes rather than finding a spurious error recorded.

### Credentials

`CredentialProtector` wraps Data Protection under its own purpose string, returns null rather than throwing on ciphertext it cannot read (nearly always a key ring that did not survive a restart — surfaced as a connection to remake), and exposes `Mask` for showing a suffix without the value.

`DataProtection__KeyPath` is in `.env.example` and `docker-compose.yml` mounts `backend-dataprotection` at it. **Without that volume the key ring is regenerated on every restart and every stored credential becomes undecryptable.**

---

## Ingest: the push path

**Built.** `POST /api/integrations/webhooks/{provider}/{token}` on `IntegrationsController`, `[AllowAnonymous]`, `[RequestSizeLimit(1_000_000)]`, rate-limited with every other route, backed by `IntegrationWebhookReceiver`.

One decision worth recording: **a failed signature does not touch the target's sync status.** A forged delivery is not the user's problem, and letting it write there would let anyone holding the URL fill their status with noise. Only genuine failures — an unreadable secret, an apply that threw — are recorded.

Why this matters for Firefly III specifically: the instance is self-hosted and usually behind NAT. A webhook is an **outbound** call from their box, so it works without the user exposing their finance server to the internet — and it removes the SSRF surface a user-supplied `BaseUrl` would otherwise create. Push-only is therefore the *default* mode for Firefly, with pull-backfill an opt-in extra for users who can expose an endpoint.

Flow:

1. Look up the target by `WebhookToken`. Unknown token → 404 (never confirm which half is wrong).
2. Read the **raw** body before model binding — the signature is computed over exact bytes. Use `EnableBuffering()` or a raw-body binder; a re-serialized DTO will not hash the same.
3. `provider.VerifyAndParse(target, rawBody, headers)`. Bad signature → 403, nothing written.
4. Project through mappings, hand to `EntryWriter`.
5. Update `LastSyncedAt` / status on the target so the UI shows push liveness the same way it shows pull.

Hardening:
- **Body size cap** on the endpoint — it's unauthenticated.
- **Replay window**: reject a signature timestamp older than ~5 minutes.
- **Constant-time comparison** of the signature digest.
- The endpoint does the minimum inline; if payloads get large, queue and return 202.

---

## Provider: intervals.icu (pull)

**Built.** `backend/src/Operum.Service/Integrations/Intervals/IntervalsProvider.cs`, catalog in `backend/src/Operum.Model/Constants/Integrations/IntervalsWellnessCatalog.cs`.

Both facts this plan flagged as remembered rather than checked are now **verified against the official API guide**:

- **Auth is HTTP Basic, username the literal `API_KEY`, password the athlete's key.** (`curl -u API_KEY:<key>`.) Third-party pages show an `Authorization: ApiKey API_KEY:<key>` form; the official guide shows Basic, which is what is implemented.
- **Route is `GET /api/v1/athlete/{athleteId}/wellness?oldest=YYYY-MM-DD&newest=YYYY-MM-DD`.** A date range, not pagination, so the whole window arrives in one response.
- Bonus: **athlete id `0` resolves to whoever the key belongs to**, so a user never has to find their own id. Used for credential validation, and as the fallback when a connection has no resolved account.

Design decisions taken while building:

- **The payload is read as `Dictionary<string, JsonElement>`, not a typed DTO.** The catalog is already the schema, so a DTO would restate ~45 nullable properties and a wrong property name would be a code change rather than a one-line catalog fix. It also keeps "absent" and "null" distinguishable without hand-writing every value type as nullable — which was the reason the DTO was specified as fully nullable in the first place.
- **Keys match with case and underscores ignored**, so a payload spelling a field `sleep_secs` resolves the same catalog entry as one spelling it `sleepSecs`. Both spellings circulate in public examples and I could not confirm which this endpoint returns without a live key; this makes the question moot rather than betting on one.
- **Every catalog key is emitted on every record, present with null where nothing was logged.** A wellness record is a complete daily snapshot, so absence means "not logged" — and presence is what a mapping's `SkipWhenNull` acts on.
- **A record with no `id` is dropped**, since without it there is no idempotency key and the record cannot be written safely.
- **A value of an unexpected shape reads as null rather than throwing**, so one odd field cannot cost the whole record.
- **Non-success responses throw**, deliberately: the executor catches them and records the failure against the target, which is the thing that knows which target it is.
- `ExternalId` = `id` (the record date). `UpdatedAt` = `updated`, which drives the cursor and is the one catalog entry not offered for mapping.
- Emits `Upsert` only. Wellness records are never deleted.
- Registered as a **named** HttpClient, with the provider taking `IHttpClientFactory`. A typed client (`AddHttpClient<T>`) would make the provider transient while the registry holds it as a singleton — a captive dependency that pins one handler for the life of the process.

### Source catalog

Fixed schema, so a hardcoded typed catalog. Lives in `backend/src/Operum.Model/Constants/Integrations/IntervalsWellnessCatalog.cs`, same shape as `DataTypes`/`OperatorTypes`.

| Source key | Operum type | Notes |
|---|---|---|
| `id` | `date` | Record key. Always mapped, not user-selectable. |
| `updated` | `datetime` | Change cursor. |
| `sleepSecs` | `timespan` | Integer seconds — `SecondsToTimeSpan` coercion. |
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
| `sportInfo` | — | **Excluded.** Nested array (`type`/`eftp`/`wPrime`/`pMax`); fields are flat scalars. Revisit by flattening per sport type (`eFTP_Ride`, `eFTP_Run`). |

### Source catalog — activities

**Built.** `backend/src/Operum.Model/Constants/Integrations/IntervalsActivitiesCatalog.cs`. Same resource abstraction the plan reserved room for — a second `ResourceType` on the same provider, no schema change.

Route is `GET /api/v1/athlete/{athleteId}/activities?oldest=YYYY-MM-DD&newest=YYYY-MM-DD` — a date range returning `Activity[]` in one response, structurally identical to wellness. `IntervalsProvider.ResourceSpec` captures the three differences (route path, record key, cursor key) and everything else is shared.

Decisions taken while building:

- **A curated ~27-field endurance subset**, not the ~180-field payload. Verified against the official OpenAPI spec (`GET /api/v1/docs`). Covers: start time, sport, name/description, distance, moving/elapsed time, elevation gain, avg/max speed, avg cadence, avg/max HR, avg + weighted-avg power, calories, carbs, training load, intensity, TRIMP, efficiency factor, decoupling, polarization index, CTL/ATL, feel, RPE. Adding one later is a single line in the catalog.
- **`ExternalId` = the activity `id`** (e.g. `i7712345`), an opaque string — *not* a date, since there are many activities per day. It is the external id only, not offered for mapping. `start_date_local` is the mappable "when".
- **No sync cursor.** Activities carry no `updated` field (`created`, `icu_sync_date`, `analyzed` all mean something narrower). `UpdatedAt` is always null, so every record reads as fresh and the reconciliation window bounds the re-read — cheap, since it is a handful of activities per week.
- **`moving_time` / `elapsed_time` are integer seconds** → `timespan`, the same `SecondsToTimeSpan` coercion as `sleepSecs`.
- **Deletions are not handled.** Wellness emits `Upsert` only and activities do the same; an activity deleted upstream leaves a stale entry. Group reconciliation (the Firefly mechanism) is available if this proves to matter.
- **One resource, `type` as a mappable string field** — a `Ride`/`Run`/`Swim` split is a per-sport concern left to later, mirroring how Firefly handles `withdrawal`/`deposit`/`transfer`.

Tests: `IntervalsProviderTests` covers the activities route, the opaque-id external id, null cursor, seconds→timespan, the training subset, and the drop-without-id rule.

---

## Provider: Firefly III (push)

**Built.** `backend/src/Operum.Service/Integrations/Firefly/`, catalog in `backend/src/Operum.Model/Constants/Integrations/FireflyTransactionCatalog.cs`.

### Webhook contract

Configured by the user in their own Firefly instance: trigger `STORE_TRANSACTION` / `UPDATE_TRANSACTION` / `DESTROY_TRANSACTION`, response `TRANSACTIONS`, URL = the Operum webhook URL, plus the secret Operum generated.

Signature scheme **verified** against a working implementation:

- Header `Signature: t=<unix timestamp>,v1=<hex digest>`
- Signed string is `<timestamp>.<raw JSON body>` — literal dot, exact bytes
- **HMAC-SHA3-256**, secret as the key, lowercase hex

**The SHA-3 question is settled, and not the way the plan guessed.** .NET 8+ *does* have `HMACSHA3_256` — but it wraps the platform's crypto library and reports `IsSupported == false` on Windows before 24H2. Checked on this machine: false. Relying on it would mean signature checks that pass in a Linux container and throw on a developer's machine. BouncyCastle is used instead, one implementation everywhere — and it costs no new assembly, because `WebPush` already brings `Portable.BouncyCastle` in. It is now referenced explicitly rather than relied on transitively.

Also implemented: a 5-minute replay window on the timestamp, fixed-time digest comparison, and tolerance for unknown scheme parts so a future `v2` alongside `v1` will not break `v1` senders. Every verification failure returns the same message — which part was wrong is not something an unauthenticated caller should learn.

Known wrinkle, unchanged: `DESTROY_TRANSACTION` has [historically not fired reliably](https://github.com/firefly-iii/firefly-iii/issues/6589). Deletions are best-effort.

### Fan-out, and the general mechanism it needed

A Firefly transaction is a **group** containing splits, each with its own `transaction_journal_id`. One payload produces *N* records, keyed on the journal id — **the split, not the group**, since keying on the group would collapse a split transaction into one entry and lose money.

The harder half is a group edit that *removes* a split: that split's entry has to go, and the provider cannot know what is already stored. This was solved generally rather than in Firefly-specific code:

- `Entry.ExternalGroupId` — the provider's id for the parent record an entry came from.
- `SourceRecord.GroupId` / `EntryWriteRecord.GroupId`, carried through the projector.
- `EntryWriter.ReconcileGroups` — for every group in a batch, delete stored entries under that group whose id is not in the batch.

Setting `GroupId` is a **promise that the batch carries every current child of that parent**. That holds for a webhook payload, which contains the whole group; it would not hold for a page of a paginated pull, which is why the field is opt-in per record rather than inferred.

The mechanism is provider-agnostic: any provider whose records nest gets the same behaviour for free.

### Money, and the sign convention

Firefly reports every amount as positive with a `type` discriminator. The provider emits **signed** amounts — withdrawals negative, deposits positive — because `Sum` over a mixed column is the obvious analytic and only works if the sign is real. **Transfers are left positive**: they move money between the user's own accounts, so they are neither income nor expense and signing them either way would distort a total.

Amounts arrive as JSON *strings*, and are parsed invariantly rather than by server locale.

**Money still has no type in Operum.** An amount is a `number` field plus a mapped `string` currency code — the deferred decision, restated here where it bites.

### Catalog (transactions)

Paths are into `attributes.transactions[i]` unless noted.

| Source key | Path | Type | Notes |
|---|---|---|---|
| `journal_id` | `transaction_journal_id` | `string` | Record key. Always mapped. |
| `group_id` | `data.id` | `string` | Needed for split reconciliation. |
| `date` | `date` | `datetime` | |
| `amount` | `amount` | `number` | String in the payload — parse invariant. |
| `currency_code` | `currency_code` | `string` | See below. |
| `type` | `type` | `string` | `withdrawal` / `deposit` / `transfer`. |
| `description` | `description` | `string` | |
| `category_name` | `category_name` | `string` | |
| `budget_name` | `budget_name` | `string` | |
| `source_name`, `destination_name` | ditto | `string` | Account names. |
| `notes` | `notes` | `string` | |
| `tags` | `tags` | `string` | Array → joined; a list has no home in `DataTypes`. |
| `foreign_amount`, `foreign_currency_code` | ditto | `number` / `string` | |

**Money has no type in Operum.** `DataTypes` is string/number/date/datetime/timespan/bool. An amount is a `number` field plus a separate `string` field for the currency, and the user has to map both. That is acceptable for a single-currency ledger and lossy for a multi-currency one. A `currency` data type is a larger change than this plan; flagging it rather than smuggling it in.

**Sign convention.** Firefly reports amounts as positive with a `type` discriminator. Decide once, in the provider, whether a `withdrawal` is emitted negative — and document it in the catalog label, because a user summing the column will otherwise get a wrong answer with no visible cause. Recommendation: emit signed (`withdrawal` → negative), since `Sum` over a mixed column is the obvious analytic and it only works if the sign is real.

---

## Data limits

The caps are being raised. What they cost is the indexing, not the numbers.

Current: `MaxEntryCount = 1000`, `MaxFieldCount = 25` (`backend/src/Operum.Model/Constants/DataLimits.cs`). The intervals catalog alone is ~45 entries, and a finance ledger passes 1000 transactions inside a year.

The constraint is that entries are EAV. Every view filter compiles to a correlated `EXISTS` over `FieldValues`:

```csharp
query.Where(e => e.FieldValues.Any(fv => fv.FieldId == fieldId && fv.NumberValue > value))
```

(`backend/src/Operum.Service/Domain/Views/ViewQueryBuilder.cs:200-205`, and the same shape for every other type.) With `MaxFilters = 6` that is up to six correlated subqueries per page load.

`FieldValues` currently has only the two single-column FK indexes EF generated — `IX_FieldValues_EntryId` and `IX_FieldValues_FieldId` (`backend/src/Operum.Model/Migrations/20250715213244_AddEntries.cs:90-98`). At today's ceiling (1000 × 25 = 25k rows/tracker) that is fine. At 25,000 entries it is 625k rows per tracker and those subqueries will be the page-load cost.

**Do before raising the cap:**

1. Composite index `(EntryId, FieldId)` — serves the correlated lookup directly.
2. Per-type composites `(FieldId, NumberValue)`, `(FieldId, DateTimeValue)`, `(FieldId, BooleanValue)`, `(FieldId, TimeSpanValue)` for the selective direction. `(FieldId, StringValue)` helps equality/prefix but not `Contains` — a trigram index if that proves hot.
3. Check `ApplyViewSorting` too: a correlated subquery inside `OrderBy` over a large table is worse than one inside `Where`, because it cannot short-circuit.
4. **Measure before and after** on a seeded tracker at the proposed ceiling. The numbers below are a starting proposal, not a finding.

**Proposed:** `MaxFieldCount` 25 → 60 (covers the intervals catalog with room), `MaxEntryCount` 1000 → 25,000. `MaxWidgetCount`, `MaxViewCount` etc. are unaffected — they are UI-surface caps, not data volume.

`BackfillFrom` still defaults to something modest (12 months) rather than "all history", and the UI still says plainly what happens at the cap. A higher ceiling is not an absent one.

---

## Credential storage

ASP.NET Core Data Protection — already available, no new dependency:

```csharp
services.AddDataProtection().PersistKeysToDbContext<OperumContext>(); // or a mounted volume
var protector = provider.CreateProtector("Operum.Integrations.Credentials");
```

Keys must persist across container restarts or every stored credential becomes undecryptable. `.env` gets `DataProtection__KeyPath`, docker-compose gets a volume. Applies to `CredentialCiphertext` and `WebhookSecret` alike.

Credentials must never appear in a DTO returned to the frontend — expose a masked suffix (`…a91f`) only. The webhook secret is shown **once**, at target creation, and never again.

---

## API surface

**Built.** `backend/src/Operum.API/Controllers/IntegrationsController.cs` over `IntegrationsService`, decorated `[RequiresIntegrations]` — with the flag off every route 404s, as if it never existed.

Decisions taken while building:

- **Owner-only**, settling open question 1. A collaborator with `CanEditData` cannot attach their own connection to someone else's tracker, matching how tracker metadata and collaborator management already work.
- **Another user's connection answers 404, not 403** — a connection that isn't yours should not be confirmed to exist.
- **A target's tracker and resource type are immutable.** Changing either would orphan everything already imported under the old pairing, since the idempotency key is `(tracker, source, external id)`. The API says to delete and remake instead.
- **`Connect` verifies before it stores** for any pull-capable provider, so a bad key is refused where it was typed. A push-only provider has nothing to call, so its connection is made unverified and the first delivery proves it.
- **Base URLs are validated at connect time**: https only, no loopback, and no literal private or link-local address. A hostname that *resolves* to a private address is not caught here — that check belongs at request time with the resolved address in hand, and is noted in the open questions.
- **The webhook secret is returned exactly once**, on the response that creates the target or rotates it. It is stored encrypted and cannot be shown again.
- `MaxIntegrationCount` (10) and `MaxIntegrationTargetCount` (10) added to `DataLimits`, matching how everything else in the app is bounded.

| Method | Route | Purpose |
|---|---|---|
| `GET` | `/api/integrations/providers` | Providers, capabilities, resource types, source catalogs |
| `GET` | `/api/integrations` | Current user's connections (credential masked) |
| `POST` | `/api/integrations` | Connect — validates the credential before storing (pull providers) |
| `DELETE` | `/api/integrations/{id}` | Disconnect |
| `GET` | `/api/integrations/{id}/targets` | Targets + mappings |
| `POST` | `/api/integrations/{id}/targets` | Create target + mappings; returns webhook URL + secret **once** for push targets |
| `PUT` | `/api/integrations/{id}/targets/{targetId}` | Update mappings / backfill / enabled |
| `DELETE` | `/api/integrations/{id}/targets/{targetId}` | Remove target |
| `POST` | `/api/integrations/{id}/targets/{targetId}/sync` | Sync now (pull targets) |
| `POST` | `/api/integrations/{id}/targets/{targetId}/rotate-secret` | New webhook secret |
| `POST` | `/api/integrations/webhooks/{provider}/{token}` | **Anonymous.** Push ingest |

DTOs in `backend/src/Operum.Model/DTOs/Integrations/`, following the `Result<T>` / `ResultStatusCodes` conventions used throughout.

### Mapping validation

Shared across providers, driven by the catalog's declared type:

| Source type | Accepts tracker field types |
|---|---|
| `number` | `number` |
| `timespan` | `timespan`, `number` (raw seconds) |
| `string` | `string` |
| `bool` | `bool` |
| `date` / `datetime` | `date`, `datetime` |

Also reject: a `FieldId` that isn't in the target tracker; a field with `IsCalculated`; a `SourceKey` not in the provider's catalog; and a nullable source onto a `Required` field with `SkipWhenNull = true` — those writes would fail on every record missing that metric.

---

## Frontend

**Built.** `frontend/src/features/integrations/`, matching the existing `features/*` layout.

- **`pages/IntegrationsPage.tsx`** — connection list, empty state, and the orchestration for every dialog.
- **`components/ConnectProviderDialog.tsx`** — the connect flow, driven by the provider's declared capabilities: an API-key field for a pull provider, and for a push-only one an explanation that no key is needed because the instance calls Operum.
- **`components/TargetFormDialog.tsx`** — the mapping editor. Source values on the left with their type, a tracker-field `Select` on the right filtered to type-compatible, non-calculated, not-already-mapped fields, and a per-row keep/clear switch for empty values.
- **`components/WebhookSetupPanel.tsx`** — URL and secret with copy buttons and the literal steps for Firefly's webhook screen, shown once with a warning that the secret cannot be shown again.
- **`components/IntegrationCard.tsx`** — per-target status, relative last-sync time, error text, "Sync now" for pull and "Waiting for first delivery" for push.
- Gated on `VITE_REACT_INTEGRATIONS_ENABLED`, so the route and the sidebar entry do not exist when the feature is off and the frontend never has to ask the backend.

Decisions taken while building:

- **No MobX store.** The plan called for one, but this page holds no state anything else reads — it loads two lists and re-fetches after a mutation. `useState` in the page is the whole requirement; a store would have been ceremony. The existing feature stores exist because sidebars and other pages share their data.
- **The mapping editor lists every source value as a row**, rather than an add-a-mapping button. A provider catalog is a fixed, browsable set, and "which of these do I want?" reads better as a checklist than as a growing list of pairs.
- **Type compatibility is mirrored client-side** so an impossible pairing is simply absent from the dropdown rather than rejected on save. The server still enforces it — this copy is a convenience, and is commented as such.
- **`skipWhenNull` is labelled keep/clear**, not by its field name, and is forced off and disabled for a required field — the pairing the server refuses. The tooltip says why.
- **The webhook secret is surfaced by the page, not the dialog.** `saveTarget` checks the response for a secret and opens the setup panel, since that response is the only place it will ever appear.

### Verification

The Chrome extension was not connected, so **the page has never been rendered**. What was verified: `npm run build` succeeds (the page code-splits into its own chunk), `eslint` is clean on every new file, and the dev server serves the module with `VITE_REACT_INTEGRATIONS_ENABLED: "true"` in `import.meta.env`.

The backend was exercised against **real Postgres** rather than the suite's SQLite: all three migrations applied cleanly on startup, and the anonymous webhook route gives a clean A/B on the feature gate —

| | Response |
|---|---|
| Flag off | `404 "Integrations are disabled"` |
| Flag on, unknown token | `404 "Unknown webhook."` |
| Flag on, unknown provider | `404 "Unknown webhook."` |

The last two being byte-identical is the intended property: a caller learns nothing about which half of the address was wrong.

Note for anyone repeating this: an unauthenticated `GET /api/integrations/providers` returns 401 whether or not the flag is set, because the fallback authorization policy runs before the action filter. It is not a test of the flag. The anonymous webhook route is.

---

## Reuse map

- `backend/src/Operum.Model/Extensions/FieldValueExtensions.cs:28` `SetFieldValue` — string → typed coercion, all six `DataTypes` branches. Providers must not reimplement this.
- `backend/src/Operum.Service/Interfaces/IFormulaEvaluationService.cs` `EvaluateAndPersistCalculatedFields` — calculated fields after every upsert.
- `backend/src/Operum.Service/Services/Notifications/NotificationEvaluatorService.cs:17-32` — `BackgroundService` + `PeriodicTimer` + scoped-per-tick structure to copy.
- `backend/src/Operum.API/Filters/RequiresNotificationsAttribute.cs` — feature-gate filter to copy.
- `backend/src/Operum.API/Configuration/ServiceConfiguration.cs:62-67` — conditional hosted-service registration pattern.
- `backend/src/Operum.Service/Integrations/MailSender/` — folder layout and `IOptions<T>` config binding for an external client.
- `backend/src/Operum.Model/Constants/Fields/DataTypes.cs` — type validation for mappings.
- `backend/src/Operum.Service/Services/Entries/EntriesService.cs:277-448` `ImportEntriesFromCsv` — the required-field validation and batch-insert shape `EntryWriter` generalizes.

---

## Build order

1. ~~**Data limits**: `FieldValues` composite indexes, then raise the caps.~~ **Done.** `MaxFieldCount` 25 → 60, `MaxEntryCount` 1000 → 25,000, with the composite indexes described above. Measuring a seeded tracker at the new ceiling is still outstanding (verification 13).
2. ~~**`EntryWriter`** + `Entry.Source`/`ExternalId` + migration.~~ **Done.** Migration `20260902*_AddIntegrationWritePath`; 12 tests in `Operum.Tests/Tests/Entries/EntryWriterTests.cs`.
3. ~~**Provider abstraction** — interfaces, `SourceRecord`, registry, DI.~~ **Done.** 23 tests in `Operum.Tests/Tests/Integrations/ProviderPipelineTests.cs`, driven by `Mocks/FakeIntegrationProvider.cs` — a provider implementing both ingest paths against no network, so the shared pipeline is proven before a real integration exists and a later regression surfaces here rather than against someone's live account.
4. ~~**intervals.icu provider** + `IntegrationSyncService` (pull path end to end).~~ **Done.** Schema + migration `AddIntegrations`, the provider, `IntegrationSyncExecutor` and the hosted `IntegrationSyncService`, `CredentialProtector`, the `Features:Integrations` flag and `RequiresIntegrationsAttribute`, plus `.env.example` / `docker-compose.yml` config including the Data Protection key volume. 29 tests across `IntervalsProviderTests` and `IntegrationSyncExecutorTests`. A second resource, **activities**, was added later against the same provider — see [Source catalog — activities](#source-catalog--activities).
5. ~~**`IntegrationsService`** + controller + DTOs.~~ **Done.** Connect / target / mapping CRUD, `SyncNow` through the shared executor, masked credentials, webhook provisioning. 25 tests in `IntegrationsApiTests` (plus `IntegrationsDisabledTests` for the flag-off case).
6. ~~**Firefly III provider** + the webhook receive endpoint.~~ **Done.** `FireflySignature`, `FireflyProvider`, `IntegrationWebhookReceiver`, the anonymous `POST /api/integrations/webhooks/{provider}/{token}` route, and the general group-reconciliation mechanism with migration `AddEntryExternalGroupId`. 42 tests across `FireflyProviderTests` and `FireflyWebhookTests`.

   **The abstraction held.** Firefly shares nothing structural with intervals.icu — push not pull, nested not flat, deletions, self-hosted, an entirely different auth model — and everything downstream of `IPushIntegrationProvider.VerifyAndParse` is the same projector and the same writer. The only thing step 3 did not anticipate was group reconciliation, and that went in as a provider-agnostic mechanism rather than a Firefly special case.
7. ~~**Frontend.**~~ **Done.** `frontend/src/features/integrations/`, plus the route in `App.tsx`, the sidebar entry (both nav modes), and `VITE_REACT_INTEGRATIONS_ENABLED` wired through `.env.example`, `docker-compose.yml` and the frontend `Dockerfile`.

---

## Still outstanding

Everything above is built; these are the gaps worth knowing about.

- **Firefly has not run against a real instance.** Its tests use stubbed payloads and a signature computed by the same code that verifies it; its payload shape is the unknown. intervals.icu wellness has been run end to end against a real account (backend and UI); the activities catalog is verified against the official OpenAPI spec but has not itself been synced from a live account.
- **The integrations page has never been rendered.** It builds and lints clean; nobody has looked at it.
- **Verification 13 is unrun** — a 6-filter view timed on a tracker seeded to the new `MaxEntryCount`, before and after the composite indexes. The caps were raised on reasoning about query shapes, not measurement.
- **Open questions 2, 5, 6 and 7** remain as written: reconciliation-window length, no sync-history table, Firefly pull-backfill, and DNS-rebinding on a self-hosted base URL.

Steps 4 and 6 both sit behind step 3 and are otherwise independent.

---

## Verification

**Shared pipeline**
1. **Migration** — apply to a dev DB with existing entries; confirm they keep null `Source`/`ExternalId` and the filtered unique index permits many such rows.
2. **Upsert idempotency** — ingest the same records twice. Second run reports `Updated`, not `Created`; entry count unchanged.
3. **Null handling** — with `SkipWhenNull = true` the field stays empty and a `Count`/`Average` analytic ignores it. Flip to `false` and confirm the value is explicitly cleared, not set to 0.
4. **Calculated fields** — a tracker with a calculated field over two mapped sources recomputes on ingest, both on insert and on update.
5. **Type rejection** — mapping a `string` source to a `number` field is rejected at save with a clear message, not at ingest time.
6. **Required-field guard** — a nullable source onto a `Required` field with `SkipWhenNull = true` is rejected at mapping time.
7. **Field cap** — a tracker one under `MaxFieldCount` accepts one more mapping and blocks the next with a message naming the cap.
8. **Entry cap** — a tracker just under `MaxEntryCount` ingesting 10 new records: writes what fits, reports the cap clearly, and **still applies updates** to existing entries on the next run.
9. **Feature flag off** — endpoints 404 (webhook included), the hosted service never starts, the frontend route is absent.
10. **Authorization** — user A cannot create a target against user B's tracker.
11. **Credential secrecy** — `GET /api/integrations` returns only the masked suffix. No raw credential in any response, log line, or Swagger example.
12. **Restart durability** — restart the API container; stored credentials still decrypt. Fails loudly if the Data Protection volume is missing.
13. **Query performance** — seed a tracker to the new `MaxEntryCount`, then time a 6-filter view before and after the composite indexes.

**Pull (intervals.icu)**
14. **Revision handling** — sync a day, edit that day's `restingHR` upstream, sync again inside the reconciliation window. The existing entry updates; no duplicate.
15. **Timespan coercion** — map `sleepSecs` to a `timespan` field; `28800` becomes `08:00:00` and `{Sleep.hours}` evaluates to 8.
16. **Bad credential** — revoke the key mid-life. That target records `LastSyncStatus = Error` with a readable message, other targets in the same tick sync normally, no entries lost.
17. **Pagination** — a backfill spanning more pages than one; every record arrives exactly once.
17a. **Activities** — connect a tracker to the `activities` resource, map a handful of fields (including `moving_time` → `timespan` and `type` → `string`), sync. Each activity is one entry keyed on its `id`; two activities on one day produce two entries; a re-sync inside the reconciliation window updates rather than duplicates.

**Push (Firefly III)**
18. **Signature verification** — a correct signature is accepted; a tampered body, a wrong secret, and a missing header are each rejected 403 with nothing written.
19. **Replay** — a valid payload with a timestamp outside the window is rejected.
20. **Split fan-out** — a 3-split transaction produces 3 entries with distinct `ExternalId`s and the correct individual amounts.
21. **Split removal** — edit that transaction down to 2 splits; the third entry is deleted, the other two update.
22. **Delete** — `DESTROY_TRANSACTION` removes every entry for that group.
23. **Unknown token** — a webhook POST to a bogus token 404s without disclosing whether the provider or token was wrong.
24. **Sign convention** — a mixed withdrawal/deposit set sums to the expected net in a `Sum` analytic.

---

## Open questions

1. ~~**Collaborator permissions.**~~ Decided: **owner-only**, matching how tracker metadata and collaborator management already work. A collaborator with `CanEditData` cannot attach a connection to someone else's tracker.
2. **Reconciliation window length.** 7 days is a guess. Longer costs API calls; shorter risks missing late device syncs.
3. ~~**SHA-3 dependency.**~~ Settled. .NET's `HMACSHA3_256` exists but is unsupported on Windows before 24H2, so BouncyCastle is used for one consistent implementation — at no new-dependency cost, since `WebPush` already pulls it in.
4. **Currency as a data type.** Decided: not now. An amount is a `number` plus a mapped `string` currency code. If multi-currency ledgers turn out to matter it is a `DataTypes` change reaching into filters, analytics and formulas — its own piece of work, not an amendment to this one.
5. **Sync history.** No `IntegrationSyncRun` table in v1 — only last-status on the target. Add one if debugging user reports proves painful.
6. **Firefly pull-backfill.** Push-only means no history before connection. Adding optional pull requires `BaseUrl` (already in the schema) and brings the SSRF surface back. Worth it only if users ask.
7. **DNS-rebinding on a self-hosted base URL.** `IntegrationsService.ValidateBaseUrl` rejects https-less schemes, loopback, and literal private addresses — but a hostname that *resolves* to one gets through, and could change its answer between validation and use. Closing that means checking the resolved address at connect time and refusing redirects into private ranges on every request, most cleanly as a `DelegatingHandler` on the self-hosted provider's client. Not needed until a self-hosted provider actually pulls; intervals.icu has a fixed host.
