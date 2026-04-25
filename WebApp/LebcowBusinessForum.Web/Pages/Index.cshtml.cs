using LebcowBusinessForum.Web.Data;
using LebcowBusinessForum.Web.Models;
using LebcowBusinessForum.Web.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace LebcowBusinessForum.Web.Pages;

public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _db;
    private readonly IBusinessService _businessService;

    public IndexModel(ApplicationDbContext db, IBusinessService businessService)
    {
        _db = db;
        _businessService = businessService;
    }

    public IReadOnlyList<Category> FeaturedCategories { get; private set; } = [];
    public IReadOnlyList<Business> FeaturedBusinesses { get; private set; } = [];
    public IReadOnlyList<BusinessEvent> UpcomingEvents { get; private set; } = [];

    public async Task OnGetAsync()
    {
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

