namespace ascent_app.Models;

public class UserBadge
{
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;

    public int BadgeId { get; set; }
    public Badge Badge { get; set; } = null!;

    public DateTime AwardedAt { get; set; } = DateTime.UtcNow;
}
