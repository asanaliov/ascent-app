# Ascent

Ascent is a polished hiking trail app for discovering, saving, reviewing, and
logging mountain routes. The demo is seeded with trails, users, reviews, badges,
and hike logs so it feels like a live outdoor community from the first run.

ASP.NET Core MVC (.NET 10) · EF Core 10 · SQLite · Identity · MapLibre GL ·
Bootstrap 5

## Design

Ascent uses a warm editorial outdoor style: soft paper tones, forest green,
terracotta accents, serif headings, photo-first trail cards, compact stats, and
map-led discovery.

The app is built around clear trail confidence signals: difficulty, distance,
elevation, region, photos, reviews, favorites, route geometry, and 3D terrain
maps.

## Highlights

- Browse, search, filter, sort, and save trails.
- View photo galleries, reviews, facts, tags, and route maps.
- Log hikes, upload photos, earn badges, and track progress.
- Explore nearby trails using home location or browser geolocation.
- View public hiker profiles and a community leaderboard.
- Manage guide trails and admin content.

## Roles

| Role  | Can do                                                        |
|-------|---------------------------------------------------------------|
| Hiker | Browse, log hikes, review, favorite (default for new sign-ups)|
| Guide | Everything a Hiker can, plus create/edit/delete trails        |
| Admin | Everything, plus the `/Admin` area and role management        |

## Demo

| Role  | Email                | Password |
|-------|----------------------|----------|
| Admin | admin@ascent.local   | Admin1!  |
| Guide | guide@ascent.local   | Guide1!  |
| Hiker | demo@ascent.local    | Hiker1!  |

The demo hiker starts from Skopje, so nearby trail discovery works immediately.
Extra seeded hikers populate reviews, profiles, and the leaderboard.

## Project Layout

```text
Controllers/            Public app controllers
Areas/Admin/            Admin area
Models/                 EF Core entities
ViewModels/             Page and form models
Services/               Difficulty, badges, geo, image storage
Data/                   DbContext and seed data
Views/                  Razor views
wwwroot/css/ascent.css  Main visual system
wwwroot/js/             Map and route editor scripts
```
