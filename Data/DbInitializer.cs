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

        await SeedDifficultiesAsync(context);
        await SeedRegionsAsync(context);
        await SeedBadgesAsync(context);

        var guide = await userManager.FindByEmailAsync("guide@ascent.local");
        await SeedTrailsAsync(context, guide?.Id);
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

    private static async Task SeedRegionsAsync(AscentDbContext context)
    {
        if (await context.Regions.AnyAsync()) return;

        context.Regions.AddRange(
            new Region { Name = "Skopje" },
            new Region { Name = "Šar Planina" },
            new Region { Name = "Mavrovo" },
            new Region { Name = "Galičica" });

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
            ["Vodno – Millennium Cross"] = new[] { "Summit", "Forest", "Panoramic" },
            ["Matka Canyon Loop"] = new[] { "Loop", "Lake", "Family-friendly" },
            ["Titov Vrv"] = new[] { "Summit", "Alpine", "Panoramic" },
            ["Galičica Ridge"] = new[] { "Panoramic", "Lake" },
            ["Mt. Korab Summit"] = new[] { "Summit", "Alpine" },
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
            (guide.Id, "Vodno – Millennium Cross", 168, 150, "Led a small group up; clear skies all morning."),
            (guide.Id, "Matka Canyon Loop", 154, 190, "Boat tour after the loop, lovely day."),
            (guide.Id, "Titov Vrv", 140, 410, "Long but rewarding alpine push."),
            (guide.Id, "Galičica Ridge", 121, 240, "Twin-lake views were unreal."),
            (guide.Id, "Mt. Korab Summit", 98, 520, "Country's roof. Started before dawn."),
            (guide.Id, "Ljuboten Peak", 76, 360, "Windy on the ridge, great pyramid summit."),
            (guide.Id, "Bistra Ridge", 54, 210, null),
            (guide.Id, "Vodno – Middle Peak", 33, 95, "Quick afternoon scouting run."),
            (guide.Id, "Magaro Peak", 12, 220, "Recon for a guided trip next month."),
            (admin.Id, "Vodno – Middle Peak", 132, 90, "Easy reset hike after work."),
            (admin.Id, "Vodno – Millennium Cross", 110, 165, "Pushed the pace today."),
            (admin.Id, "Matka Canyon Loop", 88, 180, null),
            (admin.Id, "Bistra Ridge", 61, 200, "Quiet trail, saw a few horses."),
            (admin.Id, "Galičica Ridge", 40, 250, "Packed lunch at the saddle."),
            (admin.Id, "Magaro Peak", 21, 215, "Clouds rolled in near the top."),
            (admin.Id, "Ljuboten Peak", 6, 375, "Tough but cleared the summit before noon."),
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
            (guide.Id, "Vodno – Millennium Cross", 5, "Best city-edge climb around. Great for beginners too."),
            (guide.Id, "Titov Vrv", 5, "A serious day out but the views pay you back."),
            (guide.Id, "Galičica Ridge", 4, "Stunning ridge, just bring sun protection."),
            (guide.Id, "Mt. Korab Summit", 5, "Bucket-list summit. Long approach."),
            (guide.Id, "Matka Canyon Loop", 4, "Family-friendly and scenic."),
            (admin.Id, "Vodno – Millennium Cross", 4, "Busy on weekends but worth it."),
            (admin.Id, "Matka Canyon Loop", 5, "Gorgeous gorge, easy underfoot."),
            (admin.Id, "Bistra Ridge", 3, "Pleasant but a bit featureless in places."),
            (admin.Id, "Galičica Ridge", 4, "Loved the twin-lake panorama."),
            (admin.Id, "Ljuboten Peak", 5, "Iconic summit, exposed ridge near the top."),
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

    private static async Task SeedTrailsAsync(AscentDbContext context, string? authorId)
    {
        if (await context.Trails.AnyAsync()) return;

        var regionIds = await context.Regions.ToDictionaryAsync(r => r.Name, r => r.Id);
        var difficulties = await context.Difficulties.OrderBy(d => d.MinScore).ToListAsync();

        // resolve the difficulty bin from the same formula DifficultyService uses
        int DifficultyId(double distanceKm, int gainM)
        {
            var score = Math.Sqrt(2 * gainM * distanceKm);
            var bin = difficulties.First(d =>
                score >= d.MinScore && (d.MaxScore == null || score < d.MaxScore));
            return bin.Id;
        }

        var samples = new (string Name, string Region, string Short, double Dist, int Gain, double Lat, double Lon)[]
        {
            ("Vodno – Millennium Cross", "Skopje", "City-edge climb to the giant cross above Skopje.", 6.5, 580, 41.9628, 21.4280),
            ("Matka Canyon Loop", "Skopje", "Riverside path through a dramatic limestone gorge.", 8.0, 350, 41.9560, 21.3010),
            ("Vodno – Middle Peak", "Skopje", "Shorter forest route to the lower Vodno summit.", 4.0, 300, 41.9700, 21.4330),
            ("Titov Vrv", "Šar Planina", "Long alpine ascent to the highest peak of the Šar range.", 15.0, 1300, 41.9900, 20.8300),
            ("Ljuboten Peak", "Šar Planina", "Iconic pyramid summit with sweeping ridge views.", 12.0, 1150, 42.1800, 21.1300),
            ("Mt. Korab Summit", "Mavrovo", "The country's highest point, on the Albanian border.", 18.0, 1600, 41.7900, 20.5500),
            ("Bistra Ridge", "Mavrovo", "Rolling highland ridge above Mavrovo lake.", 11.0, 700, 41.6500, 20.7500),
            ("Galičica Ridge", "Galičica", "Panoramic ridge walk between Ohrid and Prespa lakes.", 10.0, 700, 40.9500, 20.8300),
            ("Magaro Peak", "Galičica", "Top of the Galičica massif with twin-lake views.", 9.0, 600, 40.9600, 20.8100),
        };

        foreach (var s in samples)
        {
            var slug = s.Name.ToLowerInvariant().Replace(' ', '-').Replace("–", "").Replace(".", "");
            context.Trails.Add(new Trail
            {
                Name = s.Name,
                ShortDescription = s.Short,
                Description = $"{s.Short} A {s.Dist} km route in the {s.Region} region with about {s.Gain} m of climbing.",
                DistanceKm = s.Dist,
                ElevationGainM = s.Gain,
                Latitude = s.Lat,
                Longitude = s.Lon,
                PhotoUrl = $"https://picsum.photos/seed/{slug}/800/500",
                RegionId = regionIds[s.Region],
                DifficultyId = DifficultyId(s.Dist, s.Gain),
                AuthorId = authorId,
                CreatedAt = DateTime.UtcNow,
            });
        }

        await context.SaveChangesAsync();
    }
}
