# Ascent

Ascent is an ASP.NET Core MVC hiking app for discovering, saving, reviewing,
and logging trails. Its built-in catalog focuses on North Macedonia and is
loaded from version-controlled JSON into the local EF Core database.

ASP.NET Core MVC (.NET 10) · EF Core 10 · SQLite · ASP.NET Core Identity ·
MapLibre GL · Bootstrap 5

## Features

- Browse, search, filter, sort, map, and favorite local trails.
- View route geometry, trail facts, reviews, and multi-image galleries.
- Log hikes, upload photos, earn badges, and view hiker profiles.
- Discover nearby trails using a saved home location or browser geolocation.
- Use Guide tools for trail management and Admin tools for site management.
- Optionally retain the Overpass/OpenStreetMap service for explicit future
  imports; normal trail browsing and startup do not call it.

## Local seed data

Seed files live in `Data/seed/`:

```text
macedonia-trails.json   20 curated North Macedonia trails and image metadata
demo-users.json         12 development-only Identity users
demo-reviews.json       48 reviews
demo-favorites.json     40 favorites
demo-hike-logs.json     24 hike logs
demo-hike-events.json   7 guided events
```

Trail naming conventions, review sources, and geometry limitations are
documented in [`Data/seed/README.md`](Data/seed/README.md).

`Data/Seeders/DatabaseSeeder.cs` applies migrations and imports these files
idempotently. Trails are matched by name, users by email, reviews/favorites by
user and trail, and logs/events by stable composite values. Restarting the app
does not duplicate seeded records.

Development settings enable both the trail catalog and demo activity:

```json
"DatabaseSeeding": {
  "SeedTrails": true,
  "SeedDemoData": true
}
```

Production defaults both options to `false`. Demo identities and activity are
also guarded by `IWebHostEnvironment.IsDevelopment()`, even if configuration
is accidentally enabled outside Development.

## Prerequisites

- .NET 10 SDK
- EF Core CLI tools (`dotnet tool restore` or `dotnet tool install --global dotnet-ef`)

## Run locally

```bash
dotnet restore
dotnet ef database update
dotnet run
```

The app also applies pending migrations and runs the configured idempotent
seeder at startup. The explicit `database update` command is useful for seeing
migration failures before launching the web app.

## Development demo accounts

All seeded demo users use the password `Demo123!`.

| Role | Email | Password |
|---|---|---|
| Admin | `admin@ascent.demo` | `Demo123!` |
| Guide | `guide@ascent.demo` | `Demo123!` |
| Hiker | `hiker@ascent.demo` | `Demo123!` |

These accounts are fake, development-only identities. The other users in
`demo-users.json` use the same password and exist only to populate the demo.

## Reset and reseed

Stop the running app, delete the local SQLite files, and start again:

```bash
rm -f ascent.db ascent.db-shm ascent.db-wal
dotnet run
```

PowerShell:

```powershell
Remove-Item ascent.db, ascent.db-shm, ascent.db-wal -ErrorAction SilentlyContinue
dotnet run
```

Startup recreates the database from committed EF Core migrations and seed JSON.
The generated database is local runtime state and must not be committed.

## Images

The database stores image paths only; it never stores image binary data.

- Curated trail photography: `wwwroot/img/trails/`
- Trail fallbacks and planned assets: `wwwroot/images/trails/`
- User assets: `wwwroot/images/users/`
- Uploaded runtime assets: `wwwroot/uploads/` (gitignored)

The repository includes safe SVG fallback images and a small set of
subject-checked, attributed trail photos. During seeding, only local trail image
files that actually exist are added to the database. Planned `.webp` paths can
remain in the JSON until licensed photography is available; they do not create
broken database records. Missing trail and profile files fall back in the UI.
See `wwwroot/img/trails/ATTRIBUTION.md` for photo credits and licenses.

## Database and repository safety

The `.gitignore` excludes SQLite/SQL Server database files, backups,
production settings, and runtime uploads:

```text
*.db
*.sqlite
*.sqlite3
*.mdf
*.bak
appsettings.Production.json
wwwroot/uploads/
```

Commit the seed JSON files and EF Core migrations. Do not commit databases,
production connection strings, real credentials, API keys, or user uploads.

## Project layout

```text
Areas/Admin/          Admin controllers and views
Controllers/          Public MVC controllers
Data/                 DbContext, migrations entry point, and seed pipeline
Data/seed/            Version-controlled JSON source data
Data/Seeders/         Idempotent EF Core and Identity seeding
Models/               EF Core entities
Services/             Trail, difficulty, geo, badge, and image services
ViewModels/            MVC page and form models
Views/                Razor views and shared trail cards
wwwroot/images/       Version-controlled image assets and fallbacks
```
