using ascent_app.Data;
using ascent_app.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ascent_app.Controllers;

[AllowAnonymous]
public class CommunityController : Controller
{
    private readonly AscentDbContext _context;

    public CommunityController(AscentDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var totalTrails = await _context.Trails.CountAsync();
        var totalHikes = await _context.HikeLogs.CountAsync();
        var totalHikers = await _context.HikeLogs
            .Select(h => h.UserId)
            .Distinct()
            .CountAsync();
        var totalElevation = await _context.HikeLogs
            .SumAsync(h => (long)h.Trail.ElevationGainM);

        var hikeRows = await _context.HikeLogs
            .Select(h => new { h.UserId, h.Trail.ElevationGainM, h.Trail.DistanceKm })
            .ToListAsync();

        var topUserIds = hikeRows
            .GroupBy(r => r.UserId)
            .Select(g => new
            {
                UserId = g.Key,
                Hikes = g.Count(),
                ElevationM = g.Sum(x => x.ElevationGainM),
                DistanceKm = g.Sum(x => x.DistanceKm),
            })
            .OrderByDescending(x => x.Hikes)
            .ThenByDescending(x => x.ElevationM)
            .Take(8)
            .ToList();

        var ids = topUserIds.Select(x => x.UserId).ToList();
        var names = await _context.Users
            .Where(u => ids.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.DisplayName);

        var topHikers = topUserIds
            .Select(x => new LeaderRow
            {
                UserId = x.UserId,
                DisplayName = names.TryGetValue(x.UserId, out var n) ? n : "Unknown hiker",
                Hikes = x.Hikes,
                ElevationM = x.ElevationM,
                DistanceKm = Math.Round(x.DistanceKm, 1),
            })
            .ToList();

        // feed: latest hikes and reviews, each with a photo (the hiker's own if they added one)
        var recentHikes = await _context.HikeLogs
            .Include(h => h.User)
            .Include(h => h.Photos)
            .Include(h => h.Trail).ThenInclude(t => t.Region)
            .Include(h => h.Trail).ThenInclude(t => t.Difficulty)
            .Include(h => h.Trail).ThenInclude(t => t.Photos)
            .OrderByDescending(h => h.HikedOn)
            .Take(12)
            .AsNoTracking()
            .ToListAsync();
        var recentReviews = await _context.Reviews
            .Include(r => r.User)
            .Include(r => r.Trail).ThenInclude(t => t.Region)
            .Include(r => r.Trail).ThenInclude(t => t.Difficulty)
            .Include(r => r.Trail).ThenInclude(t => t.Photos)
            .OrderByDescending(r => r.CreatedAt)
            .Take(12)
            .AsNoTracking()
            .ToListAsync();
        var activity = recentHikes.Select(h => new ActivityItem
            {
                Kind = "hike", At = h.HikedOn, UserId = h.UserId, DisplayName = h.User.DisplayName, Trail = h.Trail,
                PhotoUrl = h.Photos.FirstOrDefault()?.Url ?? h.Trail.CoverImageUrl,
                Text = h.Notes, DurationMinutes = h.DurationMinutes,
            })
            .Concat(recentReviews.Select(r => new ActivityItem
            {
                Kind = "review", At = r.CreatedAt, UserId = r.UserId, DisplayName = r.User.DisplayName, Trail = r.Trail,
                PhotoUrl = r.Trail.CoverImageUrl, Text = r.Comment, Rating = r.Rating,
            }))
            .OrderByDescending(a => a.At)
            .Take(16)
            .ToList();

        var upcoming = await _context.HikeEvents
            .Include(e => e.Guide)
            .Include(e => e.Trail).ThenInclude(t => t.Photos)
            .Include(e => e.Trail).ThenInclude(t => t.Difficulty)
            .Where(e => e.StartsAt >= DateTime.UtcNow.AddDays(-1))
            .OrderBy(e => e.StartsAt)
            .Take(6)
            .AsNoTracking()
            .ToListAsync();

        var favCounts = await _context.Favorites
            .GroupBy(f => f.TrailId)
            .Select(g => new { TrailId = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(5)
            .ToListAsync();

        var favTrailIds = favCounts.Select(x => x.TrailId).ToList();
        var favTrails = await _context.Trails
            .Include(t => t.Region)
            .Include(t => t.Difficulty)
            .Include(t => t.Photos)
            .Where(t => favTrailIds.Contains(t.Id))
            .AsNoTracking()
            .ToListAsync();

        var popularTrails = favCounts
            .Where(x => favTrails.Any(t => t.Id == x.TrailId))
            .Select(x => new PopularTrail
            {
                Trail = favTrails.First(t => t.Id == x.TrailId),
                FavoriteCount = x.Count,
            })
            .ToList();

        return View(new CommunityViewModel
        {
            TotalTrails = totalTrails,
            TotalHikes = totalHikes,
            TotalHikers = totalHikers,
            TotalElevationM = totalElevation,
            Activity = activity,
            UpcomingEvents = upcoming,
            TopHikers = topHikers,
            PopularTrails = popularTrails,
        });
    }
}
