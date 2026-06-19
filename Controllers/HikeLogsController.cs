using ascent_app.Data;
using ascent_app.Models;
using ascent_app.Services;
using ascent_app.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ascent_app.Controllers;

[Authorize] // any signed-in user (Hiker and up) can log hikes
public class HikeLogsController : Controller
{
    private readonly AscentDbContext _context;
    private readonly IBadgeService _badges;
    private readonly UserManager<ApplicationUser> _userManager;

    public HikeLogsController(
        AscentDbContext context,
        IBadgeService badges,
        UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _badges = badges;
        _userManager = userManager;
    }

    // GET: /HikeLogs  — "My hikes"
    public async Task<IActionResult> Index()
    {
        var userId = _userManager.GetUserId(User)!;

        var hikes = await _context.HikeLogs
            .Where(h => h.UserId == userId)
            .Include(h => h.Trail).ThenInclude(t => t.Region)
            .Include(h => h.Trail).ThenInclude(t => t.Difficulty)
            .OrderByDescending(h => h.HikedOn)
            .AsNoTracking()
            .ToListAsync();

        // earned badges for the sidebar
        ViewBag.Badges = await _context.UserBadges
            .Where(ub => ub.UserId == userId)
            .Include(ub => ub.Badge)
            .OrderBy(ub => ub.AwardedAt)
            .Select(ub => ub.Badge)
            .AsNoTracking()
            .ToListAsync();

        return View(hikes);
    }

    // GET: /HikeLogs/Create?trailId=5
    public async Task<IActionResult> Create(int trailId)
    {
        var trail = await _context.Trails.FindAsync(trailId);
        if (trail == null) return NotFound();

        return View(new HikeLogFormViewModel { TrailId = trail.Id, TrailName = trail.Name });
    }

    // POST: /HikeLogs/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(HikeLogFormViewModel form)
    {
        var trail = await _context.Trails.FindAsync(form.TrailId);
        if (trail == null) return NotFound();

        if (!ModelState.IsValid)
        {
            form.TrailName = trail.Name;
            return View(form);
        }

        var userId = _userManager.GetUserId(User)!;
        _context.HikeLogs.Add(new HikeLog
        {
            UserId = userId,
            TrailId = form.TrailId,
            HikedOn = form.HikedOn,
            DurationMinutes = form.DurationMinutes,
            Notes = form.Notes,
            CreatedAt = DateTime.UtcNow,
        });
        await _context.SaveChangesAsync();

        // award any newly-earned badges and surface them
        var earned = await _badges.EvaluateAsync(userId);
        if (earned.Count > 0)
            TempData["NewBadges"] = string.Join(", ", earned.Select(b => b.Name));

        return RedirectToAction(nameof(Index));
    }

    // POST: /HikeLogs/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = _userManager.GetUserId(User)!;
        var hike = await _context.HikeLogs
            .FirstOrDefaultAsync(h => h.Id == id && h.UserId == userId); // ownership check

        if (hike != null)
        {
            _context.HikeLogs.Remove(hike);
            await _context.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index));
    }
}
