# 🏔️ Ascent

<p align="center">
  <img src="https://github.com/user-attachments/assets/8dec7958-727c-413d-bd90-abc4bcf1d5f9" alt="Ascent Logo" width="150">
</p>

<h3 align="center">Discover. Explore. Climb.</h3>

<p align="center">
  A full-stack hiking platform for discovering, saving, reviewing, and logging trails across North Macedonia.
</p>

<p align="center">
  <strong>ASP.NET Core MVC</strong> ·
  <strong>.NET 10</strong> ·
  <strong>EF Core 10</strong> ·
  <strong>SQLite</strong> ·
  <strong>ASP.NET Core Identity</strong> ·
  <strong>MapLibre GL</strong> ·
  <strong>Bootstrap 5</strong>
</p>

---

## 🖥️ Preview

<p align="center">
  <img src="https://github.com/user-attachments/assets/92a53a78-cfd0-4628-bf7a-839dab2d34ad" alt="Ascent Trail Discovery" width="900">
</p>

---

## ✨ Features

### 🗺️ Trail Discovery

* Browse a curated catalog of hiking trails across North Macedonia
* Search, filter, and sort trails
* Explore trails on an interactive map
* View route geometry and detailed trail information
* Discover nearby trails using a saved home location or browser geolocation
* Save trails to favorites

### 🥾 Hiking & Activity

* Log completed hikes
* Upload photos from hiking activities
* Earn badges and achievements
* Track hiking activity through your profile
* View personal hiking history

### ⭐ Reviews & Profiles

* Review and rate trails
* Browse reviews from other hikers
* View detailed hiker profiles
* Explore uploaded hiking photos
* View earned badges and activity

### 🧭 Guide & Admin Tools

* Dedicated Guide tools for trail management
* Administrative tools for site management
* Role-based access control through ASP.NET Core Identity

---

## 🏔️ Trail Catalog

Ascent ships with a curated local catalog focused on **North Macedonia**.

The catalog is stored as version-controlled JSON and imported into the local EF Core database during development.

```text
Data/seed/
├── macedonia-trails.json
├── demo-users.json
├── demo-reviews.json
├── demo-favorites.json
├── demo-hike-logs.json
└── demo-hike-events.json
```

### Seeded development data

| Dataset           | Records |
| ----------------- | ------: |
| 🥾 Curated trails |      20 |
| 👤 Demo users     |      12 |
| ⭐ Reviews         |      48 |
| ❤️ Favorites      |      40 |
| 📖 Hike logs      |      24 |
| 🧭 Guided events  |       7 |

Trail naming conventions, review sources, and geometry limitations are documented in [`Data/seed/README.md`](Data/seed/README.md).

---

## 🌱 Idempotent Database Seeding

Ascent uses a dedicated `DatabaseSeeder` to populate the development database.

The seeding process:

* Applies pending EF Core migrations
* Imports the version-controlled trail catalog
* Creates development Identity users
* Adds demo reviews, favorites, hike logs, and events
* Matches existing records using stable identifiers
* Prevents duplicate records when the application is restarted

This means the application can be stopped and restarted without continuously creating duplicate demo data.

Development seeding is controlled through:

```json
"DatabaseSeeding": {
  "SeedTrails": true,
  "SeedDemoData": true
}
```

Production defaults both options to `false`.

Demo identities and activity are additionally protected by:

```csharp
IWebHostEnvironment.IsDevelopment()
```

---

## 🛠️ Tech Stack

| Technology                   | Purpose                                     |
| ---------------------------- | ------------------------------------------- |
| **ASP.NET Core MVC**         | Web application architecture                |
| **C# / .NET 10**             | Application development                     |
| **Entity Framework Core 10** | ORM & database access                       |
| **SQLite**                   | Local relational database                   |
| **ASP.NET Core Identity**    | Authentication & authorization              |
| **MapLibre GL**              | Interactive maps & geospatial visualization |
| **Bootstrap 5**              | Responsive UI                               |
| **Razor**                    | Server-side rendering                       |
| **JSON**                     | Version-controlled seed data                |
| **EF Core Migrations**       | Database schema management                  |

---

## 🏗️ Architecture

Ascent follows the **Model-View-Controller (MVC)** architecture with dedicated services and view models.

```text
                         ┌───────────────────┐
                         │      Browser      │
                         └─────────┬─────────┘
                                   │
                                   ▼
                         ┌───────────────────┐
                         │   Razor Views     │
                         │   Bootstrap 5     │
                         └─────────┬─────────┘
                                   │
                                   ▼
                         ┌───────────────────┐
                         │   Controllers     │
                         │       MVC         │
                         └─────────┬─────────┘
                                   │
                    ┌──────────────┴──────────────┐
                    ▼                             ▼
          ┌──────────────────┐          ┌──────────────────┐
          │     Services     │          │    ViewModels    │
          │  Business Logic  │          │   Page / Forms   │
          └────────┬─────────┘          └──────────────────┘
                   │
                   ▼
          ┌──────────────────┐
          │      Models      │
          │     EF Core     │
          └────────┬─────────┘
                   │
                   ▼
          ┌──────────────────┐
          │      SQLite      │
          └──────────────────┘
```

---

## 📁 Project Structure

```text
Ascent/
│
├── Areas/
│   └── Admin/             # Admin controllers and views
│
├── Controllers/           # Public MVC controllers
│
├── Data/
│   ├── seed/              # Version-controlled JSON data
│   └── Seeders/           # Database and Identity seeding
│
├── Models/                # EF Core entities
│
├── Services/              # Application and business services
│
├── ViewModels/            # MVC page and form models
│
├── Views/                 # Razor views and shared components
│
├── wwwroot/
│   ├── img/trails/        # Curated trail photography
│   ├── images/            # Images and fallbacks
│   └── uploads/           # Runtime user uploads
│
└── Migrations/            # EF Core database migrations
```

---

## 🚀 Getting Started

### Prerequisites

* [.NET 10 SDK](https://dotnet.microsoft.com/)
* EF Core CLI tools

If the repository includes local tool configuration:

```bash
dotnet tool restore
```

Or install EF Core globally:

```bash
dotnet tool install --global dotnet-ef
```

### Run locally

Clone the repository:

```bash
git clone https://github.com/asanaliov/ascent-app.git
cd ascent-app
```

Restore dependencies:

```bash
dotnet restore
```

Apply database migrations:

```bash
dotnet ef database update
```

Start the application:

```bash
dotnet run
```

The application also applies pending migrations and runs the configured idempotent seeder automatically at startup.

---

## 🔑 Development Demo Accounts

All seeded demo users use:

```text
Password: Demo123!
```

| Role      | Email               |
| --------- | ------------------- |
| 🛡️ Admin | `admin@ascent.demo` |
| 🧭 Guide  | `guide@ascent.demo` |
| 🥾 Hiker  | `hiker@ascent.demo` |

These are **fake, development-only identities** used to demonstrate different application roles.

The remaining demo users in `demo-users.json` use the same password and exist only to populate the development environment.

---

## 🔄 Reset & Reseed

To completely reset the local development database, stop the application and remove the SQLite files.

### Linux / macOS

```bash
rm -f ascent.db ascent.db-shm ascent.db-wal
dotnet run
```

### PowerShell

```powershell
Remove-Item ascent.db, ascent.db-shm, ascent.db-wal -ErrorAction SilentlyContinue
dotnet run
```

The application will recreate the database from the committed EF Core migrations and seed JSON.

> The generated database is local runtime state and should never be committed.

---

## 🖼️ Image Handling

The database stores **image paths**, not image binary data.

```text
wwwroot/
├── img/trails/            # Curated trail photography
├── images/
│   ├── trails/            # Trail fallbacks and planned assets
│   └── users/             # User assets
└── uploads/               # Runtime user uploads
```

The repository includes safe SVG fallback images and a small collection of subject-checked, attributed trail photography.

During seeding, only trail image files that actually exist are added to the database.

Planned `.webp` paths may remain in the seed JSON until licensed photography becomes available without creating broken database records.

See [`wwwroot/img/trails/ATTRIBUTION.md`](wwwroot/img/trails/ATTRIBUTION.md) for photo credits and licensing information.

---

## 🌍 Map & Trail Data

Ascent's primary trail catalog is local and version-controlled, allowing the application to run without relying on an external trail API during normal startup.

The project also retains support for **Overpass / OpenStreetMap** for explicit future trail imports.

Normal trail browsing and application startup do **not** require an external Overpass request.

---

## 🔐 Repository Safety

The repository excludes local databases, production configuration, and runtime-generated files.

```text
*.db
*.sqlite
*.sqlite3
*.mdf
*.bak
appsettings.Production.json
wwwroot/uploads/
```

### Never commit

* Production databases
* Production connection strings
* Real credentials
* API keys
* User uploads
* Other secrets

### Commit

* Source code
* EF Core migrations
* Seed JSON
* Safe static assets
* Development configuration

---

## 🧪 Testing

The project includes a dedicated test project for automated testing.

Run the test suite with:

```bash
dotnet test
```

---

## 🎯 Project Focus

Ascent was built as a complete full-stack .NET application rather than a simple CRUD project.

The project demonstrates experience with:

* MVC application architecture
* Relational database design
* Entity Framework Core
* Authentication and authorization
* Role-based access control
* Geospatial data and interactive maps
* User-generated content
* Image management
* Database migrations
* Idempotent data seeding
* Service-based application architecture
* Automated testing

---

## 👨‍💻 Author

**Asan Aliov**

Software Engineering Student · FINKI

[GitHub](https://github.com/asanaliov)

---

<p align="center">
  <strong>🥾 Explore more. Climb higher. Ascent.</strong>
</p>
