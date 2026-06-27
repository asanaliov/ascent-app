namespace ascent_app.Models;

public class Favorite
{
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;

    public int TrailId { get; set; }
    public Trail Trail { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public bool IsDemoData { get; set; }
}
