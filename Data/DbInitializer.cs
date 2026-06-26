using System.Text.Json;
using ascent_app.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ascent_app.Data;

public static class DbInitializer
{
    public static readonly string[] Roles = { "Hiker", "Guide", "Admin" };

    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var sp = scope.ServiceProvider;

        var context = sp.GetRequiredService<AscentDbContext>();
        await context.Database.MigrateAsync();

        var roleManager = sp.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var role in Roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }

        var userManager = sp.GetRequiredService<UserManager<ApplicationUser>>();
        await EnsureUserAsync(userManager, "admin@ascent.local", "Admin", "Admin1!");
        await EnsureUserAsync(userManager, "guide@ascent.local", "Guide", "Guide1!", displayName: "Demo Guide");

        await EnsureUserAsync(userManager, "demo@ascent.local", "Hiker", "Hiker1!",
            displayName: "Demo Hiker",
            bio: "Weekend hiker based in Skopje, slowly ticking off the Macedonian peaks.",
            home: (41.9981, 21.4254, "Skopje, North Macedonia"), createdDaysAgo: 420);

        foreach (var m in MockHikers)
            await EnsureUserAsync(userManager, m.Email, "Hiker", "Hiker1!", m.Name,
                bio: m.Bio, home: m.Home, createdDaysAgo: m.JoinedDaysAgo);

        var env = sp.GetRequiredService<IHostEnvironment>();
        var seedTrails = LoadSeedTrails(env.ContentRootPath);

        await SeedDifficultiesAsync(context);
        await SeedRegionsAsync(context, seedTrails);
        await SeedBadgesAsync(context);

        var guide = await userManager.FindByEmailAsync("guide@ascent.local");
        await SeedTrailsAsync(context, guide?.Id, seedTrails);
        await SeedTrailPhotosAsync(context, seedTrails);
        await SeedTagsAsync(context);
        await SeedActivityAsync(context, userManager);
    }

    private static readonly (string Email, string Name, (double Lat, double Lng, string Name) Home, string Bio, int JoinedDaysAgo)[] MockHikers =
    {
        ("ana@ascent.local",   "Ana Petrovska",   (41.9981, 21.4254, "Skopje, North Macedonia"),  "Trail runner and Šar regular.", 380),
        ("bojan@ascent.local", "Bojan Ristov",    (42.0096, 20.9719, "Tetovo, North Macedonia"),  "Grew up under Šar Planina.", 410),
        ("elena@ascent.local", "Elena Stojanoska",(41.0314, 21.3347, "Bitola, North Macedonia"),  "Pelister every chance I get.", 300),
        ("marko@ascent.local", "Marko Ilievski",  (41.1172, 20.8019, "Ohrid, North Macedonia"),   "Lakeside hikes around Galičica.", 260),
        ("sara@ascent.local",  "Sara Novak",      (46.0569, 14.5058, "Ljubljana, Slovenia"),      "Alpine hiker visiting the Balkans.", 210),
    };

    private static async Task EnsureUserAsync(
        UserManager<ApplicationUser> userManager,
        string email, string role, string password, string? displayName = null,
        string? bio = null, (double Lat, double Lng, string Name)? home = null, int createdDaysAgo = 0)
    {
        if (await userManager.FindByEmailAsync(email) is not null) return;

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            DisplayName = displayName ?? role,
            Bio = bio,
            HomeLat = home?.Lat,
            HomeLng = home?.Lng,
            HomeLocationName = home?.Name,
            CreatedAt = DateTime.UtcNow.AddDays(-createdDaysAgo),
        };

        var result = await userManager.CreateAsync(user, password);
        if (result.Succeeded)
            await userManager.AddToRoleAsync(user, role);
    }

    private static async Task SeedDifficultiesAsync(AscentDbContext context)
    {
        if (await context.Difficulties.AnyAsync()) return;

        context.Difficulties.AddRange(
            new Difficulty { Label = "Easy", MinScore = 0, MaxScore = 50, BadgeClass = "diff-easy" },
            new Difficulty { Label = "Moderate", MinScore = 50, MaxScore = 100, BadgeClass = "diff-moderate" },
            new Difficulty { Label = "Hard", MinScore = 100, MaxScore = 150, BadgeClass = "diff-hard" },
            new Difficulty { Label = "Strenuous", MinScore = 150, MaxScore = null, BadgeClass = "diff-strenuous" });

        await context.SaveChangesAsync();
    }

    private static async Task SeedRegionsAsync(AscentDbContext context, List<SeedTrail> seeds)
    {
        if (await context.Regions.AnyAsync()) return;

        var regions = seeds
            .GroupBy(s => (s.Country, s.Region))
            .Select(g => new Region { Name = g.Key.Region, Country = g.Key.Country });

        context.Regions.AddRange(regions);
        await context.SaveChangesAsync();
    }

    private static async Task SeedBadgesAsync(AscentDbContext context)
    {
        if (await context.Badges.AnyAsync()) return;

        context.Badges.AddRange(
            new Badge { Name = "First Steps", Description = "Logged your first hike.", IconName = "ti-shoe", Criteria = BadgeCriteria.HikeCount, Threshold = 1 },
            new Badge { Name = "Trailblazer", Description = "Logged 10 hikes.", IconName = "ti-flame", Criteria = BadgeCriteria.HikeCount, Threshold = 10 },
            new Badge { Name = "Summit Seeker", Description = "Climbed 5,000 m in total.", IconName = "ti-mountain", Criteria = BadgeCriteria.CumulativeElevation, Threshold = 5000 },
            new Badge { Name = "Explorer", Description = "Hiked in 3 different regions.", IconName = "ti-map-2", Criteria = BadgeCriteria.DistinctRegions, Threshold = 3 });

        await context.SaveChangesAsync();
    }

    private static async Task SeedTagsAsync(AscentDbContext context)
    {
        if (await context.Tags.AnyAsync()) return;

        var names = new[] { "Forest", "Lake", "Summit", "Family-friendly", "Loop", "Panoramic", "Alpine", "Waterfall" };
        var tags = names.Select(n => new Tag { Name = n }).ToList();
        context.Tags.AddRange(tags);
        await context.SaveChangesAsync();

        var byName = tags.ToDictionary(t => t.Name, t => t.Id);
        var map = new Dictionary<string, string[]>
        {
            ["Matka – Shishevo Monastery"] = new[] { "Lake", "Family-friendly", "Forest" },
            ["Popova Šapka – Titov Vrv"] = new[] { "Summit", "Alpine", "Panoramic" },
            ["Plat Ridge"] = new[] { "Alpine", "Panoramic" },
            ["Karanikolica Lake"] = new[] { "Lake", "Alpine" },
            ["Mount Korab"] = new[] { "Summit", "Alpine", "Panoramic" },
            ["Strezimir – Golem Korab"] = new[] { "Summit", "Alpine", "Panoramic" },
            ["Bozovce – Lešnica Waterfalls"] = new[] { "Waterfall", "Forest", "Alpine" },
            ["Magaro Peak"] = new[] { "Summit", "Panoramic", "Lake" },
            ["Galičica H-6 Ridge"] = new[] { "Panoramic", "Forest" },
            ["Galičica G-2"] = new[] { "Forest", "Family-friendly" },
        };

        var trails = await context.Trails.Where(t => map.Keys.Contains(t.Name)).ToListAsync();
        foreach (var trail in trails)
            foreach (var tagName in map[trail.Name])
                context.TrailTags.Add(new TrailTag { TrailId = trail.Id, TagId = byName[tagName] });

        await context.SaveChangesAsync();
    }

    private static async Task SeedActivityAsync(
        AscentDbContext context, UserManager<ApplicationUser> userManager)
    {
        if (await context.HikeLogs.AnyAsync()) return;

        var emails = new[]
        {
            "admin@ascent.local", "guide@ascent.local", "demo@ascent.local",
            "ana@ascent.local", "bojan@ascent.local", "elena@ascent.local",
            "marko@ascent.local", "sara@ascent.local",
        };
        var users = new Dictionary<string, ApplicationUser>();
        foreach (var e in emails)
            if (await userManager.FindByEmailAsync(e) is { } u) users[e] = u;

        var trails = await context.Trails.ToDictionaryAsync(t => t.Name, t => t.Id);
        int Trail(string name) => trails[name];

        var logs = new (string User, string Trail, int DaysAgo, int? Duration, string? Notes)[]
        {
            ("guide@ascent.local", "Matka – Shishevo Monastery", 168, 110, "Led a small group along the canyon; calm water all morning."),
            ("guide@ascent.local", "Popova Šapka – Titov Vrv", 140, 410, "Long but rewarding alpine push to the Šar high point."),
            ("guide@ascent.local", "Mount Korab", 98, 520, "Country's roof. Started before dawn."),
            ("guide@ascent.local", "Strezimir – Golem Korab", 12, 690, "Recon for a guided border traverse next month."),
            ("admin@ascent.local", "Galičica G-2", 132, 90, "Easy reset hike after work."),
            ("admin@ascent.local", "Magaro Peak", 88, 210, null),
            ("admin@ascent.local", "Plat Ridge", 40, 185, "Packed lunch at the saddle."),
            ("demo@ascent.local", "Matka – Shishevo Monastery", 205, 115, "First proper hike of the season."),
            ("demo@ascent.local", "Galičica G-2", 176, 100, "Quick lakeside loop."),
            ("demo@ascent.local", "Popova Šapka – Titov Vrv", 150, 425, "Bucket-list Šar summit — finally!"),
            ("demo@ascent.local", "Pelister – Kratero Ridge", 119, 305, "Big ridge day down in Pelister."),
            ("demo@ascent.local", "Magaro Peak", 88, 205, "Twin-lake views never get old."),
            ("demo@ascent.local", "Mavrovo MK-6 Trail", 57, 160, "Cool forest air, saw deer."),
            ("demo@ascent.local", "St. George Monastery Path", 29, 110, "Short Sunday hike to the monastery."),
            ("demo@ascent.local", "Galičica T-3", 9, 95, "Legs felt good today."),
            ("demo@ascent.local", "Bozovce – Lešnica Waterfalls", 47, 230, "Followed the valley up to the falls — gorgeous."),
            ("bojan@ascent.local", "Bozovce – Lešnica Waterfalls", 71, 240, "Local favourite, the waterfall was roaring."),
            ("ana@ascent.local", "Plat Ridge", 160, 170, "Trail-run reps on the plateau."),
            ("ana@ascent.local", "Karanikolica Lake", 95, 160, null),
            ("ana@ascent.local", "Popova Šapka – Titov Vrv", 35, 405, "PR on the climb."),
            ("bojan@ascent.local", "Popova Šapka – Titov Vrv", 200, 430, "Home mountain."),
            ("bojan@ascent.local", "Mount Korab", 120, 540, "Long day on the border ridge."),
            ("bojan@ascent.local", "Plat Ridge", 44, 175, null),
            ("elena@ascent.local", "Pelister – Kratero Ridge", 150, 300, "Pelister in autumn colours."),
            ("elena@ascent.local", "Magaro Peak", 70, 210, "Drove up from Bitola."),
            ("elena@ascent.local", "Galičica H-6 Ridge", 25, 235, null),
            ("marko@ascent.local", "Magaro Peak", 130, 200, "Sunset over Ohrid."),
            ("marko@ascent.local", "Galičica G-2", 80, 95, null),
            ("marko@ascent.local", "Galičica T-3", 33, 100, "Short evening leg-stretch."),
            ("sara@ascent.local", "Triglav via Kredarica", 180, 480, "Home in the Julian Alps."),
            ("sara@ascent.local", "Mount Korab", 60, 545, "Balkan road trip highlight."),
            ("sara@ascent.local", "Tongariro Alpine Crossing", 20, 410, "On holiday in NZ!"),
        };

        var hikeLogs = logs
            .Where(l => users.ContainsKey(l.User))
            .Select(l => new HikeLog
            {
                UserId = users[l.User].Id,
                TrailId = Trail(l.Trail),
                HikedOn = DateTime.Today.AddDays(-l.DaysAgo),
                DurationMinutes = l.Duration,
                Notes = l.Notes,
                CreatedAt = DateTime.UtcNow,
            });
        await context.HikeLogs.AddRangeAsync(hikeLogs);

        var reviews = new (string User, string Trail, int Rating, string? Comment)[]
        {
            ("guide@ascent.local", "Matka – Shishevo Monastery", 5, "Gorgeous canyon walk. Great for beginners too."),
            ("guide@ascent.local", "Mount Korab", 5, "Bucket-list summit. Long approach."),
            ("admin@ascent.local", "Galičica G-2", 4, "Family-friendly and scenic."),
            ("admin@ascent.local", "Magaro Peak", 4, "Loved the view over both lakes."),
            ("demo@ascent.local", "Popova Šapka – Titov Vrv", 5, "Hardest day I've done, worth every step."),
            ("demo@ascent.local", "Matka – Shishevo Monastery", 4, "Lovely and easy, gets busy on weekends."),
            ("demo@ascent.local", "Pelister – Kratero Ridge", 5, "Pelister is underrated — incredible ridge."),
            ("demo@ascent.local", "Mavrovo MK-6 Trail", 4, "Quiet, shady, well-marked."),
            ("ana@ascent.local", "Plat Ridge", 4, "Great runnable terrain up top."),
            ("ana@ascent.local", "Popova Šapka – Titov Vrv", 5, "The Šar classic. Start early."),
            ("bojan@ascent.local", "Bozovce – Lešnica Waterfalls", 5, "Best waterfall hike in the Šar. Bring sturdy shoes."),
            ("bojan@ascent.local", "Mount Korab", 5, "Nothing beats the roof of the country."),
            ("bojan@ascent.local", "Plat Ridge", 3, "Nice but exposed when it's windy."),
            ("elena@ascent.local", "Pelister – Kratero Ridge", 5, "My favourite, right on my doorstep."),
            ("elena@ascent.local", "Galičica H-6 Ridge", 4, "Big open views over both lakes."),
            ("marko@ascent.local", "Magaro Peak", 5, "Best sunset spot in the country."),
            ("marko@ascent.local", "Galičica G-2", 4, "Easy and scenic, perfect after work."),
            ("sara@ascent.local", "Mount Korab", 4, "Tough approach but stunning. Bring water."),
            ("sara@ascent.local", "Triglav via Kredarica", 5, "Steep, exposed, unforgettable."),
        };

        var trailReviews = reviews
            .Where(r => users.ContainsKey(r.User))
            .Select(r => new Review
            {
                UserId = users[r.User].Id,
                TrailId = Trail(r.Trail),
                Rating = r.Rating,
                Comment = r.Comment,
                CreatedAt = DateTime.UtcNow,
            });
        await context.Reviews.AddRangeAsync(trailReviews);

        await context.SaveChangesAsync();
    }

    private sealed class SeedTrail
    {
        public string Name { get; set; } = "";
        public string Region { get; set; } = "";
        public string Country { get; set; } = "";
        public string Short { get; set; } = "";
        public double DistanceKm { get; set; }
        public int ElevationGainM { get; set; }
        public int? TrailheadEle { get; set; }
        public int? SummitEle { get; set; }
        public long OsmRelationId { get; set; }
        public string? PhotoUrl { get; set; }
        public List<string>? Photos { get; set; }
        public JsonElement Route { get; set; }
    }

    private static List<SeedTrail> LoadSeedTrails(string contentRoot)
    {
        var path = Path.Combine(contentRoot, "Data", "seed", "trails.json");
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<List<SeedTrail>>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
    }

    private static async Task SeedTrailsAsync(AscentDbContext context, string? authorId, List<SeedTrail> seeds)
    {
        if (await context.Trails.AnyAsync()) return;

        var regionIds = await context.Regions
            .ToDictionaryAsync(r => $"{r.Country}|{r.Name}", r => r.Id);
        var difficulties = await context.Difficulties.OrderBy(d => d.MinScore).ToListAsync();

        int DifficultyId(double distanceKm, int gainM)
        {
            var score = Math.Sqrt(2 * gainM * distanceKm);
            var bin = difficulties.First(d =>
                score >= d.MinScore && (d.MaxScore == null || score < d.MaxScore));
            return bin.Id;
        }

        foreach (var s in seeds)
        {
            var coords = s.Route.GetProperty("coordinates");
            var head = coords[0];
            double lng = head[0].GetDouble(), lat = head[1].GetDouble();
            var climb = s.TrailheadEle is int lo && s.SummitEle is int hi
                ? $" Climbs from about {lo} m to {hi} m."
                : "";
            context.Trails.Add(new Trail
            {
                Name = s.Name,
                ShortDescription = s.Short,
                Description = $"{s.Short} A {s.DistanceKm} km marked route in the {s.Region} region " +
                              $"with about {s.ElevationGainM} m of climbing.{climb} Route geometry from OpenStreetMap.",
                DistanceKm = s.DistanceKm,
                ElevationGainM = s.ElevationGainM,
                Latitude = lat,
                Longitude = lng,
                PhotoUrl = s.PhotoUrl,
                RouteGeoJson = s.Route.GetRawText(),
                RegionId = regionIds[$"{s.Country}|{s.Region}"],
                DifficultyId = DifficultyId(s.DistanceKm, s.ElevationGainM),
                AuthorId = authorId,
                CreatedAt = DateTime.UtcNow,
            });
        }

        await context.SaveChangesAsync();
    }

    private static async Task SeedTrailPhotosAsync(AscentDbContext context, List<SeedTrail> seeds)
    {
        if (await context.TrailPhotos.AnyAsync()) return;

        var trailIds = await context.Trails.ToDictionaryAsync(t => t.Name, t => t.Id);
        foreach (var s in seeds)
        {
            if (s.Photos is null || !trailIds.TryGetValue(s.Name, out var trailId)) continue;
            foreach (var url in s.Photos)
                context.TrailPhotos.Add(new TrailPhoto { TrailId = trailId, Url = url });
        }

        await context.SaveChangesAsync();
    }
}
