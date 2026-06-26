using ascent_app.Areas.Admin.ViewModels;
using ascent_app.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ascent_app.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class UsersController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;

    public UsersController(UserManager<ApplicationUser> userManager) => _userManager = userManager;

    public async Task<IActionResult> Index()
    {
        var users = await _userManager.Users.OrderBy(u => u.DisplayName).ToListAsync();

        var rows = new List<UserRowViewModel>();
        foreach (var u in users)
        {
            var roles = await _userManager.GetRolesAsync(u);
            rows.Add(new UserRowViewModel
            {
                Id = u.Id,
                DisplayName = u.DisplayName,
                Email = u.Email,
                IsGuide = roles.Contains("Guide"),
                IsAdmin = roles.Contains("Admin"),
            });
        }

        return View(rows);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleRole(string userId, string role)
    {
        if (role != "Guide" && role != "Admin") return BadRequest();

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return NotFound();

        if (role == "Admin" && userId == _userManager.GetUserId(User))
        {
            TempData["Err"] = "You can't change your own admin role.";
            return RedirectToAction(nameof(Index));
        }

        if (await _userManager.IsInRoleAsync(user, role))
            await _userManager.RemoveFromRoleAsync(user, role);
        else
            await _userManager.AddToRoleAsync(user, role);

        TempData["Ok"] = $"Updated roles for {user.DisplayName}.";
        return RedirectToAction(nameof(Index));
    }
}
