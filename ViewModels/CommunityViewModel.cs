using ascent_app.Models;

namespace ascent_app.ViewModels;

public class CommunityViewModel
{
    public int TotalTrails { get; set; }
    public int TotalHikes { get; set; }
    public int TotalHikers { get; set; }
    public long TotalElevationM { get; set; }

    public List<ActivityItem> Activity { get; set; } = new();
    public List<HikeEvent> UpcomingEvents { get; set; } = new();
    public List<LeaderRow> TopHikers { get; set; } = new();
    public List<PopularTrail> PopularTrails { get; set; } = new();
}

// one tile in the community feed: a logged hike or a review, newest first
public class ActivityItem
{
    public string Kind { get; set; } = "hike";   // hike | review
    public DateTime At { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public Trail Trail { get; set; } = null!;
    public string PhotoUrl { get; set; } = string.Empty;
    public string? Text { get; set; }
    public int? Rating { get; set; }
    public int? DurationMinutes { get; set; }
}

public class LeaderRow
{
    public string UserId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public int Hikes { get; set; }
    public int ElevationM { get; set; }
    public double DistanceKm { get; set; }
}

public class PopularTrail
{
    public Trail Trail { get; set; } = null!;
    public int FavoriteCount { get; set; }
}
