using ascent_app.Models;

namespace ascent_app.ViewModels;

public class DashboardViewModel
{
    public string DisplayName { get; set; } = "";

    public int TotalHikes { get; set; }
    public int TotalElevationM { get; set; }
    public double TotalDistanceKm { get; set; }
    public int DistinctRegions { get; set; }
    public int ReviewsWritten { get; set; }
    public int SavedTrails { get; set; }

    public List<HikeLog> RecentHikes { get; set; } = new();
    public List<BadgeProgress> Badges { get; set; } = new();

    public int BadgesEarned => Badges.Count(b => b.Earned);
}

// one badge + how close the user is to it
public class BadgeProgress
{
    public Badge Badge { get; set; } = null!;
    public bool Earned { get; set; }
    public int Current { get; set; }
    public int Threshold { get; set; }

    public int Percent => Threshold <= 0 ? 100 : Math.Min(100, (int)(100.0 * Current / Threshold));
}
