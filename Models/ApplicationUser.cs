using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace ascent_app.Models;

public class ApplicationUser : IdentityUser
{
    [Required]
    [MaxLength(80)]
    public string DisplayName { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Bio { get; set; }

    public double? HomeLat { get; set; }
    public double? HomeLng { get; set; }

    [MaxLength(100)]
    public string? HomeLocationName { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Trail> AuthoredTrails { get; set; } = new List<Trail>();
    public ICollection<HikeLog> HikeLogs { get; set; } = new List<HikeLog>();
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
    public ICollection<Favorite> Favorites { get; set; } = new List<Favorite>();
    public ICollection<UserBadge> UserBadges { get; set; } = new List<UserBadge>();
}
