using System.ComponentModel.DataAnnotations;
using LebcowBusinessForum.Web.Data;
using LebcowBusinessForum.Web.Models;
using LebcowBusinessForum.Web.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace LebcowBusinessForum.Web.Pages;

public class BusinessDetailModel : PageModel
{
    private readonly ApplicationDbContext _db;
    private readonly IReviewService _reviewService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IConfiguration _config;

    public BusinessDetailModel(ApplicationDbContext db, IReviewService reviewService, UserManager<ApplicationUser> userManager, IConfiguration config)
    {
        _db = db;
        _reviewService = reviewService;
        _userManager = userManager;
        _config = config;
    }

    public Business? Business { get; private set; }
    public IReadOnlyList<Review> Reviews { get; private set; } = [];
    public double AverageRating { get; private set; }
    public int ReviewCount { get; private set; }
    public bool CanReview { get; private set; }

    [BindProperty]
    public ReviewInputModel ReviewInput { get; set; } = new();

    public class ReviewInputModel
    {
        [Range(1, 5)]
        public int Rating { get; set; } = 5;

        [Required, StringLength(1000)]
        public string Comment { get; set; } = string.Empty;
    }

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        Business = await _db.Businesses
            .Include(b => b.Category)
            .FirstOrDefaultAsync(b => b.BusinessId == id && b.Status == "approved");

        if (Business is null)
            return Page();

        Reviews = await _reviewService.GetForBusinessAsync(id);
        ReviewCount = Reviews.Count;
        AverageRating = ReviewCount > 0 ? Reviews.Average(r => r.Rating) : 0;

        ViewData["GoogleMapsKey"] = _config["AppSettings:GoogleMapsApiKey"] ?? string.Empty;

        if (User.Identity?.IsAuthenticated == true)
        {
            var userId = Guid.Parse(_userManager.GetUserId(User)!);
            CanReview = !await _reviewService.HasReviewedAsync(id, userId);
        }

        return Page();
    }

    public async Task<IActionResult> OnPostReviewAsync(Guid id)
    {
        if (!User.Identity?.IsAuthenticated == true)
            return Challenge();

        Business = await _db.Businesses
            .Include(b => b.Category)
            .FirstOrDefaultAsync(b => b.BusinessId == id && b.Status == "approved");

        if (Business is null) return Page();

        Reviews = await _reviewService.GetForBusinessAsync(id);

        if (!ModelState.IsValid)
            return Page();

        var userId = Guid.Parse(_userManager.GetUserId(User)!);
        var submitted = await _reviewService.SubmitAsync(id, userId, ReviewInput.Rating, ReviewInput.Comment);
        if (!submitted)
            ModelState.AddModelError(string.Empty, "You have already reviewed this business or an error occurred.");

        return RedirectToPage(new { id });
    }
}

