# Operum

**Track anything. Structure it your way.**

Operum is a self-hosted data tracking app built for people who want more control than a spreadsheet but less overhead than a full database. Define your schema, log entries, build views, and visualize trends — all in one place, shareable with your team.

---

## Features

### Trackers

A tracker is a container for a specific kind of data — a reading list, a workout log, a bug database, a habit tracker. You define the shape of that data by adding fields, and Operum handles the rest.

**Field types:** `string` `number` `bool` `date` `datetime` `timespan`

Each field has a name, optional description, ordering, and visibility toggle. Up to 25 fields per tracker.

**Calculated fields**

Mark any `number`, `bool`, or `timespan` field as calculated and give it a formula. Formulas reference other fields and constants using `{FieldName}` syntax. TimeSpan fields additionally support property access: `{Field.hours}`, `{Field.minutes}`, `{Field.seconds}`.

Calculated fields are evaluated automatically on every create and update, and can be force-recalculated in bulk when a formula changes after data already exists.

**Constants**

Define named, reusable values at the tracker level and reference them in formulas by name. Constants support `number`, `bool`, and `timespan` types.

Each constant can have up to 6 **conditional values**. A conditional value has a priority and a set of filter conditions — when a formula is evaluated for an entry, Operum picks the lowest-priority conditional value whose filters all match, falling back to the base value if none do. This lets a single constant behave differently depending on what's in the entry.

---

### Entries

The data itself. Each entry fills in the fields defined by the tracker.

- Create, edit, duplicate, and delete entries individually
- Select multiple entries to bulk-delete or bulk-recalculate calculated fields
- Import from CSV to bring in existing data
- Export to CSV at any time, optionally filtered to a specific view

---

### Views

A view is a saved configuration on top of your data — filters, sort order, and visible columns. Switch between views to see the same entries from different angles without losing your other setups.

- Up to 25 views per tracker
- Up to 6 filters and 3 sort rules per view
- Drag to reorder views
- Set a default view that opens automatically

---

### Analytics

Turn raw entries into charts. Analytics are calculated at query time and can be scoped to a specific view's filters.

Up to 10 analytics per tracker, displayed in a draggable masonry grid.

| Chart type | Description |
|---|---|
| Single Value | One headline metric: count, sum, min, max, average, standard deviation, or boolean percentage |
| Line Chart | X/Y line plot with support for raw, aggregated, or cumulative modes |
| Scatter Chart | Numeric X/Y point cloud for correlation analysis |
| Donut Chart | Groups entries by a category field, sums a numeric field per group |
| Bar Chart | Bar visualization grouped by a field |
| Calendar | Maps entries to a date field, labels each point with another field's value |

Each chart type only surfaces fields that are compatible with the role being configured — a Y-axis only accepts `number` or `timespan`, a calendar axis only accepts `date` or `datetime`.

---

### Collaboration

Share any tracker with other users. Search by username and add them as collaborators with fine-grained permissions:

| Permission | What it unlocks |
|---|---|
| Edit data | Create, edit, and delete entries |
| Edit schema | Create, edit, and delete fields, views, analytics, and constants |

Both flags are off by default — collaborators are view-only until explicitly granted access. Permissions can be changed at any time. Only the tracker owner can manage collaborators, tracker metadata, and default views.

---

### Templates

Admins can publish tracker templates that any user can clone as a starting point. Browse available templates instead of building common structures from scratch.

---

### Accounts

- Email/password registration with email confirmation
- Google OAuth login
- Account lockout after repeated failed login attempts
- Two roles: **User** (create trackers, collaborate) and **Admin** (full access plus user management and platform configuration)

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
| Conditional values per constant | 6 |
| Filters per view | 6 |
| Sorts per view | 3 |
| Formula length | 500 characters |

---

## Self-Hosting

Operum runs as a Docker Compose stack: backend API, frontend, PostgreSQL, and optional Prometheus + Grafana monitoring.

**Requirements:** Docker and Docker Compose

**Setup:**

```bash
# 1. Configure the backend
cp backend/src/Operum.API/appsettings.Example.txt backend/src/Operum.API/appsettings.json
# Edit appsettings.json — database connection string, JWT key, Mailgun credentials, Google Client ID

# 2. Configure the frontend
cp frontend/.env.example frontend/.env
# Edit .env — set VITE_REACT_API_URL and VITE_REACT_GOOGLE_CLIENT

# 3. Start
docker-compose up -d
```

| Service | URL |
|---|---|
| App | http://localhost:3000 |
| API | http://localhost:5000/api |
| API Docs | http://localhost:5000/api/swagger/index.html |
| Grafana | http://localhost:3001 |

**Required configuration:**

| Setting | File | Purpose |
|---|---|---|
| `ConnectionStrings.Operum` | appsettings.json | PostgreSQL connection string |
| `JwtSettings.Key` | appsettings.json | JWT signing secret |
| `MailGun.ApiKey` | appsettings.json | Email confirmation |
| `Google.ClientId` | appsettings.json + .env | Google OAuth |
| `VITE_REACT_API_URL` | frontend/.env | API base URL |

---

## Tech Stack

| Layer | Technologies |
|---|---|
| Backend | .NET 9, ASP.NET Core, Entity Framework Core, PostgreSQL |
| Frontend | React 19, TypeScript, Vite, Mantine, MobX |
| Infrastructure | Docker, Nginx, Prometheus, Grafana |
