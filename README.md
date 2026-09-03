# Operum

Available at operum.app

Flexible data tracking. Define your schema, log entries, build views, assemble dashboards, and visualize trends, without wrestling with spreadsheets.

---

## Features

### Trackers and fields

A tracker is a container for one kind of data: a reading list, workout log, bug database, whatever. You define its structure with fields, then start logging entries. Each tracker has a name, color, and icon. Create one field by field, step through the guided wizard, or clone a published template.

Supported field types: `string`, `number`, `bool`, `date`, `datetime`, `timespan`. Each field has a name, an optional description, can be marked required, and `string` fields can carry a fixed list of select options. Which fields show, and in what order, is decided per view.

**Calculated fields** — any `number`, `bool`, or `timespan` field can be marked as calculated. Write a formula using `{FieldName}` syntax and Operum evaluates it automatically on every create or update. TimeSpan fields support unit access via `{Field.hours}`, `{Field.minutes}`, and `{Field.seconds}`. Calculated fields are hidden from the entry form. Select a group of entries and force-recalculate them on demand when a formula changes after data already exists.

**Constants** — define named reusable values (`number`, `bool`, `timespan`) and reference them in formulas by name. Each constant can have up to 6 conditional values: each conditional value has a priority and filter conditions, and the evaluator picks the lowest-priority match for the current entry, falling back to the base value if none match. This lets a single constant behave differently depending on what's in the entry.

### Entries

Create, edit, duplicate, and delete entries individually, or select a group to bulk-delete or bulk-recalculate calculated fields. A quick-add dialog captures one entry in a few keystrokes. Import existing data from a CSV file, or export at any time, optionally filtered to a specific view.

### Views

A view is a saved lens on your data: a set of filters, a sort order, and a column selection. Switch between views without losing any configuration. Drag to reorder them, and mark one as the default that opens automatically. Use dynamic value filters to build live views that shift over time, with date values such as `today`, `start of month`, or `end of year`.

Views are assembled from reusable queries. A query is a single clause, one filter or one sort, shared across the views that use it, so the same "logged this month" filter can back several views at once.

### Dashboards

Build any number of dashboards ("boards"), each with its own name, color, and icon, and set one as your landing page. A board is a grid of widgets you arrange by dragging; desktop and mobile layouts are kept separately, so a board can be dense on a screen and single-column on a phone. Any widget can be made expandable for a full-size view, and related widgets can be grouped into a container panel that moves and resizes as one.

| Widget | What it does |
|---|---|
| Chart | A saved chart definition from the Widget Library, placed by reference |
| Entries table | A saved table showing a tracker's most recent rows, with chosen columns |
| Quick-add button | Opens a tracker's quick-add entry dialog straight from the board |
| Filter | Live controls that narrow the chart and table widgets on the board that follow it |
| Header / Divider / Note | Layout and annotation |
| Container | A panel holding a sub-grid of other widgets, so a group can be moved, resized, and titled as one. Nesting is one level deep |

The **Widget Library** holds chart and table definitions independently of any board. Build one once, place it on as many boards as you want, edit or delete it in one place. A filter widget can offer board-level saved filter sets as one-tap presets, and each followed widget picks which of its tracker's fields the filter runs against.

### Analytics

Charts are built in the Widget Library and calculated at query time. Each chart reads from one or more sources; a source is a tracker plus a field mapping, and can be scoped to a fixed tracker view. Line and bar charts can combine several sources onto one shared axis to compare trackers side by side; other chart types read from a single tracker.

| Type | Variants | Description |
|---|---|---|
| Single Value | 12 | One headline metric: count, sum, min, max, average, standard deviation, unique count, most/least common, or yes/no counts and percentage |
| Line Chart | 7 | Raw values, sum by category, cumulative running total, or daily/weekly/monthly/yearly totals |
| Bar Chart | 7 | Count, sum, or average per category, or daily/weekly/monthly/yearly totals |
| Scatter Chart | 1 | Numeric X/Y point cloud for spotting correlations |
| Donut Chart | 1 | Groups entries by a category, sums a numeric field per group |
| Calendar | 1 | Maps entries onto a date field, labeled with another field's value |

Each chart only exposes fields that are compatible with the role being configured: a Y-axis only accepts `number` or `timespan`, a calendar date axis only accepts `date` or `datetime`.

### Integrations

Pull data in from external services and map their values onto tracker fields. You pick which source field feeds which tracker field, and the type pairing is checked when you save. Imports are read-only and upsert on a provider-stable id, so re-syncing updates rows instead of duplicating them; a missing value is skipped, not written as zero. Re-import from a chosen date at any time to backfill or repair history. Stored credentials are encrypted at rest.

| Provider | Direction | Data |
|---|---|---|
| intervals.icu | Pull on a schedule, connected with an API key | Daily wellness snapshots and activities |
| Firefly III | Push by webhook, self-hosted (needs your instance URL) | Transactions |

Connecting a push provider gives you a webhook URL and a signing secret to paste into that service; the secret is shown once and can be rotated.

### Collaboration

Share a tracker with other users by searching for their username. Collaborators are view-only by default. Grant permissions independently:

- **Edit data** — create, edit, and delete entries
- **Edit schema** — manage fields, views, and constants

Permissions can be changed at any time. Tracker metadata, the default view, and collaborator management are owner-only.

### Accounts and administration

Authentication supports email/password with confirmation and Google OAuth. Two roles: **User** and **Admin**. The admin panel adds a platform overview, user management, tracker oversight, and template publishing, admins publish tracker templates that any user can clone as a starting point.

A command palette (`Ctrl`/`Cmd` + `K`) jumps to any tracker or board and runs common actions from anywhere.

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
