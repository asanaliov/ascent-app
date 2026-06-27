using System.Text.Json;

namespace ascent_app.Data.Seeders;

internal sealed class TrailSeed
{
    public string SeedKey { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public List<string> LegacyNames { get; set; } = new();
    public string Region { get; set; } = string.Empty;
    public string Country { get; set; } = "North Macedonia";
    public string Difficulty { get; set; } = string.Empty;
    public double DistanceKm { get; set; }
    public int ElevationGainM { get; set; }
    public double EstimatedTimeHours { get; set; }
    public string ShortDescription { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public double StartLatitude { get; set; }
    public double StartLongitude { get; set; }
    public JsonElement? RouteGeometry { get; set; }
    public string Source { get; set; } = "Ascent curated seed";
    public List<string> Tags { get; set; } = new();
    public List<TrailImageSeed> Images { get; set; } = new();
}

internal sealed class TrailImageSeed
{
    public string ImageUrl { get; set; } = string.Empty;
    public string? Caption { get; set; }
    public bool IsCoverImage { get; set; }
}

internal sealed class DemoUserSeed
{
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = "Hiker";
    public string ProfileImageUrl { get; set; } = string.Empty;
    public string ExperienceLevel { get; set; } = "Intermediate";
    public string? Bio { get; set; }
    public double? HomeLatitude { get; set; }
    public double? HomeLongitude { get; set; }
    public string? HomeLocationName { get; set; }
    public DateTime CreatedAt { get; set; }
}

internal sealed class DemoReviewSeed
{
    public string UserEmail { get; set; } = string.Empty;
    public string TrailKey { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; }
}

internal sealed class DemoFavoriteSeed
{
    public string UserEmail { get; set; } = string.Empty;
    public string TrailKey { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

internal sealed class DemoHikeLogSeed
{
    public string UserEmail { get; set; } = string.Empty;
    public string TrailKey { get; set; } = string.Empty;
    public DateTime HikedOn { get; set; }
    public int? DurationMinutes { get; set; }
    public string? Notes { get; set; }
}

internal sealed class DemoHikeEventSeed
{
    public string Title { get; set; } = string.Empty;
    public string GuideEmail { get; set; } = string.Empty;
    public string TrailKey { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime StartsAt { get; set; }
    public int MaxParticipants { get; set; }
    public string? MeetingPoint { get; set; }
}
