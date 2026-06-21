using ascent_app.Data;
using ascent_app.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ascent_app.Controllers;

[Authorize] // favoriting is per-user, so you must be signed in
public class FavoritesController : Controller
{
    private readonly AscentDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public FavoritesController(AscentDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    // GET: /Favorites  — "Saved trails"
    public async Task<IActionResult> Index()
    {
        var userId = _userManager.GetUserId(User)!;

        // favorite trail ids, most recently saved first
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
            .AsNoTracking()
            .ToListAsync();

        // re-apply saved order (the Where above doesn't preserve it)
        trails = orderedIds.Select(id => trails.First(t => t.Id == id)).ToList();

        // every trail on this page is, by definition, a favorite
        ViewBag.FavoriteTrailIds = trails.Select(t => t.Id).ToHashSet();
        return View(trails);
    }

    // POST: /Favorites/Toggle  — add if missing, remove if present
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(int trailId, string? returnUrl = null)
    {
        var userId = _userManager.GetUserId(User)!;

        var existing = await _context.Favorites
            .FirstOrDefaultAsync(f => f.UserId == userId && f.TrailId == trailId);

        if (existing == null)
        {
            // guard against favoriting a trail that doesn't exist
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
