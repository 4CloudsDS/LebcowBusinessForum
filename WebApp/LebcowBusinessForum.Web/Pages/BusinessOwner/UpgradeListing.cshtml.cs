using System.ComponentModel.DataAnnotations;
using LebcowBusinessForum.Web.Data;
using LebcowBusinessForum.Web.Models;
using LebcowBusinessForum.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace LebcowBusinessForum.Web.Pages.BusinessOwner;

[Authorize]
public class UpgradeListingModel : PageModel
{
    private readonly ApplicationDbContext _db;
    private readonly IBusinessService _businessService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAuditService _audit;

    public UpgradeListingModel(
        ApplicationDbContext db,
        IBusinessService businessService,
        UserManager<ApplicationUser> userManager,
        IAuditService audit)
    {
        _db = db;
        _businessService = businessService;
        _userManager = userManager;
        _audit = audit;
    }

    public Business? Business { get; private set; }

    [BindProperty]
    [Required]
    public string Tier { get; set; } = "premium";

    public async Task<IActionResult> OnGetAsync(Guid businessId)
    {
        var userId = Guid.Parse(_userManager.GetUserId(User)!);
        Business = await _businessService.GetByIdAsync(businessId);

        if (Business is null || !await _businessService.IsOwnerAsync(businessId, userId))
            return Forbid();

        return Page();
    }

    /// <summary>
    /// Initiates listing upgrade. In production this will redirect to PayFast/Stripe.
    /// For now, creates a 'pending payment' Listing record.
    /// </summary>
    public async Task<IActionResult> OnPostAsync(Guid businessId)
    {
        var userId = Guid.Parse(_userManager.GetUserId(User)!);

        if (!await _businessService.IsOwnerAsync(businessId, userId))
            return Forbid();

        if (Tier is not ("premium" or "featured"))
        {
            ModelState.AddModelError(nameof(Tier), "Invalid tier.");
            Business = await _businessService.GetByIdAsync(businessId);
            return Page();
        }

        var listing = new Listing
        {
            ListingId = Guid.NewGuid(),
            BusinessId = businessId,
            Tier = Tier,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(30),
            PaymentStatus = "pending"   // Updated to 'paid' after payment webhook
        };

        _db.Listings.Add(listing);
        await _db.SaveChangesAsync();
        await _audit.LogAsync(userId, $"Initiated listing upgrade to '{Tier}' for business id={businessId}");

        // TODO: Redirect to PayFast/Stripe checkout.
        // PayFast: https://www.payfast.co.za/developers
        // Stripe:  https://stripe.com/docs/payments/checkout
        TempData["UpgradeMessage"] = $"Your {Tier} listing request has been submitted. Payment integration coming soon.";
        return RedirectToPage("/Account/Dashboard");
    }
}
