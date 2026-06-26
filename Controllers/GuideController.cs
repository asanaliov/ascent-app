using ascent_app.Data;
using ascent_app.Models;
using ascent_app.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ascent_app.Controllers;

[Authorize(Roles = "Guide,Admin")]
public class GuideController : Controller
{
    private readonly AscentDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public GuideController(AscentDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var userId = _userManager.GetUserId(User);

        var trails = await _context.Trails
            .Where(t => t.AuthorId == userId)
            .Include(t => t.Region)
            .Include(t => t.Difficulty)
            .Include(t => t.Reviews)
            .Include(t => t.HikeLogs)
            .Include(t => t.TrailTags).ThenInclude(tt => tt.Tag)
            .AsNoTracking()
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();

        var rows = trails.Select(t => new GuideTrailRow
        {
            Trail = t,
            HikeCount = t.HikeLogs.Count,
            ReviewCount = t.Reviews.Count,
            AverageRating = t.Reviews.Count == 0 ? 0 : Math.Round(t.Reviews.Average(r => r.Rating), 1),
        }).ToList();

        var vm = new GuideTrailsViewModel
        {
            Rows = rows,
            TrailCount = rows.Count,
            TotalHikes = rows.Sum(r => r.HikeCount),
        };

        return View(vm);
    }
}
