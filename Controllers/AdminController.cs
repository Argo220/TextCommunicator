using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TextCommunicator.Data;

namespace TextCommunicator.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public AdminController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public async Task<IActionResult> Users()
{
    var users = await _db.Users.OrderBy(u => u.Email).ToListAsync();
    var rows = new List<UserRowVm>();

    foreach (var u in users)
    {
        var isAdmin = await _userManager.IsInRoleAsync(u, "Admin");
        rows.Add(new UserRowVm(u.Id, u.Email ?? u.UserName ?? "(brak)", isAdmin));
    }

    return View(rows);
}

    

[HttpPost]
public async Task<IActionResult> ToggleAdmin(string id)
{
    var u = await _userManager.FindByIdAsync(id);
    if (u is null) return RedirectToAction(nameof(Users));

    if (u.Email == "admin@tc.local")
    {
        TempData["Err"] = "Nie można zmieniać roli domyślnego admina (admin@tc.local).";
        return RedirectToAction(nameof(Users));
    }

    var isAdmin = await _userManager.IsInRoleAsync(u, "Admin");
    if (isAdmin)
    {
        await _userManager.RemoveFromRoleAsync(u, "Admin");
        // ensure at least User role
        if (!await _userManager.IsInRoleAsync(u, "User"))
            await _userManager.AddToRoleAsync(u, "User");
    }
    else
    {
        await _userManager.AddToRoleAsync(u, "Admin");
    }

    return RedirectToAction(nameof(Users));
}
[HttpPost]
    public async Task<IActionResult> DeleteUser(string id)
    {
        var u = await _userManager.FindByIdAsync(id);
        if (u is null) return RedirectToAction(nameof(Users));

        // Don't allow deleting the default admin easily
        if (u.Email == "admin@tc.local")
        {
            TempData["Err"] = "Nie można usunąć domyślnego admina (admin@tc.local).";
            return RedirectToAction(nameof(Users));
        }

        _db.GroupMembers.RemoveRange(_db.GroupMembers.Where(gm => gm.UserId == id));
        _db.Messages.RemoveRange(_db.Messages.Where(m => m.SenderId == id || m.RecipientId == id));
        _db.GroupMessages.RemoveRange(_db.GroupMessages.Where(m => m.SenderId == id));

        await _db.SaveChangesAsync();
        await _userManager.DeleteAsync(u);

        return RedirectToAction(nameof(Users));
    }


public record UserRowVm(string Id, string Email, bool IsAdmin);

}