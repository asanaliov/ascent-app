using System.ComponentModel.DataAnnotations;

namespace ascent_app.Models;

// one per user per trail (unique index in OnModelCreating)
public class Review
{
    public int Id { get; set; }

    [Range(1, 5)]
    public int Rating { get; set; }

    [MaxLength(1000)]
    public string? Comment { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;

    public int TrailId { get; set; }
    public Trail Trail { get; set; } = null!;
}
