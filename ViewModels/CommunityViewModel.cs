using ascent_app.Models;

namespace ascent_app.ViewModels;

public class CommunityViewModel
{
    public int TotalTrails { get; set; }
    public int TotalHikes { get; set; }
    public int TotalHikers { get; set; }
    public long TotalElevationM { get; set; }

    public List<LeaderRow> TopHikers { get; set; } = new();
    public List<PopularTrail> PopularTrails { get; set; } = new();
    public List<Trail> RecentTrails { get; set; } = new();
}

public class LeaderRow
{
    public string UserId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public int Hikes { get; set; }
    public int ElevationM { get; set; }
}

public class PopularTrail
{
    public Trail Trail { get; set; } = null!;
    public int FavoriteCount { get; set; }
}
