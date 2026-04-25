using LebcowBusinessForum.Web.Data;
using LebcowBusinessForum.Web.Models;
using LebcowBusinessForum.Web.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace LebcowBusinessForum.Web.Pages;

public class BrowseModel : PageModel
{
    private readonly ApplicationDbContext _db;
    private readonly IBusinessService _businessService;

    public BrowseModel(ApplicationDbContext db, IBusinessService businessService)
    {
        _db = db;
        _businessService = businessService;
    }

    public string Query { get; private set; } = string.Empty;
    public string Location { get; private set; } = string.Empty;
    public Guid? SelectedCategory { get; private set; }
    public IReadOnlyList<Category> AllCategories { get; private set; } = [];
    public IReadOnlyList<Business> Results { get; private set; } = [];

    public async Task OnGetAsync(string? q, string? location, Guid? category)
    {
        Query = q?.Trim() ?? string.Empty;
        Location = location?.Trim() ?? string.Empty;
        SelectedCategory = category;

        AllCategories = await _db.Categories
            .OrderBy(c => c.Name)
            .ToListAsync();

        Results = await _businessService.SearchAsync(Query, Location, category);
    }
}

