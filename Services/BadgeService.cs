using ascent_app.Data;
using ascent_app.Models;
using Microsoft.EntityFrameworkCore;

namespace ascent_app.Services;

public interface IBadgeService
{
    Task<IReadOnlyList<Badge>> EvaluateAsync(string userId);
}

public class BadgeService : IBadgeService
{
    private readonly AscentDbContext _context;

    public BadgeService(AscentDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<Badge>> EvaluateAsync(string userId)
    {
        var hikes = await _context.HikeLogs
            .Where(h => h.UserId == userId)
            .Include(h => h.Trail)
            .AsNoTracking()
            .ToListAsync();

        var hikeCount = hikes.Count;
        var cumulativeElevation = hikes.Sum(h => h.Trail.ElevationGainM);
        var distinctRegions = hikes.Select(h => h.Trail.RegionId).Distinct().Count();

        var earnedIds = await _context.UserBadges
            .Where(ub => ub.UserId == userId)
            .Select(ub => ub.BadgeId)
            .ToListAsync();

        var candidates = await _context.Badges
            .Where(b => !earnedIds.Contains(b.Id))
            .ToListAsync();

        var newlyEarned = new List<Badge>();
        foreach (var badge in candidates)
        {
            var value = badge.Criteria switch
            {
                BadgeCriteria.HikeCount => hikeCount,
                BadgeCriteria.CumulativeElevation => cumulativeElevation,
                BadgeCriteria.DistinctRegions => distinctRegions,
                _ => 0,
            };

            if (value >= badge.Threshold)
            {
                _context.UserBadges.Add(new UserBadge
                {
                    UserId = userId,
                    BadgeId = badge.Id,
                    AwardedAt = DateTime.UtcNow,
                });
                newlyEarned.Add(badge);
            }
        }

        if (newlyEarned.Count > 0)
            await _context.SaveChangesAsync();

        return newlyEarned;
    }
}
