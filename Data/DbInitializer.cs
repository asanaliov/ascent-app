using ascent_app.Models;
using ascent_app.Services;
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

        IReadOnlyList<ExternalTrail> externalTrails = Array.Empty<ExternalTrail>();
        if (!await context.Trails.AnyAsync())
        {
            var trailSource = sp.GetRequiredService<IExternalTrailSource>();
            externalTrails = await trailSource.GetTrailsAsync();
        }

        await SeedDifficultiesAsync(context);
        await SeedRegionsAsync(context, externalTrails);
        await SeedBadgesAsync(context);

        var guide = await userManager.FindByEmailAsync("guide@ascent.local");
        await SeedTrailsAsync(context, guide?.Id, externalTrails);
        await SeedTagsAsync(context, externalTrails);
        await EnrichTrailPhotosAsync(context, sp.GetRequiredService<ITrailPhotoSource>());
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

    private static async Task SeedRegionsAsync(AscentDbContext context, IReadOnlyList<ExternalTrail> trails)
    {
        if (await context.Regions.AnyAsync()) return;

        var regions = trails
            .GroupBy(t => (t.Country, t.Region))
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

    private static async Task SeedTagsAsync(AscentDbContext context, IReadOnlyList<ExternalTrail> externalTrails)
    {
        if (await context.Tags.AnyAsync()) return;

        var names = externalTrails
            .SelectMany(t => t.Tags)
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(t => t)
            .ToList();
        var tags = names.Select(n => new Tag { Name = n }).ToList();
        context.Tags.AddRange(tags);
        await context.SaveChangesAsync();

        var tagIds = tags.ToDictionary(t => t.Name, t => t.Id, StringComparer.OrdinalIgnoreCase);
        var tagMap = externalTrails.ToDictionary(t => t.Name, t => t.Tags, StringComparer.OrdinalIgnoreCase);
        var trails = await context.Trails.ToListAsync();
        foreach (var trail in trails.Where(t => tagMap.ContainsKey(t.Name)))
            foreach (var tagName in tagMap[trail.Name].Where(tagIds.ContainsKey))
                context.TrailTags.Add(new TrailTag { TrailId = trail.Id, TagId = tagIds[tagName] });

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

        var trailIds = await context.Trails
            .OrderBy(t => t.Name)
            .Select(t => t.Id)
            .ToListAsync();
        if (trailIds.Count == 0) return;

        var emailsWithUsers = emails.Where(users.ContainsKey).ToList();
        var hikeLogs = emailsWithUsers
            .SelectMany((email, userIndex) => trailIds
                .Take(4)
                .Select((trailId, trailIndex) => new HikeLog
                {
                    UserId = users[email].Id,
                    TrailId = trailIds[(trailIndex + userIndex) % trailIds.Count],
                    HikedOn = DateTime.Today.AddDays(-20 - userIndex * 11 - trailIndex * 17),
                    DurationMinutes = 90 + trailIndex * 35,
                    Notes = trailIndex % 2 == 0 ? "Clear trail and great views." : null,
                    CreatedAt = DateTime.UtcNow,
                }));
        await context.HikeLogs.AddRangeAsync(hikeLogs);

        var trailReviews = emailsWithUsers
            .SelectMany((email, userIndex) => trailIds
                .Take(2)
                .Select((trailId, trailIndex) => new Review
                {
                    UserId = users[email].Id,
                    TrailId = trailIds[(trailIndex + userIndex) % trailIds.Count],
                    Rating = 4 + (trailIndex + userIndex) % 2,
                    Comment = trailIndex == 0 ? "Beautiful route and worth repeating." : "Good day out with solid views.",
                    CreatedAt = DateTime.UtcNow,
                }))
            .GroupBy(r => new { r.UserId, r.TrailId })
            .Select(g => g.First());
        await context.Reviews.AddRangeAsync(trailReviews);

        await context.SaveChangesAsync();
    }

    private static async Task SeedTrailsAsync(AscentDbContext context, string? authorId, IReadOnlyList<ExternalTrail> externalTrails)
    {
        if (await context.Trails.AnyAsync()) return;
        if (externalTrails.Count == 0) return;

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

        foreach (var external in externalTrails)
        {
            context.Trails.Add(new Trail
            {
                Name = external.Name,
                ShortDescription = external.ShortDescription,
                Description = $"{external.ShortDescription} Route data imported from OpenStreetMap.",
                DistanceKm = external.DistanceKm,
                ElevationGainM = external.ElevationGainM,
                Latitude = external.Latitude,
                Longitude = external.Longitude,
                PhotoUrl = external.PhotoUrl,
                RouteGeoJson = external.RouteGeoJson,
                RegionId = regionIds[$"{external.Country}|{external.Region}"],
                DifficultyId = DifficultyId(external.DistanceKm, external.ElevationGainM),
                AuthorId = authorId,
                CreatedAt = DateTime.UtcNow,
            });
        }

        await context.SaveChangesAsync();
    }

    private static async Task EnrichTrailPhotosAsync(AscentDbContext context, ITrailPhotoSource photoSource)
    {
        var trails = await context.Trails
            .Include(t => t.Region)
            .Where(t => string.IsNullOrWhiteSpace(t.PhotoUrl))
            .OrderBy(t => t.Name)
            .Take(16)
            .ToListAsync();

        foreach (var trail in trails)
        {
            var photoUrl = await photoSource.FindPhotoAsync(trail);
            if (!string.IsNullOrWhiteSpace(photoUrl))
                trail.PhotoUrl = photoUrl;
        }

        await context.SaveChangesAsync();
    }
}
