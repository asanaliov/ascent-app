namespace ascent_app.ViewModels;

// current filter selection echoed back to the trails index view
public class TrailFilterViewModel
{
    public string? Q { get; set; }
    public int? RegionId { get; set; }
    public int? DifficultyId { get; set; }
    public int? TagId { get; set; }
}
