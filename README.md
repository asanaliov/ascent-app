# Ascent

A hiking-trail tracker for the mountains of North Macedonia. Browse trails, log
your hikes, earn badges, review and save trails, and find what's near you on a map.

ASP.NET Core MVC (.NET 10) · EF Core 10 (code-first, SQLite) · ASP.NET Core
Identity · Leaflet · Bootstrap 5.

## Features

- **Trails** — browse the grid and open detail pages with stats, tags, a multi-photo
  gallery, and an embedded trailhead map (Leaflet). Guides and Admins create/edit trails.
- **Computed difficulty** — Easy → Strenuous is derived from distance and elevation
  gain (`DifficultyService`), never entered by hand.
- **Filtering, search, sort & pagination** — the trail list filters by region,
  difficulty, tag, and free text, with sorting and paged results.
- **Photo galleries** — Guides upload multiple photos per trail; files live under
  `wwwroot/uploads` (gitignored) and paths are stored in the DB.
- **Hike logs** — any signed-in user logs hikes against a trail, with optional photo
  uploads.
- **Badges** — awarded automatically after each hike by `BadgeService` (hike count,
  cumulative elevation, distinct regions); never awarded twice.
- **Reviews** — one rating (1–5 stars) + comment per user per trail; Admins can moderate.
- **Favorites** — save trails to a personal list.
- **Dashboard** — your totals, badge progress, and recent hikes.
- **Public profiles** (`/Profile/Index/{id}`) — a hiker's public stats, badges, and
  recent activity.
- **My Trails** (`/Guide`) — a guide's own trails with quick management links.
- **Community** (`/Community`) — a public leaderboard ranking hikers.
- **Account profile** (`/Manage`) — edit your own display name and account details.
- **Trails near you** — browser geolocation + Haversine distance (`GeoService`) sorts
  trails by distance, shown on a Leaflet map.
- **Tags** — trails can be tagged; tags are managed in the admin area.
- **Admin area** (`/Admin`) — CRUD for regions, tags, badges, users, trails, and
  reviews, plus role management and review moderation. Admin-only.

## Roles

| Role  | Can do                                                        |
|-------|---------------------------------------------------------------|
| Hiker | Browse, log hikes, review, favorite (default for new sign-ups)|
| Guide | Everything a Hiker can, plus create/edit/delete trails        |
| Admin | Everything, plus the `/Admin` area and role management        |

## Running

```bash
dotnet run
```

The SQLite database is created and seeded automatically on first run — roles, demo
users, difficulty bins, regions, badges, sample trails, and tags.

App runs at the URL printed on startup (e.g. `http://localhost:5164`).
Geolocation ("near me") needs `https://` or `localhost`.

## Demo credentials

| Role  | Email                | Password |
|-------|----------------------|----------|
| Admin | admin@ascent.local   | Admin1!  |
| Guide | guide@ascent.local   | Guide1!  |

New registrations are assigned the **Hiker** role by default.

## Project layout

```
Controllers/            site controllers (Trails, HikeLogs, Reviews, Favorites,
                        Dashboard, Profile, Guide, Community, Manage, Account, Home)
Areas/Admin/            admin area (Regions, Tags, Badges, Users, Trails, Reviews CRUD)
Models/                 13 EF entities
ViewModels/             form/page models (entities aren't bound directly)
Services/               DifficultyService, BadgeService, GeoService
Data/                   AscentDbContext + DbInitializer (seeding)
Views/                  Razor views
wwwroot/css/ascent.css  design system
wwwroot/uploads/        uploaded trail & hike photos (gitignored)
```

## EF Core commands

```bash
dotnet ef migrations add <Name>     # new migration
dotnet ef database update           # apply migrations
dotnet ef migrations remove         # undo the last (if unapplied)
```
