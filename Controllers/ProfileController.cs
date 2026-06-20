using ascent_app.Data;
using ascent_app.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ascent_app.Controllers;

public class ProfileController : Controller
{
    private readonly AscentDbContext _context;

    public ProfileController(AscentDbContext context)
    {
        _context = context;
    }

    // GET: /Profile/Index/{id}  — public hiker profile
    [AllowAnonymous]
    public async Task<IActionResult> Index(string id)
    {
        if (string.IsNullOrEmpty(id)) return NotFound();

        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id);
        if (user == null) return NotFound();

        // all hikes with trail nav loaded once, aggregates computed in memory
        var hikes = await _context.HikeLogs
            .Where(h => h.UserId == id)
            .Include(h => h.Trail).ThenInclude(t => t.Region)
            .Include(h => h.Trail).ThenInclude(t => t.Difficulty)
            .OrderByDescending(h => h.HikedOn)
            .AsNoTracking()
            .ToListAsync();

        var badges = await _context.UserBadges
            .Where(ub => ub.UserId == id)
            .Include(ub => ub.Badge)
            .OrderBy(ub => ub.AwardedAt)
            .Select(ub => ub.Badge)
            .AsNoTracking()
            .ToListAsync();

        var reviewsCount = await _context.Reviews
            .CountAsync(r => r.UserId == id);

        var vm = new ProfileViewModel
        {
            UserId = user.Id,
            DisplayName = user.DisplayName,
            Bio = user.Bio,
            MemberSince = user.CreatedAt,
            TotalHikes = hikes.Count,
            TotalDistanceKm = hikes.Sum(h => h.Trail.DistanceKm),
            TotalElevationM = hikes.Sum(h => h.Trail.ElevationGainM),
            RegionsCount = hikes.Select(h => h.Trail.RegionId).Distinct().Count(),
            ReviewsCount = reviewsCount,
            Badges = badges,
            RecentHikes = hikes.Take(10).ToList(),
        };

        return View(vm);
    }
}
