using System.ComponentModel.DataAnnotations;

namespace ascent_app.Models;

public class HikeEvent
{
    public int Id { get; set; }

    [Required]
    [MaxLength(120)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    public DateTime StartsAt { get; set; }

    [Range(1, 100)]
    public int MaxParticipants { get; set; }

    [MaxLength(200)]
    public string? MeetingPoint { get; set; }

    public bool IsDemoData { get; set; }

    public int TrailId { get; set; }
    public Trail Trail { get; set; } = null!;

    public string GuideId { get; set; } = string.Empty;
    public ApplicationUser Guide { get; set; } = null!;
}
