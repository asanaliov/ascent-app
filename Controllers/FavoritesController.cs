using ascent_app.Data;
using ascent_app.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ascent_app.Controllers;

[Authorize]
public class FavoritesController : Controller
{
    private readonly AscentDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public FavoritesController(AscentDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var userId = _userManager.GetUserId(User)!;

        var orderedIds = await _context.Favorites
            .Where(f => f.UserId == userId)
            .OrderByDescending(f => f.CreatedAt)
            .Select(f => f.TrailId)
            .ToListAsync();

        var trails = await _context.Trails
            .Where(t => orderedIds.Contains(t.Id))
            .Include(t => t.Region)
            .Include(t => t.Difficulty)
            .Include(t => t.Reviews)
            .Include(t => t.Photos)
            .AsNoTracking()
            .ToListAsync();

        trails = orderedIds.Select(id => trails.First(t => t.Id == id)).ToList();

        ViewBag.FavoriteTrailIds = trails.Select(t => t.Id).ToHashSet();
        return View(trails);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(int trailId, string? returnUrl = null)
    {
        var userId = _userManager.GetUserId(User)!;

        var existing = await _context.Favorites
            .FirstOrDefaultAsync(f => f.UserId == userId && f.TrailId == trailId);

        if (existing == null)
        {
            if (!await _context.Trails.AnyAsync(t => t.Id == trailId)) return NotFound();

            _context.Favorites.Add(new Favorite
            {
                UserId = userId,
                TrailId = trailId,
                CreatedAt = DateTime.UtcNow,
            });
        }
        else
        {
            _context.Favorites.Remove(existing);
        }

        await _context.SaveChangesAsync();

        return Url.IsLocalUrl(returnUrl)
            ? Redirect(returnUrl!)
            : RedirectToAction("Details", "Trails", new { id = trailId });
    }
}
