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
            .Select(h => new { h.UserId, h.Trail.ElevationGainM })
            .ToListAsync();

        var topUserIds = hikeRows
            .GroupBy(r => r.UserId)
            .Select(g => new
            {
                UserId = g.Key,
                Hikes = g.Count(),
                ElevationM = g.Sum(x => x.ElevationGainM),
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
            })
            .ToList();

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

        var recentTrails = await _context.Trails
            .Include(t => t.Region)
            .Include(t => t.Difficulty)
            .OrderByDescending(t => t.CreatedAt)
            .Take(5)
            .AsNoTracking()
            .ToListAsync();

        return View(new CommunityViewModel
        {
            TotalTrails = totalTrails,
            TotalHikes = totalHikes,
            TotalHikers = totalHikers,
            TotalElevationM = totalElevation,
            TopHikers = topHikers,
            PopularTrails = popularTrails,
            RecentTrails = recentTrails,
        });
    }
}
