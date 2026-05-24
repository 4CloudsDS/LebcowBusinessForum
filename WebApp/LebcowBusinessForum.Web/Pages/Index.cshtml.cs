using LebcowBusinessForum.Web.Data;
using LebcowBusinessForum.Web.Models;
using LebcowBusinessForum.Web.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace LebcowBusinessForum.Web.Pages;

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

    public IReadOnlyList<Category> FeaturedCategories { get; private set; } = [];
    public IReadOnlyList<Business> FeaturedBusinesses { get; private set; } = [];
    public IReadOnlyList<BusinessEvent> UpcomingEvents { get; private set; } = [];
    public string? WelcomeName { get; private set; }

    public async Task OnGetAsync()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            var user = await _userManager.GetUserAsync(User);
            WelcomeName = user?.FullName ?? user?.UserName ?? User.Identity?.Name;
        }

        FeaturedCategories = await _db.Categories
            .Where(c => c.ParentCategoryId == null)
            .OrderBy(c => c.Name)
            .Take(8)
            .ToListAsync();

        FeaturedBusinesses = await _businessService.GetFeaturedAsync(6);

        UpcomingEvents = await _db.Events
            .Where(e => e.Date >= DateTime.UtcNow)
            .OrderBy(e => e.Date)
            .Take(3)
            .ToListAsync();
    }
}

