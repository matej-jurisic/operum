# Operum

Operum is a personal and collaborative data tracking app. Instead of wrestling with spreadsheets, you define exactly the data you want to track, how to view it, and how to visualize it — all in one place.

## What You Can Do

### Build Custom Trackers
A tracker is a collection of data you care about — a reading list, a workout log, a bug database, anything. You define the structure by adding fields with the types that make sense for your data: text, numbers, dates, and more. Up to 25 fields per tracker, each with its own label, description, ordering, and visibility.

**Field types:** String, Number, Boolean, Date, DateTime, TimeSpan

**Calculated fields:** Mark any Number, Bool, or TimeSpan field as calculated and write a formula to derive its value automatically from other fields and constants. Formulas use `{FieldName}` syntax; TimeSpan fields support `{Field.hours}`, `{Field.minutes}`, and `{Field.seconds}` for unit-aware access. Calculated fields evaluate on every entry create or update and are hidden from the entry form. You can also select a group of entries and re-run their calculated fields on demand, useful when a formula changes after entries already exist.

**Constants:** Define reusable named values (Number, Bool, or TimeSpan) at the tracker level and reference them in formulas by name. Up to 25 constants per tracker.

### Log Entries
Once a tracker is set up, you start adding entries. Each entry fills in the fields you've defined. You can create, edit, duplicate, and delete individual entries or bulk-delete a selection. Select multiple entries to bulk-delete or force-recalculate calculated fields on all of them at once. If you already have data elsewhere, you can import it directly from a CSV file. You can also export your data to CSV at any time, optionally scoped to a specific view's filters.

### Create Views
A view is a saved lens on your tracker's data. You can filter by field values, set sort orders, and choose which columns are visible. Switch between views to see the same data from different angles without losing your other configurations. Each tracker can have a default view that opens automatically. Views can be reordered by dragging.

### Visualize with Analytics
Add charts to a tracker to turn raw entries into visual summaries. Up to 10 analytics per tracker, displayed in a draggable masonry grid. You can reorder them by dragging.

**Six chart types:**

- **Single Value** — shows one calculated metric front and center: count, sum, min, max, average, standard deviation, or boolean percentages
- **Line Chart** — plots entries along X/Y axes; supports raw lines, aggregated sums grouped by X, or cumulative running totals
- **Scatter Chart** — numeric X/Y point cloud for spotting correlations between two number fields
- **Donut Chart** — groups entries by a category field and sums a numeric field per group
- **Bar Chart** — bar chart visualization grouped by a field
- **Calendar** — maps entries to a date field and labels each point with another field's value

**Configuring an analytic:**

Pick a chart type and calculation method, then map your tracker's fields to the roles that chart needs (e.g. X-axis, Y-axis, or Value). The UI only shows fields whose data type is compatible with each role — for example, a Y-axis only accepts Number or TimeSpan fields, while a Calendar's date axis only accepts Date or DateTime fields. Analytics are calculated at query time against the tracker's current entries, and can optionally be scoped to a specific view.

### Collaborate
Trackers can be shared with other users. Add teammates or friends to a tracker by searching by username. Each collaborator gets two independent permission flags:

- **Edit data** — can create, edit, and delete entries
- **Edit schema** — can create, edit, and delete fields, views, analytics, and constants

Both flags are off by default (view-only). They can be toggled independently and changed at any time by the tracker owner. Only the owner can manage collaborators and tracker metadata (name, color, description, default views).

### Use Templates
Admins can publish public template trackers that other users can use as a starting point. Browse available templates and build on top of them instead of starting from scratch.

### Accounts & Access
- Sign up with email and password, or log in with Google
- Email confirmation on registration
- Account lockout after repeated failed login attempts
- Two roles: **User** (create and manage trackers, collaborate) and **Admin** (everything, plus user management and platform configuration)

---

## Limits

| Resource | Limit |
|---|---|
| Trackers per account | 20 |
| Fields per tracker | 25 |
| Entries per tracker | 1,000 |
| Views per tracker | 25 |
| Analytics per tracker | 10 |
| Constants per tracker | 25 |
| Filters per view | 6 |
| Sorts per view | 3 |
| Formula length | 500 characters |

---

## Self-Hosting

Operum runs as a Docker Compose stack — backend API, frontend, PostgreSQL, and optional Prometheus + Grafana monitoring.

### Requirements

- Docker and Docker Compose

### Setup

1. Configure the backend:

   ```bash
   cp backend/src/Operum.API/appsettings.Example.txt backend/src/Operum.API/appsettings.json
   # Edit appsettings.json — set database connection string, JWT key, Mailgun credentials, Google Client ID
   ```

2. Configure the frontend:

   ```bash
   cp frontend/.env.example frontend/.env
   # Edit .env — set VITE_REACT_API_URL and VITE_REACT_GOOGLE_CLIENT
   ```

3. Start everything:

   ```bash
   docker-compose up -d
   ```

| Service | URL |
|---|---|
| App | http://localhost:3000 |
| API | http://localhost:5000/api |
| API Docs | http://localhost:5000/api/swagger/index.html |
| Grafana | http://localhost:3001 |

### Required Configuration

| Setting | Where | Purpose |
|---|---|---|
| `ConnectionStrings.Operum` | appsettings.json | PostgreSQL connection |
| `JwtSettings.Key` | appsettings.json | JWT signing secret |
| `MailGun.ApiKey` | appsettings.json | Email confirmation sending |
| `Google.ClientId` | appsettings.json + .env | Google OAuth |
| `VITE_REACT_API_URL` | frontend/.env | API base URL |

## Tech Stack

| | |
|---|---|
| Backend | .NET 9, ASP.NET Core, Entity Framework Core, PostgreSQL |
| Frontend | React 19, TypeScript, Vite, Mantine, MobX |
| Infrastructure | Docker, Nginx, Prometheus, Grafana |
