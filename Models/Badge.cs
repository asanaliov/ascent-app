using System.ComponentModel.DataAnnotations;

namespace ascent_app.Models;

public enum BadgeCriteria
{
    HikeCount,
    CumulativeElevation,
    DistinctRegions
}

public class Badge
{
    public int Id { get; set; }

    [Required]
    [MaxLength(60)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(300)]
    public string? Description { get; set; }

    [MaxLength(40)]
    public string? IconName { get; set; } // tabler icon, e.g. "ti-mountain"

    public BadgeCriteria Criteria { get; set; }

    // award when Criteria value >= this
    public int Threshold { get; set; }

    public ICollection<UserBadge> UserBadges { get; set; } = new List<UserBadge>();
}
