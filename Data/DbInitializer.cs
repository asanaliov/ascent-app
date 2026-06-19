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

        var guide = await userManager.FindByEmailAsync("guide@ascent.local");
        await SeedTrailsAsync(context, guide?.Id);
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
