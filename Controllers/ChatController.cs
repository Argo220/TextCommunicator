using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TextCommunicator.Data;

namespace TextCommunicator.Controllers;

[Authorize]
public class ChatController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public ChatController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var me = _userManager.GetUserId(User)!;

        var users = await _db.Users
            .Where(u => u.Id != me)
            .OrderBy(u => u.UserName)
            .Select(u => new UserListItem(u.Id, u.UserName!))
            .ToListAsync();

        return View(users);
    }

    public async Task<IActionResult> With(string id)
    {
        var me = _userManager.GetUserId(User)!;

        var other = await _db.Users.Where(u => u.Id == id).Select(u => u.UserName).FirstOrDefaultAsync();
        if (other is null) return NotFound();

        var msgs = await _db.Messages
            .Where(m => (m.SenderId == me && m.RecipientId == id) ||
                        (m.SenderId == id && m.RecipientId == me))
            .OrderBy(m => m.CreatedAt)
            .Select(m => new ChatMessageVm(m.SenderId, m.Content, m.CreatedAt))
            .ToListAsync();

        ViewBag.OtherId = id;
        ViewBag.OtherName = other;
        ViewBag.MeId = me;

        return View(msgs);
    }

    [HttpPost]
    public async Task<IActionResult> Send(string recipientId, string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return RedirectToAction(nameof(With), new { id = recipientId });

        var me = _userManager.GetUserId(User)!;

        _db.Messages.Add(new Message
        {
            SenderId = me,
            RecipientId = recipientId,
            Content = content.Trim()
        });

        await _db.SaveChangesAsync();

        return RedirectToAction(nameof(With), new { id = recipientId });
    }

    public record UserListItem(string Id, string UserName);
    public record ChatMessageVm(string SenderId, string Content, DateTime CreatedAt);
}
