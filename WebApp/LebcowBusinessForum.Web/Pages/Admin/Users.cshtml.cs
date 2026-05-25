using LebcowBusinessForum.Web.Data;
using LebcowBusinessForum.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace LebcowBusinessForum.Web.Pages.Admin;

[Authorize(Roles = "Admin")]
public class UsersModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _db;

    public UsersModel(UserManager<ApplicationUser> userManager, ApplicationDbContext db)
    {
        _userManager = userManager;
        _db = db;
    }

    public record UserRow(
        Guid Id,
        string Email,
        string FullName,
        string? PhoneNumber,
        bool IsActive,
        DateTime CreatedAt,
        IList<string> Roles
    );

    [BindProperty(SupportsGet = true)]
    public string? RoleFilter { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }

    public IReadOnlyList<UserRow> Users { get; private set; } = [];

    public async Task OnGetAsync()
    {
        var users = await _db.Users.OrderBy(u => u.CreatedAt).ToListAsync();

        var rows = new List<UserRow>();
        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);

            if (!string.IsNullOrWhiteSpace(Search))
            {
                var term = Search.Trim().ToLowerInvariant();
                if (!user.Email!.Contains(term, StringComparison.OrdinalIgnoreCase) &&
                    !user.FullName.Contains(term, StringComparison.OrdinalIgnoreCase))
                    continue;
            }

            if (!string.IsNullOrWhiteSpace(RoleFilter) && RoleFilter != "all" &&
                !roles.Contains(RoleFilter, StringComparer.OrdinalIgnoreCase))
                continue;

            rows.Add(new UserRow(user.Id, user.Email ?? "", user.FullName, user.PhoneNumber, user.IsActive, user.CreatedAt, roles));
        }

        Users = rows;
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        var actingUserId = Guid.Parse(_userManager.GetUserId(User)!);
        if (id == actingUserId)
        {
            TempData["Message"] = "You cannot delete your own account.";
            TempData["MessageType"] = "danger";
            return RedirectToPage(new { RoleFilter, Search });
        }

        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user is not null)
            await _userManager.DeleteAsync(user);

        TempData["Message"] = "User deleted.";
        TempData["MessageType"] = "success";
        return RedirectToPage(new { RoleFilter, Search });
    }

    public async Task<IActionResult> OnPostToggleAdminAsync(Guid id)
    {
        var actingUserId = Guid.Parse(_userManager.GetUserId(User)!);
        if (id == actingUserId)
        {
            TempData["Message"] = "You cannot modify your own admin role.";
            TempData["MessageType"] = "danger";
            return RedirectToPage(new { RoleFilter, Search });
        }

        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user is not null)
        {
            if (await _userManager.IsInRoleAsync(user, "Admin"))
                await _userManager.RemoveFromRoleAsync(user, "Admin");
            else
                await _userManager.AddToRoleAsync(user, "Admin");
        }

        TempData["Message"] = "Admin role updated.";
        TempData["MessageType"] = "success";
        return RedirectToPage(new { RoleFilter, Search });
    }

    public async Task<IActionResult> OnPostToggleActiveAsync(Guid id)
    {
        var actingUserId = Guid.Parse(_userManager.GetUserId(User)!);
        if (id == actingUserId)
        {
            TempData["Message"] = "You cannot flag your own account.";
            TempData["MessageType"] = "danger";
            return RedirectToPage(new { RoleFilter, Search });
        }

        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user is not null)
        {
            user.IsActive = !user.IsActive;
            // Mirror IsActive into Identity lockout so inactive users can't sign in
            if (!user.IsActive)
            {
                await _userManager.SetLockoutEnabledAsync(user, true);
                await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
            }
            else
            {
                await _userManager.SetLockoutEndDateAsync(user, null);
            }
            await _userManager.UpdateAsync(user);
        }

        TempData["Message"] = "User status updated.";
        TempData["MessageType"] = "success";
        return RedirectToPage(new { RoleFilter, Search });
    }
}
