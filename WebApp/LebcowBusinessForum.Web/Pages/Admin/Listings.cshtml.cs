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
public class ListingsModel : PageModel
{
    private readonly ApplicationDbContext _db;
    private readonly IBusinessService _businessService;
    private readonly UserManager<ApplicationUser> _userManager;

    public ListingsModel(ApplicationDbContext db, IBusinessService businessService, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _businessService = businessService;
        _userManager = userManager;
    }

    [BindProperty(SupportsGet = true)]
    public string? StatusFilter { get; set; }

    public IReadOnlyList<Business> Businesses { get; private set; } = [];
    public IReadOnlyList<Listing> PendingUpgrades { get; private set; } = [];

    public async Task OnGetAsync()
    {
        var query = _db.Businesses
            .Include(b => b.Category)
            .Include(b => b.Listings)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(StatusFilter) && StatusFilter != "all")
            query = query.Where(b => b.Status == StatusFilter);

        Businesses = await query.OrderBy(b => b.CreatedAt).ToListAsync();

        // Listing upgrade requests awaiting payment confirmation
        PendingUpgrades = await _db.Listings
            .Include(l => l.Business)
            .Where(l => l.PaymentStatus == "pending")
            .OrderByDescending(l => l.StartDate)
            .ToListAsync();
    }

    public async Task<IActionResult> OnPostApproveAsync(Guid id)
    {
        var adminId = Guid.Parse(_userManager.GetUserId(User)!);
        await _businessService.ApproveAsync(id, adminId);
        TempData["Message"] = "Business approved.";
        return RedirectToPage(new { StatusFilter });
    }

    public async Task<IActionResult> OnPostRejectAsync(Guid id)
    {
        var adminId = Guid.Parse(_userManager.GetUserId(User)!);
        await _businessService.RejectAsync(id, adminId);
        TempData["Message"] = "Business rejected.";
        return RedirectToPage(new { StatusFilter });
    }

    public async Task<IActionResult> OnPostDelistAsync(Guid id)
    {
        var adminId = Guid.Parse(_userManager.GetUserId(User)!);
        await _businessService.DelistAsync(id, adminId);
        TempData["Message"] = "Business delisted.";
        return RedirectToPage(new { StatusFilter });
    }

    public async Task<IActionResult> OnPostConfirmUpgradeAsync(Guid listingId)
    {
        var listing = await _db.Listings.FindAsync(listingId);
        if (listing is not null)
        {
            listing.PaymentStatus = "paid";
            await _db.SaveChangesAsync();
            TempData["Message"] = $"Listing upgrade confirmed as paid.";
        }
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostCancelUpgradeAsync(Guid listingId)
    {
        var listing = await _db.Listings.FindAsync(listingId);
        if (listing is not null)
        {
            listing.Tier = "free";
            listing.PaymentStatus = "unpaid";
            await _db.SaveChangesAsync();
            TempData["Message"] = "Listing upgrade request cancelled.";
        }
        return RedirectToPage();
    }
}
