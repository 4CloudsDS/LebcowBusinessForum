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
public class EditModel : PageModel
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IBusinessService _businessService;
    private readonly IAuditService _audit;

    public EditModel(ApplicationDbContext db, UserManager<ApplicationUser> userManager, IBusinessService businessService, IAuditService audit)
    {
        _db = db;
        _userManager = userManager;
        _businessService = businessService;
        _audit = audit;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public IReadOnlyList<Category> Categories { get; private set; } = [];
    public Business? Business { get; private set; }

    public class InputModel
    {
        [Required, StringLength(200)]
        [Display(Name = "Business name")]
        public string Name { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Category")]
        public Guid CategoryId { get; set; }

        [Required, StringLength(2000)]
        public string Description { get; set; } = string.Empty;

        [Required, StringLength(300)]
        public string Address { get; set; } = string.Empty;

        [Required, Phone]
        [Display(Name = "Phone number")]
        public string Phone { get; set; } = string.Empty;

        [Required, EmailAddress]
        [Display(Name = "Email address")]
        public string Email { get; set; } = string.Empty;

        [Url]
        public string? Website { get; set; }
    }

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        var userId = Guid.Parse(_userManager.GetUserId(User)!);
        Business = await _businessService.GetByIdAsync(id);

        if (Business is null)
            return NotFound();

        // Only owner or admin can edit
        var user = await _userManager.FindByIdAsync(userId.ToString());
        var isAdmin = user is not null && await _userManager.IsInRoleAsync(user, "Admin");

        if (!isAdmin && Business.OwnerId != userId)
            return Forbid();

        Input = new InputModel
        {
            Name = Business.Name,
            CategoryId = Business.CategoryId,
            Description = Business.Description,
            Address = Business.Address,
            Phone = Business.Phone,
            Email = Business.Email,
            Website = Business.Website,
        };

        Categories = await _db.Categories.OrderBy(c => c.Name).ToListAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(Guid id)
    {
        var userId = Guid.Parse(_userManager.GetUserId(User)!);
        Categories = await _db.Categories.OrderBy(c => c.Name).ToListAsync();

        if (!ModelState.IsValid)
        {
            Business = await _businessService.GetByIdAsync(id);
            return Page();
        }

        var business = await _businessService.GetByIdAsync(id);
        if (business is null) return NotFound();

        var user = await _userManager.FindByIdAsync(userId.ToString());
        var isAdmin = user is not null && await _userManager.IsInRoleAsync(user, "Admin");

        if (!isAdmin && business.OwnerId != userId)
            return Forbid();

        business.Name = Input.Name;
        business.CategoryId = Input.CategoryId;
        business.Description = Input.Description;
        business.Address = Input.Address;
        business.Phone = Input.Phone;
        business.Email = Input.Email;
        business.Website = Input.Website;

        // Reset to pending so admin reviews changes (unless admin is editing)
        if (!isAdmin)
            business.Status = "pending";

        await _businessService.UpdateAsync(business);
        await _audit.LogAsync(userId, $"Modified business '{business.Name}' (id={id}), status reset to pending");

        TempData["EditMessage"] = isAdmin
            ? $"'{business.Name}' updated successfully."
            : $"Your changes to '{business.Name}' have been submitted for review.";

        return RedirectToPage("/Account/Dashboard");
    }
}
