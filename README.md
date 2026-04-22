# Operum

Available at operum.app

Flexible data tracking. Define your schema, log entries, build views, and visualize trends — without wrestling with spreadsheets.

---

## Features

### Trackers and fields

A tracker is a container for one kind of data — a reading list, workout log, bug database, whatever. You define its structure with fields, then start logging entries.

Supported field types: `string`, `number`, `bool`, `date`, `datetime`, `timespan`. Each field has a name, optional description, and can be hidden from views or marked required.

**Calculated fields** — any `number`, `bool`, or `timespan` field can be marked as calculated. Write a formula using `{FieldName}` syntax and Operum evaluates it automatically on every create or update. TimeSpan fields support unit access via `{Field.hours}`, `{Field.minutes}`, and `{Field.seconds}`. Calculated fields are hidden from the entry form. Select a group of entries and force-recalculate them on demand when a formula changes after data already exists.

**Constants** — define named reusable values (`number`, `bool`, `timespan`) and reference them in formulas by name. Each constant can have up to 6 conditional values: each conditional value has a priority and filter conditions, and the evaluator picks the lowest-priority match for the current entry, falling back to the base value if none match. This lets a single constant behave differently depending on what's in the entry.

### Entries

Create, edit, duplicate, and delete entries individually, or select a group to bulk-delete or bulk-recalculate calculated fields. Import existing data from a CSV file, or export at any time — optionally filtered to a specific view.

### Views

A view is a saved lens on your data: a set of filters, a sort order, and a column selection. Switch between views without losing any configuration. Drag to reorder them, and mark one as the default that opens automatically. Make use of dynamic value filters to create live views that change over time by using Date values such as `today`, `start of month`, `end of year` and such.

### Analytics

Add charts to any tracker to visualize your entries. Charts are calculated at query time and can be scoped to a specific view's filters.

| Type | Description |
|---|---|
| Single Value | One headline metric: count, sum, min, max, average, standard deviation, or boolean percentage |
| Line Chart | X/Y line with raw, aggregated sum, or cumulative running total modes |
| Scatter Chart | Numeric X/Y point cloud for spotting correlations |
| Donut Chart | Groups entries by a category, sums a numeric field per group |
| Bar Chart | Bar visualization grouped by any field |
| Calendar | Maps entries onto a date field, labeled with another field's value |

Each chart only exposes fields that are compatible with the role being configured — a Y-axis only accepts `number` or `timespan`, a calendar date axis only accepts `date` or `datetime`.

### Collaboration

Share a tracker with other users by searching for their username. Collaborators are view-only by default. Grant permissions independently:

- **Edit data** — create, edit, and delete entries
- **Edit schema** — manage fields, views, analytics, and constants

Permissions can be changed at any time. Tracker metadata, default views, and collaborator management are owner-only.

### Templates and accounts

Admins can publish tracker templates that any user can clone as a starting point. Authentication supports email/password with confirmation and Google OAuth. Two roles: **User** and **Admin** (adds user management and platform configuration).

---

## Setup & Installation

Requires Docker and Docker Compose.

```bash
cp backend/src/Operum.API/appsettings.Example.txt backend/src/Operum.API/appsettings.json
cp frontend/.env.example frontend/.env
# Fill in both files, then:
docker-compose up -d
```

| Service | URL |
|---|---|
| App | http://localhost:3000 |
| API | http://localhost:5000/api |
| Swagger | http://localhost:5000/api/swagger/index.html |
| Grafana | http://localhost:3001 |

Key settings: `ConnectionStrings.Operum`, `JwtSettings.Key`, `MailGun.ApiKey`, `Google.ClientId`, `VITE_REACT_API_URL`.

---

## Stack

Backend: .NET 9, ASP.NET Core, Entity Framework Core, PostgreSQL  
Frontend: React 19, TypeScript, Vite, Mantine, MobX  
Infrastructure: Docker, Nginx, Prometheus, Grafana
