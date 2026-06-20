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
    private readonly IImageStorage _images;
    private readonly UserManager<ApplicationUser> _userManager;

    public HikeLogsController(
        AscentDbContext context,
        IBadgeService badges,
        IImageStorage images,
        UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _badges = badges;
        _images = images;
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
            .Include(h => h.Photos)
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

        string? photoUrl = null;
        if (form.PhotoFile != null)
        {
            var (ok, url, error) = await _images.SaveAsync(form.PhotoFile, "hikes");
            if (!ok)
            {
                ModelState.AddModelError(nameof(form.PhotoFile), error!);
                form.TrailName = trail.Name;
                return View(form);
            }
            photoUrl = url;
        }

        var userId = _userManager.GetUserId(User)!;
        var hike = new HikeLog
        {
            UserId = userId,
            TrailId = form.TrailId,
            HikedOn = form.HikedOn,
            DurationMinutes = form.DurationMinutes,
            Notes = form.Notes,
            CreatedAt = DateTime.UtcNow,
        };
        if (photoUrl != null)
            hike.Photos.Add(new Photo { Url = photoUrl, Caption = form.PhotoCaption });

        _context.HikeLogs.Add(hike);
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

    // POST: /HikeLogs/AddPhoto  — attach a photo to one of my hikes
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddPhoto(int hikeLogId, IFormFile? photoFile, string? caption)
    {
        var userId = _userManager.GetUserId(User)!;
        var hike = await _context.HikeLogs
            .FirstOrDefaultAsync(h => h.Id == hikeLogId && h.UserId == userId); // ownership check
        if (hike == null) return NotFound();

        if (photoFile == null)
        {
            TempData["PhotoError"] = "Choose an image first.";
            return RedirectToAction(nameof(Index));
        }

        var (ok, url, error) = await _images.SaveAsync(photoFile, "hikes");
        if (!ok)
        {
            TempData["PhotoError"] = error;
            return RedirectToAction(nameof(Index));
        }

        _context.Photos.Add(new Photo { HikeLogId = hikeLogId, Url = url!, Caption = caption });
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    // POST: /HikeLogs/DeletePhoto/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeletePhoto(int id)
    {
        var userId = _userManager.GetUserId(User)!;
        var photo = await _context.Photos
            .FirstOrDefaultAsync(p => p.Id == id && p.HikeLog.UserId == userId); // owned via the hike

        if (photo != null)
        {
            _context.Photos.Remove(photo);
            await _context.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index));
    }
}
