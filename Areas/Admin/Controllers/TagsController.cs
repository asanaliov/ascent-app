using ascent_app.Areas.Admin.ViewModels;
using ascent_app.Data;
using ascent_app.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ascent_app.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class TagsController : Controller
{
    private readonly AscentDbContext _context;

    public TagsController(AscentDbContext context) => _context = context;

    public async Task<IActionResult> Index()
    {
        var tags = await _context.Tags.OrderBy(t => t.Name).AsNoTracking().ToListAsync();

        ViewBag.TrailCounts = await _context.TrailTags
            .GroupBy(tt => tt.TagId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count);

        return View(tags);
    }

    public IActionResult Create() => View(new TagFormViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TagFormViewModel form)
    {
        if (!ModelState.IsValid) return View(form);

        _context.Tags.Add(new Tag { Name = form.Name });
        await _context.SaveChangesAsync();
        TempData["Ok"] = $"Tag \"{form.Name}\" created.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var tag = await _context.Tags.FindAsync(id);
        if (tag == null) return NotFound();

        return View(new TagFormViewModel { Id = tag.Id, Name = tag.Name });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(TagFormViewModel form)
    {
        if (!ModelState.IsValid) return View(form);

        var tag = await _context.Tags.FindAsync(form.Id);
        if (tag == null) return NotFound();

        tag.Name = form.Name;
        await _context.SaveChangesAsync();
        TempData["Ok"] = $"Tag \"{form.Name}\" updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var tag = await _context.Tags.FindAsync(id);
        if (tag == null) return NotFound();

        _context.Tags.Remove(tag);
        await _context.SaveChangesAsync();
        TempData["Ok"] = $"Tag \"{tag.Name}\" deleted.";
        return RedirectToAction(nameof(Index));
    }
}
