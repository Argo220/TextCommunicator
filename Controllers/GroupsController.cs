using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TextCommunicator.Data;

namespace TextCommunicator.Controllers;

[Authorize]
public class GroupsController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public GroupsController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    // List groups for current user
    public async Task<IActionResult> Index()
    {
        var me = _userManager.GetUserId(User)!;

        var groups = await _db.GroupMembers
            .Where(gm => gm.UserId == me)
            .Select(gm => gm.Group)
            .OrderBy(g => g.Name)
            .ToListAsync();

        return View(groups);
    }

    // Admin: create group
    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        ViewBag.Users = await _db.Users.OrderBy(u => u.Email).ToListAsync();
        return View();
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<IActionResult> Create(string name, List<string> userIds)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            TempData["Err"] = "Podaj nazwę grupy.";
            return RedirectToAction(nameof(Create));
        }

        var g = new Group { Name = name.Trim() };
        _db.Groups.Add(g);
        await _db.SaveChangesAsync();

        foreach (var uid in userIds.Distinct())
            _db.GroupMembers.Add(new GroupMember { GroupId = g.Id, UserId = uid });

        await _db.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    // Group chat (members only)
    public async Task<IActionResult> Chat(int id)
    {
        var me = _userManager.GetUserId(User)!;

        var isMember = await _db.GroupMembers.AnyAsync(x => x.GroupId == id && x.UserId == me);
        if (!isMember) return Forbid();

        var groupName = await _db.Groups.Where(g => g.Id == id).Select(g => g.Name).FirstOrDefaultAsync();
        if (groupName is null) return NotFound();

        var msgs = await _db.GroupMessages
            .Where(m => m.GroupId == id)
            .OrderBy(m => m.CreatedAt)
            .Join(_db.Users,
                gm => gm.SenderId,
                u => u.Id,
                (gm, u) => new GroupMessageVm(gm.SenderId, u.UserName ?? u.Email ?? "?", gm.Content, gm.CreatedAt))
            .ToListAsync();

        ViewBag.GroupId = id;
        ViewBag.GroupName = groupName;
        ViewBag.MeId = me;

        return View(msgs);
    }

    [HttpPost]
    public async Task<IActionResult> Send(int groupId, string content)
    {
        var me = _userManager.GetUserId(User)!;

        var isMember = await _db.GroupMembers.AnyAsync(x => x.GroupId == groupId && x.UserId == me);
        if (!isMember) return Forbid();

        if (!string.IsNullOrWhiteSpace(content))
        {
            _db.GroupMessages.Add(new GroupMessage
            {
                GroupId = groupId,
                SenderId = me,
                Content = content.Trim()
            });
            await _db.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Chat), new { id = groupId });
    }

    // Admin: delete group
    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var g = await _db.Groups.FirstOrDefaultAsync(x => x.Id == id);
        if (g is null) return RedirectToAction(nameof(Index));

        _db.GroupMembers.RemoveRange(_db.GroupMembers.Where(m => m.GroupId == id));
        _db.GroupMessages.RemoveRange(_db.GroupMessages.Where(m => m.GroupId == id));
        _db.Groups.Remove(g);

        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    // Admin: manage members
    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<IActionResult> Members(int id)
    {
        var g = await _db.Groups.FirstOrDefaultAsync(x => x.Id == id);
        if (g is null) return NotFound();

        var members = await (from gm in _db.GroupMembers
                             join u in _db.Users on gm.UserId equals u.Id
                             where gm.GroupId == id
                             orderby u.Email
                             select new MemberRowVm(gm.Id, u.Email ?? u.UserName ?? "(brak)"))
                            .ToListAsync();

        ViewBag.GroupId = id;
        ViewBag.GroupName = g.Name;
        return View(members);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<IActionResult> RemoveMember(int membershipId, int groupId)
    {
        var m = await _db.GroupMembers.FirstOrDefaultAsync(x => x.Id == membershipId && x.GroupId == groupId);
        if (m is not null)
        {
            _db.GroupMembers.Remove(m);
            await _db.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Members), new { id = groupId });
    }

// ===== Admin: add members to existing group =====
[Authorize(Roles = "Admin")]
[HttpGet]
public async Task<IActionResult> AddMembers(int id)
{
    var g = await _db.Groups.FirstOrDefaultAsync(x => x.Id == id);
    if (g == null) return NotFound();

    var existingUserIds = await _db.GroupMembers
        .Where(x => x.GroupId == id)
        .Select(x => x.UserId)
        .ToListAsync();

    var users = await _db.Users
        .OrderBy(u => u.Email)
        .Select(u => new AddUserRowVm(u.Id, u.Email ?? u.UserName ?? "(brak)", existingUserIds.Contains(u.Id)))
        .ToListAsync();

    ViewBag.GroupId = id;
    ViewBag.GroupName = g.Name;
    return View(users);
}

[Authorize(Roles = "Admin")]
[HttpPost]
public async Task<IActionResult> AddMembers(int groupId, List<string> userIds)
{
    // add only new ones
    var existing = await _db.GroupMembers
        .Where(x => x.GroupId == groupId)
        .Select(x => x.UserId)
        .ToListAsync();

    foreach (var uid in userIds.Distinct())
    {
        if (!existing.Contains(uid))
            _db.GroupMembers.Add(new GroupMember { GroupId = groupId, UserId = uid });
    }

    await _db.SaveChangesAsync();
    return RedirectToAction(nameof(Members), new { id = groupId });
}

// ===== Admin: list all groups =====
[Authorize(Roles = "Admin")]
[HttpGet]
public async Task<IActionResult> AllGroups()
{
    var groups = await _db.Groups
        .OrderBy(g => g.Name)
        .Select(g => new GroupListVm(g.Id, g.Name))
        .ToListAsync();

    return View(groups);
}





    public record MemberRowVm(int MembershipId, string Email);
    public record GroupListVm(int GroupId, string Name);
    public record AddUserRowVm(string UserId, string Email, bool IsMember);


    public record GroupMessageVm(string SenderId, string SenderName, string Content, DateTime CreatedAt);
}
