using ascent_app.Data;
using ascent_app.Models;
using ascent_app.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ascent_app.Controllers;

[Authorize] // must be signed in to post or delete a review
public class ReviewsController : Controller
{
    private readonly AscentDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public ReviewsController(AscentDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    // POST: /Reviews/Create  (form lives on the trail Details page)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ReviewFormViewModel form)
    {
        var trail = await _context.Trails.FindAsync(form.TrailId);
        if (trail == null) return NotFound();

        if (!ModelState.IsValid)
        {
            // bounce the first validation message back to the page via TempData
            TempData["ReviewError"] = ModelState.Values
                .SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage
                ?? "Please check your review and try again.";
            return RedirectToTrail(form.TrailId);
        }

        var userId = _userManager.GetUserId(User)!;

        // unique index = one review per user per trail, so upsert instead of insert
        var existing = await _context.Reviews
            .FirstOrDefaultAsync(r => r.TrailId == form.TrailId && r.UserId == userId);

        if (existing == null)
        {
            _context.Reviews.Add(new Review
            {
                TrailId = form.TrailId,
                UserId = userId,
                Rating = form.Rating,
                Comment = form.Comment,
                CreatedAt = DateTime.UtcNow,
            });
        }
        else
        {
            existing.Rating = form.Rating;
            existing.Comment = form.Comment;
            existing.CreatedAt = DateTime.UtcNow; // bump so edits resurface
        }

        await _context.SaveChangesAsync();
        return RedirectToTrail(form.TrailId);
    }

    // POST: /Reviews/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var review = await _context.Reviews.FindAsync(id);
        if (review == null) return NotFound();

        // owner can remove their own; Admin can moderate anyone's
        var userId = _userManager.GetUserId(User)!;
        if (review.UserId != userId && !User.IsInRole("Admin"))
            return Forbid();

        var trailId = review.TrailId;
        _context.Reviews.Remove(review);
        await _context.SaveChangesAsync();
        return RedirectToTrail(trailId);
    }

    private IActionResult RedirectToTrail(int trailId)
        => RedirectToAction("Details", "Trails", new { id = trailId });
}