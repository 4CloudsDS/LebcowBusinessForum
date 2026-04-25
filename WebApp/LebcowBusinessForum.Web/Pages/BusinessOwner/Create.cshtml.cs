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
public class CreateModel : PageModel
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IBusinessService _businessService;

    public CreateModel(ApplicationDbContext db, UserManager<ApplicationUser> userManager, IBusinessService businessService)
    {
        _db = db;
        _userManager = userManager;
        _businessService = businessService;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public IReadOnlyList<Category> Categories { get; private set; } = [];

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

    public async Task OnGetAsync()
    {
        Categories = await _db.Categories.OrderBy(c => c.Name).ToListAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        Categories = await _db.Categories.OrderBy(c => c.Name).ToListAsync();

        if (!ModelState.IsValid)
            return Page();

        var ownerId = Guid.Parse(_userManager.GetUserId(User)!);

        var business = new Business
        {
            BusinessId = Guid.NewGuid(),
            Name = Input.Name,
            CategoryId = Input.CategoryId,
            Description = Input.Description,
            Address = Input.Address,
            Phone = Input.Phone,
            Email = Input.Email,
            Website = Input.Website,
            Status = "pending",
            OwnerId = ownerId,
        };

        await _businessService.CreateAsync(business);

        return RedirectToPage("/Account/Dashboard");
    }
}
