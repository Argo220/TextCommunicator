using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TextCommunicator.Data;
using TextCommunicator.Models;

namespace TextCommunicator.Controllers;

[Authorize]
public class ProfileController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IWebHostEnvironment _env;

    public ProfileController(UserManager<ApplicationUser> userManager, IWebHostEnvironment env)
    {
        _userManager = userManager;
        _env = env;
    }

    // Edit own profile
    [HttpGet]
    public async Task<IActionResult> Edit(string? id = null)
    {
        var meId = _userManager.GetUserId(User)!;

        // Admin can edit any, normal user only self
        var targetId = string.IsNullOrWhiteSpace(id) ? meId : id;
        if (targetId != meId && !User.IsInRole("Admin"))
            return Forbid();

        var u = await _userManager.FindByIdAsync(targetId);
        if (u is null) return NotFound();

        var vm = new ProfileEditViewModel
        {
            UserId = u.Id,
            Email = u.Email,
            FirstName = u.FirstName,
            LastName = u.LastName,
            PhoneNumber = u.PhoneNumber,
            CurrentPhotoPath = u.ProfileImagePath
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ProfileEditViewModel vm)
    {
        var meId = _userManager.GetUserId(User)!;
        if (vm.UserId != meId && !User.IsInRole("Admin"))
            return Forbid();

        var u = await _userManager.FindByIdAsync(vm.UserId);
        if (u is null) return NotFound();

        if (!ModelState.IsValid)
        {
            vm.Email = u.Email;
            vm.CurrentPhotoPath = u.ProfileImagePath;
            return View(vm);
        }

        u.FirstName = string.IsNullOrWhiteSpace(vm.FirstName) ? null : vm.FirstName.Trim();
        u.LastName = string.IsNullOrWhiteSpace(vm.LastName) ? null : vm.LastName.Trim();
        u.PhoneNumber = string.IsNullOrWhiteSpace(vm.PhoneNumber) ? null : vm.PhoneNumber.Trim();

        // photo upload (optional)
        if (vm.Photo is not null && vm.Photo.Length > 0)
        {
            // basic validation
            var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".webp" };
            var ext = Path.GetExtension(vm.Photo.FileName);
            if (!allowed.Contains(ext))
            {
                ModelState.AddModelError(nameof(vm.Photo), "Dozwolone formaty: JPG, PNG, WEBP.");
                vm.Email = u.Email;
                vm.CurrentPhotoPath = u.ProfileImagePath;
                return View(vm);
            }
            if (vm.Photo.Length > 2 * 1024 * 1024) // 2MB
            {
                ModelState.AddModelError(nameof(vm.Photo), "Maksymalny rozmiar zdjęcia: 2MB.");
                vm.Email = u.Email;
                vm.CurrentPhotoPath = u.ProfileImagePath;
                return View(vm);
            }

            var uploadsDir = Path.Combine(_env.WebRootPath, "uploads");
            Directory.CreateDirectory(uploadsDir);

            var fileName = $"{Guid.NewGuid():N}{ext}";
            var savePath = Path.Combine(uploadsDir, fileName);

            using (var fs = new FileStream(savePath, FileMode.Create))
            {
                await vm.Photo.CopyToAsync(fs);
            }

            // optionally delete old file if was in uploads
            if (!string.IsNullOrWhiteSpace(u.ProfileImagePath) && u.ProfileImagePath.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase))
            {
                var old = Path.Combine(_env.WebRootPath, u.ProfileImagePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                if (System.IO.File.Exists(old))
                    System.IO.File.Delete(old);
            }

            u.ProfileImagePath = "/uploads/" + fileName;
        }

        await _userManager.UpdateAsync(u);

        TempData["Ok"] = "Zapisano profil.";
        return RedirectToAction(nameof(Edit), new { id = (User.IsInRole("Admin") ? u.Id : null) });
    }
}
