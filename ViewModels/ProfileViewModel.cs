using ascent_app.Models;

namespace ascent_app.ViewModels;

// public hiker profile — read-only view of who they are + their activity
public class ProfileViewModel
{
    public string UserId { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;
    public string? Bio { get; set; }
    public DateTime MemberSince { get; set; }

    // aggregates over all hikes
    public int TotalHikes { get; set; }
    public double TotalDistanceKm { get; set; }
    public int TotalElevationM { get; set; }
    public int RegionsCount { get; set; }
    public int ReviewsCount { get; set; }

    public List<Badge> Badges { get; set; } = new();
    public List<HikeLog> RecentHikes { get; set; } = new();
}
