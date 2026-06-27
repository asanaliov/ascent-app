using System.ComponentModel.DataAnnotations;

namespace ascent_app.Models;

public class HikeLog
{
    public int Id { get; set; }

    public DateTime HikedOn { get; set; }

    [Range(0, 10000)]
    public int? DurationMinutes { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public bool IsDemoData { get; set; }

    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;

    public int TrailId { get; set; }
    public Trail Trail { get; set; } = null!;

    public ICollection<Photo> Photos { get; set; } = new List<Photo>();
}
