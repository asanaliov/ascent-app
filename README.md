# Ascent

A hiking-trail tracker. ASP.NET Core MVC (.NET 10), EF Core,
SQLite, ASP.NET Core Identity.

## Running

```bash
dotnet run
```

The database is created and seeded automatically on first run (roles, demo users,
difficulty bins, regions).

## Demo credentials

| Role  | Email                | Password |
|-------|----------------------|----------|
| Admin | admin@ascent.local   | Admin1!  |
| Guide | guide@ascent.local   | Guide1!  |

New registrations are assigned the **Hiker** role by default.
