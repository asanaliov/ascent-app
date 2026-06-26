using ascent_app.Data;
using ascent_app.Models;
using ascent_app.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ascent_app.Controllers;

[Authorize]
public class DashboardController : Controller
{
    private readonly AscentDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public DashboardController(AscentDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        var userId = user!.Id;

        var hikes = await _context.HikeLogs
            .Where(h => h.UserId == userId)
            .Include(h => h.Trail).ThenInclude(t => t.Region)
            .Include(h => h.Trail).ThenInclude(t => t.Difficulty)
            .OrderByDescending(h => h.HikedOn)
            .AsNoTracking()
            .ToListAsync();

        var totalHikes = hikes.Count;
        var totalElevation = hikes.Sum(h => h.Trail.ElevationGainM);
        var totalDistance = hikes.Sum(h => h.Trail.DistanceKm);
        var distinctRegions = hikes.Select(h => h.Trail.RegionId).Distinct().Count();

        var earnedBadgeIds = (await _context.UserBadges
            .Where(ub => ub.UserId == userId)
            .Select(ub => ub.BadgeId)
            .ToListAsync()).ToHashSet();

        var allBadges = await _context.Badges
            .OrderBy(b => b.Threshold)
            .AsNoTracking()
            .ToListAsync();

        var badges = allBadges.Select(b => new BadgeProgress
        {
            Badge = b,
            Earned = earnedBadgeIds.Contains(b.Id),
            Threshold = b.Threshold,
            Current = b.Criteria switch
            {
                BadgeCriteria.HikeCount => totalHikes,
                BadgeCriteria.CumulativeElevation => totalElevation,
                BadgeCriteria.DistinctRegions => distinctRegions,
                _ => 0,
            },
        }).ToList();

        var vm = new DashboardViewModel
        {
            DisplayName = user.DisplayName,
            TotalHikes = totalHikes,
            TotalElevationM = totalElevation,
            TotalDistanceKm = Math.Round(totalDistance, 1),
            DistinctRegions = distinctRegions,
            ReviewsWritten = await _context.Reviews.CountAsync(r => r.UserId == userId),
            SavedTrails = await _context.Favorites.CountAsync(f => f.UserId == userId),
            RecentHikes = hikes.Take(5).ToList(),
            Badges = badges,
        };

        return View(vm);
    }
}
