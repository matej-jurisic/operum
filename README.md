# Operum

Available at operum.app

Flexible data tracking. Define your schema, log entries, build views, assemble dashboards, and visualize trends, without wrestling with spreadsheets.

---

## Features

### Trackers and fields

A tracker is a container for one kind of data: a reading list, workout log, bug database, whatever. You define its structure with fields, then start logging entries. Each tracker has a name, color, and icon. Create one field by field, step through the guided wizard, or clone a published template.

Supported field types: `string`, `number`, `bool`, `date`, `datetime`, `timespan`, `reference`. Each field has a name, an optional description, can be marked required, and `string` fields can carry a fixed list of select options. Which fields show, and in what order, is decided per view.

**Reference fields** — a `reference` field links each entry to one entry in another tracker, picked from a search box. Configure which tracker it points at and which of that tracker's fields to show as the label. The label is kept in sync when the linked entry changes, and the cell clears if the linked entry is deleted. Filtering a view by a reference field matches on that label.

**Extract to a new tracker** — select one or more of a tracker's fields and pull them out into a new tracker. The new tracker gets a copy of each field and one row per distinct combination of their values, and the original tracker keeps a single reference field in their place, linked to the matching row on every existing entry. Useful for normalizing imported or already-logged data where the same values repeat across rows.

**Calculated fields** — any `number`, `bool`, or `timespan` field can be marked as calculated. Write a formula using `{FieldName}` syntax and Operum evaluates it automatically on every create or update. TimeSpan fields support unit access via `{Field.hours}`, `{Field.minutes}`, and `{Field.seconds}`. Calculated fields are hidden from the entry form. Select a group of entries and force-recalculate them on demand when a formula changes after data already exists.

**Constants** — define named reusable values (`number`, `bool`, `timespan`) and reference them in formulas by name. Each constant can have up to 6 conditional values: each conditional value has a priority and filter conditions, and the evaluator picks the lowest-priority match for the current entry, falling back to the base value if none match. This lets a single constant behave differently depending on what's in the entry.

### Entries

Create, edit, duplicate, and delete entries individually, or select a group to bulk-delete or bulk-recalculate calculated fields. A quick-add dialog captures one entry in a few keystrokes. Import existing data from a CSV file, or export at any time, optionally filtered to a specific view.

### Views

A view is a saved lens on your data: a set of filters, a sort order, and a column selection. Switch between views without losing any configuration. Drag to reorder them, and mark one as the default that opens automatically. Use dynamic value filters to build live views that shift over time, with date values such as `now`, `today`, `start of month`, or `end of year`, each with an offset for any number of days, weeks, months, or years back or ahead.

Views are assembled from reusable queries. A query is a single clause, one filter or one sort, shared across the views that use it, so the same "logged this month" filter can back several views at once.

### Dashboards

Build any number of dashboards ("boards"), each with its own name, color, and icon, and set one as your landing page. A board is a grid of widgets you arrange by dragging; desktop and mobile layouts are kept separately, so a board can be dense on a screen and single-column on a phone, and while arranging you can switch to a phone-width frame to lay out the mobile version without leaving your desk. Any widget can be made expandable for a full-size view, and related widgets can be grouped into a container panel, or a set of tabs, that moves and resizes as one.

| Widget | What it does |
|---|---|
| Chart | A saved chart definition from the Widget Library, placed by reference |
| Entries table | A saved table showing a tracker's most recent rows, with chosen columns |
| Quick-add button | Opens a tracker's quick-add entry dialog straight from the board |
| Filter | Live controls that narrow the chart and table widgets on the board that follow it |
| Header / Divider / Note | Layout and annotation |
| Container | A panel holding a sub-grid of other widgets, so a group can be moved, resized, and titled as one. Nesting is one level deep |
| Tabs container | A container whose body is split into named tabs, each holding its own sub-grid; only the active tab's widgets show |

A board can also be built and edited as JSON. **Edit as JSON** in the board menu opens the whole board as one document, and **Import from JSON** in the New board dialog creates a new board from one. The document lists every widget with its grid placement, display mode, color, text, and wiring (Library widget, sources, filter clauses and links, goal targets), plus the board's filter presets, so a whole dashboard can be written by hand or generated and imported in one step. A save is all or nothing, and every problem found comes back at once with the path it sits at.

- The document has no ids. A widget is named by its `key` (letters, digits, dot, dash, underscore), which it is given the first time its board is exported and keeps from then on. Trackers, fields, views, presets, and tabs are named. Where a name matches more than one, the error asks you to rename one.
- A widget whose `key` is on the board is updated, and any other `key` is a new widget. A widget the document leaves out is deleted, after a confirmation.
- An analytic widget either points at a Widget Library widget by name with `wiring.library` or defines a new one inline with `wiring.widget` and `wiring.sources`, which is added to the Library. An existing widget's Library definition is read-only, and a save that changed it is refused.
- A field you leave out stays as it is, and one set to null is cleared. A new widget can leave out its layout and is placed below the board.
- **Copy tracker schema**, in the same editor, copies every tracker you can access as names only: fields with their types, and views with their columns, filters, and sorts. It carries no entries or ids, so it is safe to paste into a chat or a prompt when generating a board.

```json
{
  "schemaVersion": 3,
  "board": { "name": "Fitness", "color": "grape" },
  "items": [
    { "key": "range", "type": "filter",
      "wiring": { "filter": {
        "clauses": [{ "key": "from", "dataType": "date", "operator": "Greater Than Or Equal", "value": "start_of_month" }],
        "links": [{ "item": "volume", "fields": { "from": "Date" } }] } } },
    { "key": "volume", "type": "analytic", "name": "Volume",
      "wiring": {
        "widget": { "resultType": "Goal", "code": "Sum", "goalTarget": "60" },
        "sources": [{ "trackerName": "Workouts", "fields": ["Value: Duration"] }] } },
    { "key": "log", "type": "entries", "name": "Recent workouts",
      "columns": ["Date", "Duration"], "wiring": { "trackerName": "Workouts" } }
  ]
}
```

Exporting a board shows working values for every field: result types (`Single Value`, `Line Chart`, `Bar Chart`, `Scatter Chart`, `Calendar`, `Donut Chart`, `Goal`), calculations such as `Sum` or `Raw Values`, and source fields written as `Purpose: Field name`. A wrong calculation for a result type comes back with the ones that type takes.

The **Widget Library** holds chart and table definitions independently of any board. Build one once, place it on as many boards as you want, edit or delete it in one place. A filter widget can offer board-level saved filter sets as one-tap presets, and each followed widget picks which of its tracker's fields the filter runs against. Adding a chart or table to a board with existing filter widgets offers a checklist to follow them right away, so the new widget doesn't load unfiltered.

### Analytics

Charts are built in the Widget Library and calculated at query time. Each chart reads from one or more sources; a source is a tracker plus a field mapping, and can be scoped to a fixed tracker view. Line and bar charts can combine several sources onto one shared axis to compare trackers side by side, a calendar unions several trackers' events, and a scatter chart's correlation calculation pairs two trackers on a shared field to plot one against the other; other chart types read from a single tracker.

| Type | Variants | Description |
|---|---|---|
| Single Value | 12 | One headline metric: count, sum, min, max, average, standard deviation, unique count, most/least common, or yes/no counts and percentage. Min and max compare one field but can display another, so the widget leads with what the winning entry holds in that field and shows the compared value beneath it |
| Goal | 9 | A single value calculation shown as progress toward a target you set, with a progress bar and percentage. The target can switch based on the values of the board filters the widget follows |
| Line Chart | grouping x calc | Pick a grouping (none for raw values, exact value, or day/week/month/year) and a calculation (sum, average, count, min, max, or a cumulative running total) independently |
| Bar Chart | grouping x calc | Pick a grouping (none for raw values, exact category, or day/week/month/year) and a calculation (sum, average, count, min, or max) independently |
| Scatter Chart | 2 | Numeric X/Y point cloud for spotting correlations: one tracker's two fields, or two trackers paired on a shared match field |
| Donut Chart | 1 | Groups entries by a category, sums a numeric field per group |
| Calendar | 1 | Maps entries onto a date field, labeled with another field's value |

Each chart only exposes fields that are compatible with the role being configured: a Y-axis only accepts `number` or `timespan`, a calendar date axis only accepts `date` or `datetime`.

### Explore

A scratchpad for calculations that don't need a home on a board. Pick a chart type, grouping, and calculation, map one or more trackers, add inline filter clauses or reuse a saved view, and run it. The whole setup lives in the URL, so a useful exploration can be bookmarked or shared. When a result is worth keeping, promote it in place: save it to the Widget Library, or drop it straight onto a dashboard. Inline filters become a saved view on their tracker on the way.

### Integrations

Pull data in from external services and map their values onto tracker fields. You pick which source field feeds which tracker field, and the type pairing is checked when you save. Imports are read-only and upsert on a provider-stable id, so re-syncing updates rows instead of duplicating them; a missing value is skipped, not written as zero. Re-import from a chosen date at any time to backfill or repair history. Stored credentials are encrypted at rest.

| Provider | Direction | Data |
|---|---|---|
| intervals.icu | Pull on a schedule, connected with an API key | Daily wellness snapshots and activities |
| Firefly III | Push by webhook, self-hosted (needs your instance URL) | Transactions |

Connecting a push provider gives you a webhook URL and a signing secret to paste into that service; the secret is shown once and can be rotated.

### Notifications

Each tracker has a notifications tab for alert rules. When a rule fires it reaches every collaborator on the tracker two ways: a browser push notification and an item in the in-app inbox (the bell in the sidebar), which stays even if the rule is later deleted.

A rule has three parts:

- **Event** — when to check. Either *scheduled* (every N days; every N weeks on chosen weekdays; or a day of the month, with a last-day toggle), each at a set time of day and optionally skipping weekends, or *triggered*, checked on a short evaluation loop and fired the moment the condition crosses from not matching to matching.
- **Value** — what to check. *Entry* mode runs the condition over the tracker's entries; *analytic* mode runs it over a chart's computed single value.
- **Condition** — a list of filter clauses that all have to hold.

Entry-mode rules remember which entries have already triggered, so a scheduled run reports only newly matching entries, and a row can fire again if it stops matching and later matches once more. Scope a rule to a view to narrow the entries it sees. The push body is generated by default, or write your own with `{count}`, `{value}`, `{tracker}`, `{notification}`, and `{fieldValueList}` tokens. Scheduled rules resolve against your account time zone, set on the profile page and captured from the browser on first sign-in.

### Collaboration

Share a tracker with other users by searching for their username. Collaborators are view-only by default. Grant permissions independently:

- **Edit data** — create, edit, and delete entries
- **Edit schema** — manage fields, views, and constants

Permissions can be changed at any time. Tracker metadata, the default view, and collaborator management are owner-only.

### Accounts and administration

Authentication supports email/password with confirmation and Google OAuth. Two roles: **User** and **Admin**. The admin panel adds a platform overview, user management, tracker oversight, and template publishing, admins publish tracker templates that any user can clone as a starting point.

A command palette (`Ctrl`/`Cmd` + `K`) jumps to any tracker or board and runs common actions (new tracker, new board, open Explore, toggle theme) from anywhere.

The profile page holds account settings: username, password, time zone, which tracker or board opens on load, and account deletion.

---

## Setup & Installation

Requires Docker and Docker Compose. Everything the containers need lives in one root `.env` file.

```bash
cp .env.example .env
./setup.ps1        # Windows, fills in .env.example's __GENERATE__ placeholders
./setup.sh          # macOS/Linux, same, run instead of setup.ps1
docker-compose up -d
```

`setup.ps1`/`setup.sh` generate a JWT signing key, DB password, admin login password, and Grafana password, and write them into `.env`. Re-running is safe: it only fills in placeholders it finds, and prints the generated admin login at the end. You can skip the script and fill in `.env` by hand instead; see the comments in `.env.example` for what each value does.

| Service | URL |
|---|---|
| App | http://localhost:3000 |
| API | http://localhost:5000/api |
| Swagger | http://localhost:5000/api/swagger/index.html |
| Grafana | http://localhost:3001 (only with `COMPOSE_PROFILES=monitoring` in `.env`) |

A default admin (`admin@example.com`) and test user (`test@example.com`) are seeded automatically on first run; the setup script prints their passwords.

Key settings in `.env`: `ConnectionStrings__Operum` (built from `POSTGRES_*`), `JwtSettings__Key`, `MailGun__ApiKey`, `AUTHENTICATION__GOOGLE__CLIENTID`, `VITE_REACT_API_URL`.

### Integrations

Integrations ship behind a flag. Set `Features__Integrations=true` and `VITE_REACT_INTEGRATIONS_ENABLED=true` in `.env` (the frontend value is inlined at build time, so the frontend image has to be rebuilt after changing it). With the flag off, the endpoints answer 404 and the sync loop never starts.

`DataProtection__KeyPath` is where credentials are encrypted to. It must survive a container restart; the compose file mounts a volume at `/app/DataProtection-Keys` for it. Lose the key ring and every stored credential has to be reconnected. `Integrations__SyncIntervalMinutes`, `Integrations__ReconciliationDays`, and `Integrations__BatchSize` tune how often pull providers run, how many days back each run re-reads, and how many records are written at a time.

### Notifications

Notifications ship behind a flag. Set `Features__Notifications=true` and `VITE_REACT_NOTIFICATIONS_ENABLED=true` in `.env` (the frontend value is inlined at build time, so the frontend image has to be rebuilt after changing it). With the flag off, the notifications tab and inbox are hidden and the endpoints answer 404.

Web push needs a VAPID keypair. With `Features__Notifications=true`, `setup.ps1`/`setup.sh` fill `Vapid__PublicKey` and `Vapid__PrivateKey` (the `__GENERATE__` placeholders in `.env.example`); otherwise run `npx --yes web-push generate-vapid-keys --json` and paste them in yourself. Set `Vapid__Subject` to a `mailto:` address. `Notifications__EvalIntervalMinutes` sets how often the evaluation loop runs.

### Running natively (without Docker)

For backend hot-reload during development:

```bash
docker-compose up -d postgres          # DB only
./setup.ps1 -Dev    # or ./setup.sh --dev, also writes appsettings.Development.json
cd backend/src/Operum.API && dotnet run
cd frontend && npm install && npm run dev
```

---

## Stack

Backend: .NET 9, ASP.NET Core, Entity Framework Core, PostgreSQL  
Frontend: React 19, TypeScript, Vite, Mantine, MobX  
Infrastructure: Docker, Nginx, Prometheus, Grafana

---

## License

Copyright (C) 2026 Matej Jurišić

Operum is free software licensed under the GNU Affero General Public License v3.0. You may run, study, share, and modify it under the terms of the AGPL-3.0. Because it is an AGPL work, anyone who runs a modified version to provide a network service must also make the modified source available to that service's users. See [LICENSE](LICENSE) for the full text.
