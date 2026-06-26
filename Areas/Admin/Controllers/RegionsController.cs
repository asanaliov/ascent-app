using ascent_app.Areas.Admin.ViewModels;
using ascent_app.Data;
using ascent_app.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ascent_app.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class RegionsController : Controller
{
    private readonly AscentDbContext _context;

    public RegionsController(AscentDbContext context) => _context = context;

    public async Task<IActionResult> Index()
    {
        var regions = await _context.Regions.OrderBy(r => r.Name).AsNoTracking().ToListAsync();

        ViewBag.TrailCounts = await _context.Trails
            .GroupBy(t => t.RegionId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count);

        return View(regions);
    }

    public IActionResult Create() => View(new RegionFormViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(RegionFormViewModel form)
    {
        if (!ModelState.IsValid) return View(form);

        _context.Regions.Add(new Region { Name = form.Name, Country = form.Country, Description = form.Description });
        await _context.SaveChangesAsync();
        TempData["Ok"] = $"Region \"{form.Name}\" created.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var region = await _context.Regions.FindAsync(id);
        if (region == null) return NotFound();

        return View(new RegionFormViewModel { Id = region.Id, Name = region.Name, Country = region.Country, Description = region.Description });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(RegionFormViewModel form)
    {
        if (!ModelState.IsValid) return View(form);

        var region = await _context.Regions.FindAsync(form.Id);
        if (region == null) return NotFound();

        region.Name = form.Name;
        region.Country = form.Country;
        region.Description = form.Description;
        await _context.SaveChangesAsync();
        TempData["Ok"] = $"Region \"{form.Name}\" updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var region = await _context.Regions.FindAsync(id);
        if (region == null) return NotFound();

        if (await _context.Trails.AnyAsync(t => t.RegionId == id))
        {
            TempData["Err"] = $"Can't delete \"{region.Name}\" — trails still use it.";
            return RedirectToAction(nameof(Index));
        }

        _context.Regions.Remove(region);
        await _context.SaveChangesAsync();
        TempData["Ok"] = $"Region \"{region.Name}\" deleted.";
        return RedirectToAction(nameof(Index));
    }
}
