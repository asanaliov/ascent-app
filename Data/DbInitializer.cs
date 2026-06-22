using System.Text.Json;
using System.Text.RegularExpressions;
using ascent_app.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ascent_app.Data;

public static class DbInitializer
{
    public static readonly string[] Roles = { "Hiker", "Guide", "Admin" };

    // Demo credentials are documented in the README, not here.
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

        var env = sp.GetRequiredService<IHostEnvironment>();
        var seedTrails = LoadSeedTrails(env.ContentRootPath);

        await SeedDifficultiesAsync(context);
        await SeedRegionsAsync(context, seedTrails);
        await SeedBadgesAsync(context);

        var guide = await userManager.FindByEmailAsync("guide@ascent.local");
        await SeedTrailsAsync(context, guide?.Id, seedTrails);
        await SeedTagsAsync(context);
        await SeedActivityAsync(context, userManager);
    }

    private static async Task EnsureUserAsync(
        UserManager<ApplicationUser> userManager,
        string email, string role, string password, string? displayName = null)
    {
        if (await userManager.FindByEmailAsync(email) is not null) return;

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            DisplayName = displayName ?? role,
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

    // regions are derived from the trail seed file — one per distinct (country, region)
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

    // demo hike logs + reviews so the leaderboard and profiles aren't empty
    private static async Task SeedActivityAsync(
        AscentDbContext context, UserManager<ApplicationUser> userManager)
    {
        if (await context.HikeLogs.AnyAsync()) return;

        var admin = await userManager.FindByEmailAsync("admin@ascent.local");
        var guide = await userManager.FindByEmailAsync("guide@ascent.local");
        if (admin is null || guide is null) return;

        var trails = await context.Trails.ToDictionaryAsync(t => t.Name, t => t.Id);
        int Trail(string name) => trails[name];

        // (userId, trailName, daysAgo, durationMinutes, notes)
        var logs = new (string UserId, string Trail, int DaysAgo, int? Duration, string? Notes)[]
        {
            (guide.Id, "Matka – Shishevo Monastery", 168, 110, "Led a small group along the canyon; calm water all morning."),
            (guide.Id, "Galičica G-2", 154, 95, "Easy warm-up loop through the meadows."),
            (guide.Id, "Popova Šapka – Titov Vrv", 140, 410, "Long but rewarding alpine push to the Šar high point."),
            (guide.Id, "Magaro Peak", 121, 200, "Twin views over Ohrid and Prespa were unreal."),
            (guide.Id, "Mount Korab", 98, 520, "Country's roof. Started before dawn."),
            (guide.Id, "Karanikolica Lake", 76, 165, "Short climb up to the cirque lake, windy on top."),
            (guide.Id, "Galičica H-6 Ridge", 54, 235, null),
            (guide.Id, "Plat Ridge", 33, 175, "Quick scouting run on the upper plateau."),
            (guide.Id, "Strezimir – Golem Korab", 12, 690, "Recon for a guided border traverse next month."),
            (admin.Id, "Galičica G-2", 132, 90, "Easy reset hike after work."),
            (admin.Id, "Matka – Shishevo Monastery", 110, 120, "Pushed the pace along the gorge today."),
            (admin.Id, "Magaro Peak", 88, 210, null),
            (admin.Id, "Galičica H-6 Ridge", 61, 240, "Quiet trail, saw a few horses."),
            (admin.Id, "Plat Ridge", 40, 185, "Packed lunch at the saddle."),
            (admin.Id, "Karanikolica Lake", 21, 170, "Clouds rolled in near the lake."),
            (admin.Id, "Popova Šapka – Titov Vrv", 6, 430, "Tough but cleared the summit before noon."),
        };

        var hikeLogs = logs.Select(l => new HikeLog
        {
            UserId = l.UserId,
            TrailId = Trail(l.Trail),
            HikedOn = DateTime.Today.AddDays(-l.DaysAgo),
            DurationMinutes = l.Duration,
            Notes = l.Notes,
            CreatedAt = DateTime.UtcNow,
        });
        await context.HikeLogs.AddRangeAsync(hikeLogs);

        // at most one review per (user, trail); ratings 3..5
        var reviews = new (string UserId, string Trail, int Rating, string? Comment)[]
        {
            (guide.Id, "Matka – Shishevo Monastery", 5, "Gorgeous canyon walk. Great for beginners too."),
            (guide.Id, "Popova Šapka – Titov Vrv", 5, "A serious day out but the views pay you back."),
            (guide.Id, "Magaro Peak", 4, "Stunning twin-lake panorama, just bring sun protection."),
            (guide.Id, "Mount Korab", 5, "Bucket-list summit. Long approach."),
            (guide.Id, "Galičica G-2", 4, "Family-friendly and scenic."),
            (admin.Id, "Matka – Shishevo Monastery", 4, "Busy on weekends but worth it."),
            (admin.Id, "Karanikolica Lake", 5, "Lovely little glacial lake under the peaks."),
            (admin.Id, "Galičica H-6 Ridge", 3, "Pleasant but a bit featureless in places."),
            (admin.Id, "Magaro Peak", 4, "Loved the view over both lakes."),
            (admin.Id, "Popova Šapka – Titov Vrv", 5, "Iconic summit, exposed ridge near the top."),
        };

        var trailReviews = reviews.Select(r => new Review
        {
            UserId = r.UserId,
            TrailId = Trail(r.Trail),
            Rating = r.Rating,
            Comment = r.Comment,
            CreatedAt = DateTime.UtcNow,
        });
        await context.Reviews.AddRangeAsync(trailReviews);

        await context.SaveChangesAsync();
    }

    // shape of each entry in Data/seed/trails.json (real OpenStreetMap routes)
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
        public JsonElement Route { get; set; } // GeoJSON LineString
    }

    // Real trails: geometry, distance and elevation all derived from OpenStreetMap
    // hiking-route relations + a DEM (see Data/seed/trails.json, generated offline).
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

        // key regions by country+name so identically named regions can't collide
        var regionIds = await context.Regions
            .ToDictionaryAsync(r => $"{r.Country}|{r.Name}", r => r.Id);
        var difficulties = await context.Difficulties.OrderBy(d => d.MinScore).ToListAsync();

        // resolve the difficulty bin from the same formula DifficultyService uses
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
            var head = coords[0]; // GeoJSON order is [lng, lat]; first point = trailhead
            double lng = head[0].GetDouble(), lat = head[1].GetDouble();
            var slug = Regex.Replace(s.Name.ToLowerInvariant(), "[^a-z0-9]+", "-").Trim('-');
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
                PhotoUrl = $"https://picsum.photos/seed/{slug}/800/500",
                RouteGeoJson = s.Route.GetRawText(),
                RegionId = regionIds[$"{s.Country}|{s.Region}"],
                DifficultyId = DifficultyId(s.DistanceKm, s.ElevationGainM),
                AuthorId = authorId,
                CreatedAt = DateTime.UtcNow,
            });
        }

        await context.SaveChangesAsync();
    }
}
