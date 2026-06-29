using System.Globalization;
using System.Text;
using System.Text.Json;
using ascent_app.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ascent_app.Data.Seeders;

public static class DatabaseSeeder
{
    public const string DemoPassword = "Demo123!";
    public static readonly string[] Roles = ["Admin", "Guide", "Hiker"];

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public static async Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var provider = scope.ServiceProvider;
        var context = provider.GetRequiredService<AscentDbContext>();
        var environment = provider.GetRequiredService<IWebHostEnvironment>();
        var configuration = provider.GetRequiredService<IConfiguration>();
        var roleManager = provider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = provider.GetRequiredService<UserManager<ApplicationUser>>();
        var logger = provider.GetRequiredService<ILoggerFactory>()
            .CreateLogger("DatabaseSeeder");

        await context.Database.MigrateAsync(cancellationToken);
        await SeedRolesAsync(roleManager);
        await SeedLookupsAsync(context, cancellationToken);

        var seedTrails = configuration.GetValue(
            "DatabaseSeeding:SeedTrails",
            environment.IsDevelopment());

        if (seedTrails)
        {
            var trails = await ReadSeedFileAsync<TrailSeed>(
                environment, "macedonia-trails.json", cancellationToken);
            await SeedTrailsAsync(
                context, trails, environment.WebRootPath, cancellationToken);
        }

        var seedDemoData = environment.IsDevelopment()
                           && configuration.GetValue("DatabaseSeeding:SeedDemoData", true);
        if (!seedDemoData)
            return;

        if (configuration.GetValue("DatabaseSeeding:RemoveLegacyImportedTrails", true))
        {
            var legacyTrailNames = await ReadSeedFileAsync<string>(
                environment, "legacy-imported-trails.json", cancellationToken);
            await RemoveLegacyImportedTrailsAsync(
                context, legacyTrailNames, logger, cancellationToken);
        }

        var users = await ReadSeedFileAsync<DemoUserSeed>(
            environment, "demo-users.json", cancellationToken);
        await SeedDemoUsersAsync(userManager, users);

        await SeedReviewsAsync(
            context,
            await ReadSeedFileAsync<DemoReviewSeed>(
                environment, "demo-reviews.json", cancellationToken),
            cancellationToken);
        await SeedFavoritesAsync(
            context,
            await ReadSeedFileAsync<DemoFavoriteSeed>(
                environment, "demo-favorites.json", cancellationToken),
            cancellationToken);
        await SeedHikeLogsAsync(
            context,
            await ReadSeedFileAsync<DemoHikeLogSeed>(
                environment, "demo-hike-logs.json", cancellationToken),
            cancellationToken);
        await SeedHikeEventsAsync(
            context,
            await ReadSeedFileAsync<DemoHikeEventSeed>(
                environment, "demo-hike-events.json", cancellationToken),
            cancellationToken);

        logger.LogInformation(
            "Development seed ready: {Trails} trails, {TrailImages} trail images, " +
            "{DemoUsers} demo users, {Reviews} reviews, {Favorites} favorites, " +
            "{HikeLogs} hike logs, and {HikeEvents} hike events.",
            await context.Trails.CountAsync(t => t.IsSeedData, cancellationToken),
            await context.TrailPhotos.CountAsync(cancellationToken),
            await context.Users.CountAsync(u => u.IsDemoUser, cancellationToken),
            await context.Reviews.CountAsync(r => r.IsDemoData, cancellationToken),
            await context.Favorites.CountAsync(f => f.IsDemoData, cancellationToken),
            await context.HikeLogs.CountAsync(h => h.IsDemoData, cancellationToken),
            await context.HikeEvents.CountAsync(e => e.IsDemoData, cancellationToken));
    }

    private static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
    {
        foreach (var roleName in Roles)
        {
            if (await roleManager.RoleExistsAsync(roleName))
                continue;

            var result = await roleManager.CreateAsync(new IdentityRole(roleName));
            EnsureIdentitySucceeded(result, $"create role '{roleName}'");
        }
    }

    private static async Task SeedLookupsAsync(
        AscentDbContext context,
        CancellationToken cancellationToken)
    {
        var difficulties = new[]
        {
            new Difficulty { Label = "Easy", MinScore = 0, MaxScore = 50, BadgeClass = "diff-easy" },
            new Difficulty { Label = "Moderate", MinScore = 50, MaxScore = 100, BadgeClass = "diff-moderate" },
            new Difficulty { Label = "Hard", MinScore = 100, MaxScore = 150, BadgeClass = "diff-hard" },
            new Difficulty { Label = "Strenuous", MinScore = 150, MaxScore = null, BadgeClass = "diff-strenuous" },
        };
        var existingDifficulties = (await context.Difficulties
            .Select(d => d.Label)
            .ToListAsync(cancellationToken))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        await context.Difficulties.AddRangeAsync(
            difficulties.Where(d => !existingDifficulties.Contains(d.Label)),
            cancellationToken);

        var badges = new[]
        {
            new Badge { Name = "First Steps", Description = "Logged your first hike.", IconName = "ti-shoe", Criteria = BadgeCriteria.HikeCount, Threshold = 1 },
            new Badge { Name = "Trailblazer", Description = "Logged 10 hikes.", IconName = "ti-flame", Criteria = BadgeCriteria.HikeCount, Threshold = 10 },
            new Badge { Name = "Summit Seeker", Description = "Climbed 5,000 m in total.", IconName = "ti-mountain", Criteria = BadgeCriteria.CumulativeElevation, Threshold = 5000 },
            new Badge { Name = "Explorer", Description = "Hiked in 3 different regions.", IconName = "ti-map-2", Criteria = BadgeCriteria.DistinctRegions, Threshold = 3 },
        };
        var existingBadges = (await context.Badges
            .Select(b => b.Name)
            .ToListAsync(cancellationToken))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        await context.Badges.AddRangeAsync(
            badges.Where(b => !existingBadges.Contains(b.Name)),
            cancellationToken);

        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedTrailsAsync(
        AscentDbContext context,
        IReadOnlyCollection<TrailSeed> seeds,
        string webRootPath,
        CancellationToken cancellationToken)
    {
        var difficulties = await context.Difficulties
            .ToDictionaryAsync(d => d.Label, StringComparer.OrdinalIgnoreCase, cancellationToken);
        var regions = await context.Regions.ToListAsync(cancellationToken);
        var tags = await context.Tags.ToListAsync(cancellationToken);
        var trails = await context.Trails
            .Include(t => t.Photos)
            .Include(t => t.TrailTags)
            .AsSplitQuery()
            .ToListAsync(cancellationToken);

        foreach (var seed in seeds)
        {
            var region = regions.FirstOrDefault(r =>
                FoldName(r.Name).Equals(
                    FoldName(seed.Region), StringComparison.OrdinalIgnoreCase)
                && r.Country.Equals(seed.Country, StringComparison.OrdinalIgnoreCase));
            if (region is null)
            {
                region = new Region { Name = seed.Region, Country = seed.Country };
                regions.Add(region);
                context.Regions.Add(region);
            }
            else
            {
                region.Name = seed.Region;
            }

            if (!difficulties.TryGetValue(seed.Difficulty, out var difficulty))
                throw new InvalidOperationException($"Unknown difficulty '{seed.Difficulty}' for '{seed.Name}'.");

            if (string.IsNullOrWhiteSpace(seed.SeedKey))
                throw new InvalidDataException($"Trail '{seed.Name}' has no seedKey.");

            var trail = trails.FirstOrDefault(t =>
                            seed.SeedKey.Equals(t.SeedKey, StringComparison.OrdinalIgnoreCase))
                        ?? trails.FirstOrDefault(t =>
                            t.IsSeedData
                            && (t.Name.Equals(seed.Name, StringComparison.OrdinalIgnoreCase)
                                || seed.LegacyNames.Contains(
                                    t.Name, StringComparer.OrdinalIgnoreCase)))
                        ?? trails.FirstOrDefault(t =>
                            t.Name.Equals(seed.Name, StringComparison.OrdinalIgnoreCase));
            if (trail is null)
            {
                trail = new Trail
                {
                    CreatedAt = new DateTime(2026, 1, 1, 8, 0, 0, DateTimeKind.Utc),
                };
                trails.Add(trail);
                context.Trails.Add(trail);
            }

            trail.Name = seed.Name;
            trail.SeedKey = seed.SeedKey;
            trail.ShortDescription = seed.ShortDescription;
            trail.Description = seed.Description;
            trail.DistanceKm = seed.DistanceKm;
            trail.ElevationGainM = seed.ElevationGainM;
            trail.EstimatedTimeHours = seed.EstimatedTimeHours;
            trail.Latitude = seed.StartLatitude;
            trail.Longitude = seed.StartLongitude;
            trail.RouteGeoJson = seed.RouteGeometry?.GetRawText();
            trail.Region = region;
            trail.Difficulty = difficulty;
            trail.Source = seed.Source;
            trail.IsSeedData = true;
            trail.PhotoUrl = null;

            var imageSeeds = seed.Images
                .Where(image => LocalAssetExists(webRootPath, image.ImageUrl))
                .ToList();
            var imageUrls = imageSeeds
                .Select(image => image.ImageUrl)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var staleSeedPhotos = trail.Photos
                .Where(photo => IsManagedSeedImage(photo.Url)
                                && !imageUrls.Contains(photo.Url))
                .ToList();
            context.TrailPhotos.RemoveRange(staleSeedPhotos);

            foreach (var imageSeed in imageSeeds)
            {
                var photo = trail.Photos.FirstOrDefault(p =>
                    p.Url.Equals(imageSeed.ImageUrl, StringComparison.OrdinalIgnoreCase));
                if (photo is not null)
                {
                    photo.Caption = imageSeed.Caption;
                    photo.IsCoverImage = imageSeed.IsCoverImage;
                    continue;
                }

                trail.Photos.Add(new TrailPhoto
                {
                    Url = imageSeed.ImageUrl,
                    Caption = imageSeed.Caption,
                    IsCoverImage = imageSeed.IsCoverImage,
                    UploadedAt = new DateTime(2026, 1, 1, 8, 0, 0, DateTimeKind.Utc),
                });
            }

            foreach (var tagName in seed.Tags.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                var tag = tags.FirstOrDefault(t =>
                    FoldName(t.Name).Equals(
                        FoldName(tagName), StringComparison.OrdinalIgnoreCase));
                if (tag is null)
                {
                    tag = new Tag { Name = tagName };
                    tags.Add(tag);
                    context.Tags.Add(tag);
                }
                else
                {
                    tag.Name = tagName;
                }

                if (trail.TrailTags.All(tt => tt.Tag != tag && tt.TagId != tag.Id))
                    trail.TrailTags.Add(new TrailTag { Tag = tag });
            }
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private static bool LocalAssetExists(string webRootPath, string imageUrl)
    {
        if (string.IsNullOrWhiteSpace(webRootPath)
            || !imageUrl.StartsWith('/')
            || imageUrl.Contains("..", StringComparison.Ordinal))
            return false;

        var root = Path.GetFullPath(webRootPath);
        var relativePath = imageUrl.TrimStart('/')
            .Replace('/', Path.DirectorySeparatorChar);
        var assetPath = Path.GetFullPath(Path.Combine(root, relativePath));
        var rootPrefix = root.TrimEnd(Path.DirectorySeparatorChar)
                         + Path.DirectorySeparatorChar;

        return assetPath.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase)
               && File.Exists(assetPath);
    }

    private static bool IsManagedSeedImage(string imageUrl) =>
        imageUrl.StartsWith("/images/trails/", StringComparison.OrdinalIgnoreCase)
        || imageUrl.StartsWith("/img/trails/", StringComparison.OrdinalIgnoreCase);

    private static async Task SeedDemoUsersAsync(
        UserManager<ApplicationUser> userManager,
        IReadOnlyCollection<DemoUserSeed> seeds)
    {
        foreach (var seed in seeds)
        {
            var user = await userManager.FindByEmailAsync(seed.Email);
            if (user is null)
            {
                user = new ApplicationUser
                {
                    UserName = seed.Email,
                    Email = seed.Email,
                    EmailConfirmed = true,
                    DisplayName = seed.FullName,
                    ProfileImageUrl = seed.ProfileImageUrl,
                    ExperienceLevel = seed.ExperienceLevel,
                    IsDemoUser = true,
                    CreatedAt = seed.CreatedAt,
                };
                var createResult = await userManager.CreateAsync(user, DemoPassword);
                EnsureIdentitySucceeded(createResult, $"create demo user '{seed.Email}'");
            }

            user.DisplayName = seed.FullName;
            user.Bio = seed.Bio;
            user.ProfileImageUrl = seed.ProfileImageUrl;
            user.ExperienceLevel = seed.ExperienceLevel;
            user.IsDemoUser = true;
            user.HomeLat = seed.HomeLatitude;
            user.HomeLng = seed.HomeLongitude;
            user.HomeLocationName = seed.HomeLocationName;
            var updateResult = await userManager.UpdateAsync(user);
            EnsureIdentitySucceeded(updateResult, $"update demo user '{seed.Email}'");

            if (!await userManager.IsInRoleAsync(user, seed.Role))
            {
                var roleResult = await userManager.AddToRoleAsync(user, seed.Role);
                EnsureIdentitySucceeded(roleResult, $"assign role '{seed.Role}' to '{seed.Email}'");
            }
        }
    }

    private static async Task RemoveLegacyImportedTrailsAsync(
        AscentDbContext context,
        IReadOnlyCollection<string> legacyTrailNames,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var names = legacyTrailNames.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var legacyTrails = await context.Trails
            .Where(t => !t.IsSeedData && t.SeedKey == null)
            .ToListAsync(cancellationToken);
        legacyTrails = legacyTrails
            .Where(t => names.Contains(t.Name))
            .ToList();
        if (legacyTrails.Count == 0)
            return;

        var trailIds = legacyTrails.Select(t => t.Id).ToList();
        context.Reviews.RemoveRange(
            await context.Reviews.Where(r => trailIds.Contains(r.TrailId))
                .ToListAsync(cancellationToken));
        context.Favorites.RemoveRange(
            await context.Favorites.Where(f => trailIds.Contains(f.TrailId))
                .ToListAsync(cancellationToken));
        context.HikeEvents.RemoveRange(
            await context.HikeEvents.Where(e => trailIds.Contains(e.TrailId))
                .ToListAsync(cancellationToken));
        context.HikeLogs.RemoveRange(
            await context.HikeLogs.Where(h => trailIds.Contains(h.TrailId))
                .ToListAsync(cancellationToken));
        context.Trails.RemoveRange(legacyTrails);

        await context.SaveChangesAsync(cancellationToken);
        logger.LogInformation(
            "Removed {Count} legacy externally imported development trails.",
            legacyTrails.Count);
    }

    private static async Task SeedReviewsAsync(
        AscentDbContext context,
        IReadOnlyCollection<DemoReviewSeed> seeds,
        CancellationToken cancellationToken)
    {
        var references = await LoadActivityReferencesAsync(context, cancellationToken);
        var existing = await context.Reviews
            .Select(r => new { r.UserId, r.TrailId })
            .ToListAsync(cancellationToken);
        var keys = existing.Select(x => $"{x.UserId}|{x.TrailId}").ToHashSet();

        foreach (var seed in seeds)
        {
            var (user, trail) = ResolveActivityReferences(seed.UserEmail, seed.TrailKey, references);
            if (!keys.Add($"{user.Id}|{trail.Id}"))
                continue;

            context.Reviews.Add(new Review
            {
                UserId = user.Id,
                TrailId = trail.Id,
                Rating = seed.Rating,
                Comment = seed.Comment,
                CreatedAt = seed.CreatedAt,
                IsDemoData = true,
            });
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedFavoritesAsync(
        AscentDbContext context,
        IReadOnlyCollection<DemoFavoriteSeed> seeds,
        CancellationToken cancellationToken)
    {
        var references = await LoadActivityReferencesAsync(context, cancellationToken);
        var keys = (await context.Favorites
                .Select(f => new { f.UserId, f.TrailId })
                .ToListAsync(cancellationToken))
            .Select(x => $"{x.UserId}|{x.TrailId}")
            .ToHashSet();

        foreach (var seed in seeds)
        {
            var (user, trail) = ResolveActivityReferences(seed.UserEmail, seed.TrailKey, references);
            if (!keys.Add($"{user.Id}|{trail.Id}"))
                continue;

            context.Favorites.Add(new Favorite
            {
                UserId = user.Id,
                TrailId = trail.Id,
                CreatedAt = seed.CreatedAt,
                IsDemoData = true,
            });
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedHikeLogsAsync(
        AscentDbContext context,
        IReadOnlyCollection<DemoHikeLogSeed> seeds,
        CancellationToken cancellationToken)
    {
        var references = await LoadActivityReferencesAsync(context, cancellationToken);
        var keys = (await context.HikeLogs
                .Select(h => new { h.UserId, h.TrailId, h.HikedOn })
                .ToListAsync(cancellationToken))
            .Select(x => $"{x.UserId}|{x.TrailId}|{x.HikedOn.Ticks}")
            .ToHashSet();

        foreach (var seed in seeds)
        {
            var (user, trail) = ResolveActivityReferences(seed.UserEmail, seed.TrailKey, references);
            if (!keys.Add($"{user.Id}|{trail.Id}|{seed.HikedOn.Ticks}"))
                continue;

            context.HikeLogs.Add(new HikeLog
            {
                UserId = user.Id,
                TrailId = trail.Id,
                HikedOn = seed.HikedOn,
                DurationMinutes = seed.DurationMinutes,
                Notes = seed.Notes,
                CreatedAt = seed.HikedOn.AddHours(12),
                IsDemoData = true,
            });
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedHikeEventsAsync(
        AscentDbContext context,
        IReadOnlyCollection<DemoHikeEventSeed> seeds,
        CancellationToken cancellationToken)
    {
        var references = await LoadActivityReferencesAsync(context, cancellationToken);
        var keys = (await context.HikeEvents
                .Select(e => new { e.Title, e.StartsAt })
                .ToListAsync(cancellationToken))
            .Select(x => $"{x.Title}|{x.StartsAt.Ticks}")
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var seed in seeds)
        {
            var (guide, trail) = ResolveActivityReferences(
                seed.GuideEmail, seed.TrailKey, references);
            if (!keys.Add($"{seed.Title}|{seed.StartsAt.Ticks}"))
                continue;

            context.HikeEvents.Add(new HikeEvent
            {
                Title = seed.Title,
                GuideId = guide.Id,
                TrailId = trail.Id,
                Description = seed.Description,
                StartsAt = seed.StartsAt,
                MaxParticipants = seed.MaxParticipants,
                MeetingPoint = seed.MeetingPoint,
                IsDemoData = true,
            });
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task<ActivityReferences> LoadActivityReferencesAsync(
        AscentDbContext context,
        CancellationToken cancellationToken)
    {
        var users = await context.Users
            .Where(u => u.IsDemoUser)
            .ToDictionaryAsync(u => u.Email!, StringComparer.OrdinalIgnoreCase, cancellationToken);
        var trails = await context.Trails
            .Where(t => t.IsSeedData && t.SeedKey != null)
            .ToDictionaryAsync(t => t.SeedKey!, StringComparer.OrdinalIgnoreCase, cancellationToken);
        return new ActivityReferences(users, trails);
    }

    private static (ApplicationUser User, Trail Trail) ResolveActivityReferences(
        string email,
        string trailKey,
        ActivityReferences references)
    {
        if (!references.Users.TryGetValue(email, out var user))
            throw new InvalidOperationException($"Demo user '{email}' was not found.");
        if (!references.Trails.TryGetValue(trailKey, out var trail))
            throw new InvalidOperationException($"Seed trail key '{trailKey}' was not found.");
        return (user, trail);
    }

    private static async Task<IReadOnlyCollection<T>> ReadSeedFileAsync<T>(
        IWebHostEnvironment environment,
        string fileName,
        CancellationToken cancellationToken)
    {
        var path = Path.Combine(environment.ContentRootPath, "Data", "seed", fileName);
        if (!File.Exists(path))
            throw new FileNotFoundException($"Required seed file was not found: {path}", path);

        await using var stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<List<T>>(
                   stream, JsonOptions, cancellationToken)
               ?? throw new InvalidDataException($"Seed file '{path}' contains no JSON array.");
    }

    private static void EnsureIdentitySucceeded(IdentityResult result, string operation)
    {
        if (result.Succeeded)
            return;

        var details = string.Join("; ", result.Errors.Select(e => e.Description));
        throw new InvalidOperationException($"Could not {operation}: {details}");
    }

    private static string FoldName(string value)
    {
        var decomposed = value.Normalize(NormalizationForm.FormD);
        var characters = decomposed.Where(c =>
            CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark);
        return new string(characters.ToArray()).Normalize(NormalizationForm.FormC);
    }

    private sealed record ActivityReferences(
        Dictionary<string, ApplicationUser> Users,
        Dictionary<string, Trail> Trails);
}
