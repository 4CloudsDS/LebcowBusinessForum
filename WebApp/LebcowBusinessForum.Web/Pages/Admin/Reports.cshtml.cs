using LebcowBusinessForum.Web.Data;
using LebcowBusinessForum.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace LebcowBusinessForum.Web.Pages.Admin;

[Authorize(Roles = "Admin")]
public class ReportsModel : PageModel
{
    private readonly ApplicationDbContext _db;

    public ReportsModel(ApplicationDbContext db) => _db = db;

    // Summary stats
    public int TotalUsers { get; private set; }
    public int TotalBusinesses { get; private set; }
    public int ApprovedBusinesses { get; private set; }
    public int PendingBusinesses { get; private set; }
    public int RejectedBusinesses { get; private set; }
    public int TotalReviews { get; private set; }
    public int TotalEvents { get; private set; }
    public int TotalListings { get; private set; }
    public int FeaturedListings { get; private set; }
    public int PremiumListings { get; private set; }
    public int FreeListings { get; private set; }

    // Breakdowns
    public IReadOnlyList<(string Region, int Count)> ByRegion { get; private set; } = [];
    public IReadOnlyList<(string Category, int Count)> ByCategory { get; private set; } = [];

    public async Task OnGetAsync()
    {
        TotalUsers = await _db.Users.CountAsync();
        TotalBusinesses = await _db.Businesses.CountAsync();
        ApprovedBusinesses = await _db.Businesses.CountAsync(b => b.Status == "approved");
        PendingBusinesses = await _db.Businesses.CountAsync(b => b.Status == "pending");
        RejectedBusinesses = await _db.Businesses.CountAsync(b => b.Status == "rejected");
        TotalReviews = await _db.Reviews.CountAsync();
        TotalEvents = await _db.Events.CountAsync();
        TotalListings = await _db.Listings.CountAsync();
        FeaturedListings = await _db.Listings.CountAsync(l => l.Tier == "featured");
        PremiumListings = await _db.Listings.CountAsync(l => l.Tier == "premium");
        FreeListings = await _db.Listings.CountAsync(l => l.Tier == "free");

        ByRegion = await _db.Businesses
            .Where(b => b.Region != null)
            .GroupBy(b => b.Region!)
            .Select(g => new { Region = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ToListAsync()
            .ContinueWith(t => (IReadOnlyList<(string, int)>)t.Result.Select(x => (x.Region, x.Count)).ToList());

        ByCategory = await _db.Businesses
            .Include(b => b.Category)
            .Where(b => b.Category != null)
            .GroupBy(b => b.Category!.Name)
            .Select(g => new { Category = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ToListAsync()
            .ContinueWith(t => (IReadOnlyList<(string, int)>)t.Result.Select(x => (x.Category, x.Count)).ToList());
    }
}
