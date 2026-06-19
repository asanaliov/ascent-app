using ascent_app.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ascent_app.Data;

public class AscentDbContext : IdentityDbContext<ApplicationUser>
{
    public AscentDbContext(DbContextOptions<AscentDbContext> options) : base(options)
    {
    }

    public DbSet<Region> Regions => Set<Region>();
    public DbSet<Difficulty> Difficulties => Set<Difficulty>();
    public DbSet<Trail> Trails => Set<Trail>();
    public DbSet<TrailPhoto> TrailPhotos => Set<TrailPhoto>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<TrailTag> TrailTags => Set<TrailTag>();
    public DbSet<HikeLog> HikeLogs => Set<HikeLog>();
    public DbSet<Photo> Photos => Set<Photo>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<Badge> Badges => Set<Badge>();
    public DbSet<UserBadge> UserBadges => Set<UserBadge>();
    public DbSet<Favorite> Favorites => Set<Favorite>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder); // Identity tables

        // composite keys on the join tables
        builder.Entity<TrailTag>().HasKey(tt => new { tt.TrailId, tt.TagId });
        builder.Entity<UserBadge>().HasKey(ub => new { ub.UserId, ub.BadgeId });
        builder.Entity<Favorite>().HasKey(f => new { f.UserId, f.TrailId });

        // one review per user per trail
        builder.Entity<Review>()
            .HasIndex(r => new { r.UserId, r.TrailId })
            .IsUnique();

        // store enum by name rather than int
        builder.Entity<Badge>()
            .Property(b => b.Criteria)
            .HasConversion<string>()
            .HasMaxLength(40);

        // trailhead author is optional; keep the trail if the user goes away
        builder.Entity<Trail>()
            .HasOne(t => t.Author)
            .WithMany(u => u.AuthoredTrails)
            .HasForeignKey(t => t.AuthorId)
            .OnDelete(DeleteBehavior.SetNull);

        // restrict the Trail leg of each User->...->Trail path (User leg cascades)
        builder.Entity<HikeLog>()
            .HasOne(h => h.Trail)
            .WithMany(t => t.HikeLogs)
            .HasForeignKey(h => h.TrailId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Review>()
            .HasOne(r => r.Trail)
            .WithMany(t => t.Reviews)
            .HasForeignKey(r => r.TrailId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Favorite>()
            .HasOne(f => f.Trail)
            .WithMany(t => t.Favorites)
            .HasForeignKey(f => f.TrailId)
            .OnDelete(DeleteBehavior.Restrict);

        // don't let a Region/Difficulty delete take its trails with it
        builder.Entity<Trail>()
            .HasOne(t => t.Region)
            .WithMany(r => r.Trails)
            .HasForeignKey(t => t.RegionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Trail>()
            .HasOne(t => t.Difficulty)
            .WithMany(d => d.Trails)
            .HasForeignKey(t => t.DifficultyId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
