# Operum

Self-hosted data tracking. Define your schema, log entries, build views, and visualize trends — without wrestling with spreadsheets.

---

## Core concepts

**Trackers** are the top-level container. Each tracker has fields (`string`, `number`, `bool`, `date`, `datetime`, `timespan`), entries, views, and analytics. Share any tracker with collaborators.

**Fields** can be plain or calculated. Calculated fields use a formula (`{FieldName}` syntax) evaluated automatically on every save. Constants let you define named reusable values referenced in formulas — each constant can have conditional values that override the base depending on the entry's data.

**Views** are saved filter + sort + column configurations. Switch between them freely. Set a default that opens automatically.

**Analytics** turn entries into charts — single value, line, scatter, donut, bar, or calendar. Up to 10 per tracker, scoped to any view.

**Collaboration** is permission-based. Collaborators are view-only by default; grant *Edit data* or *Edit schema* independently.

---

## Self-hosting

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
