using ascent_app.Models;
using ascent_app.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace ascent_app.Controllers;

[Authorize]
public class ManageController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;

    public ManageController(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    // GET: /Manage/Index — edit own profile
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var vm = new EditProfileViewModel
        {
            DisplayName = user.DisplayName,
            Bio = user.Bio,
            Email = user.Email,
            MemberSince = user.CreatedAt,
            UserId = user.Id,
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(EditProfileViewModel form)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        // re-fill read-only fields so the view renders correctly on any return
        form.Email = user.Email;
        form.MemberSince = user.CreatedAt;
        form.UserId = user.Id;

        if (!ModelState.IsValid) return View(form);

        user.DisplayName = form.DisplayName;
        user.Bio = form.Bio;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);
            return View(form);
        }

        TempData["Ok"] = "Profile updated.";
        return RedirectToAction(nameof(Index));
    }
}
