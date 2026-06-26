using ascent_app.Data;
using ascent_app.Models;
using ascent_app.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ascent_app.Controllers;

[Authorize(Roles = "Guide,Admin")]
public class TrailPhotosController : Controller
{
    private readonly AscentDbContext _context;
    private readonly IImageStorage _images;

    public TrailPhotosController(AscentDbContext context, IImageStorage images)
    {
        _context = context;
        _images = images;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(int trailId, IFormFile? photoFile, string? caption)
    {
        var trailExists = await _context.Trails.AnyAsync(t => t.Id == trailId);
        if (!trailExists) return NotFound();

        if (photoFile == null)
        {
            TempData["TrailPhotoError"] = "Choose an image first.";
            return RedirectToAction("Details", "Trails", new { id = trailId });
        }

        var (ok, url, error) = await _images.SaveAsync(photoFile, "trails");
        if (!ok)
        {
            TempData["TrailPhotoError"] = error;
            return RedirectToAction("Details", "Trails", new { id = trailId });
        }

        _context.TrailPhotos.Add(new TrailPhoto { TrailId = trailId, Url = url!, Caption = caption });
        await _context.SaveChangesAsync();
        return RedirectToAction("Details", "Trails", new { id = trailId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var photo = await _context.TrailPhotos.FirstOrDefaultAsync(p => p.Id == id);
        if (photo == null) return NotFound();

        var trailId = photo.TrailId;
        _context.TrailPhotos.Remove(photo);
        await _context.SaveChangesAsync();
        return RedirectToAction("Details", "Trails", new { id = trailId });
    }
}
