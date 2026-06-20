using ascent_app.Areas.Admin.ViewModels;
using ascent_app.Data;
using ascent_app.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ascent_app.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class BadgesController : Controller
{
    private readonly AscentDbContext _context;

    public BadgesController(AscentDbContext context) => _context = context;

    public async Task<IActionResult> Index()
    {
        var badges = await _context.Badges.OrderBy(b => b.Threshold).AsNoTracking().ToListAsync();

        ViewBag.AwardCounts = await _context.UserBadges
            .GroupBy(ub => ub.BadgeId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count);

        return View(badges);
    }

    public IActionResult Create() => View(new BadgeFormViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(BadgeFormViewModel form)
    {
        if (!ModelState.IsValid) return View(form);

        _context.Badges.Add(new Badge
        {
            Name = form.Name,
            Description = form.Description,
            IconName = form.IconName,
            Criteria = form.Criteria,
            Threshold = form.Threshold,
        });
        await _context.SaveChangesAsync();
        TempData["Ok"] = $"Badge \"{form.Name}\" created.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var badge = await _context.Badges.FindAsync(id);
        if (badge == null) return NotFound();

        return View(new BadgeFormViewModel
        {
            Id = badge.Id,
            Name = badge.Name,
            Description = badge.Description,
            IconName = badge.IconName,
            Criteria = badge.Criteria,
            Threshold = badge.Threshold,
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(BadgeFormViewModel form)
    {
        if (!ModelState.IsValid) return View(form);

        var badge = await _context.Badges.FindAsync(form.Id);
        if (badge == null) return NotFound();

        badge.Name = form.Name;
        badge.Description = form.Description;
        badge.IconName = form.IconName;
        badge.Criteria = form.Criteria;
        badge.Threshold = form.Threshold;
        await _context.SaveChangesAsync();
        TempData["Ok"] = $"Badge \"{form.Name}\" updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var badge = await _context.Badges.FindAsync(id);
        if (badge == null) return NotFound();

        // UserBadge rows cascade away with the badge
        _context.Badges.Remove(badge);
        await _context.SaveChangesAsync();
        TempData["Ok"] = $"Badge \"{badge.Name}\" deleted.";
        return RedirectToAction(nameof(Index));
    }
}
