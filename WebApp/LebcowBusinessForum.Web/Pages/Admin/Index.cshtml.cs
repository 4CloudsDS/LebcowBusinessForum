using LebcowBusinessForum.Web.Data;
using LebcowBusinessForum.Web.Models;
using LebcowBusinessForum.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace LebcowBusinessForum.Web.Pages.Admin;

[Authorize(Roles = "Admin")]
public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _db;
    private readonly IBusinessService _businessService;
    private readonly UserManager<ApplicationUser> _userManager;

    public IndexModel(ApplicationDbContext db, IBusinessService businessService, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _businessService = businessService;
        _userManager = userManager;
    }

    public int PendingListings { get; private set; }
    public int TotalBusinesses { get; private set; }
    public int TotalUsers { get; private set; }
    public int PendingReviews { get; private set; }
    public IReadOnlyList<Business> PendingBusinesses { get; private set; } = [];

    public async Task OnGetAsync()
    {
        PendingListings = await _db.Businesses.CountAsync(b => b.Status == "pending");
        TotalBusinesses = await _db.Businesses.CountAsync();
        TotalUsers = await _db.Users.CountAsync();
        PendingReviews = await _db.Reviews.CountAsync();
        PendingBusinesses = await _db.Businesses
            .Include(b => b.Category)
            .Where(b => b.Status == "pending")
            .OrderBy(b => b.CreatedAt)
            .ToListAsync();
    }

    public async Task<IActionResult> OnPostApproveAsync(Guid id)
    {
        var adminId = Guid.Parse(_userManager.GetUserId(User)!);
        await _businessService.ApproveAsync(id, adminId);
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostRejectAsync(Guid id)
    {
        var adminId = Guid.Parse(_userManager.GetUserId(User)!);
        await _businessService.RejectAsync(id, adminId);
        return RedirectToPage();
    }
}

