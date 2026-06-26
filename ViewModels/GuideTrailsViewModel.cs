using ascent_app.Models;

namespace ascent_app.ViewModels;

public class GuideTrailRow
{
    public Trail Trail { get; set; } = null!;
    public int HikeCount { get; set; }
    public int ReviewCount { get; set; }
    public double AverageRating { get; set; }
}

public class GuideTrailsViewModel
{
    public List<GuideTrailRow> Rows { get; set; } = new();

    public int TrailCount { get; set; }
    public int TotalHikes { get; set; }
}
