using LebcowBusinessForum.Web.Data;
using LebcowBusinessForum.Web.Models;
using LebcowBusinessForum.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace LebcowBusinessForum.Web.Pages.Account;

[Authorize]
public class DashboardModel : PageModel
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IBusinessService _businessService;

    public DashboardModel(ApplicationDbContext db, UserManager<ApplicationUser> userManager, IBusinessService businessService)
    {
        _db = db;
        _userManager = userManager;
        _businessService = businessService;
    }

    public IReadOnlyList<Business> MyBusinesses { get; private set; } = [];
    public IReadOnlyList<Business> SavedBusinesses { get; private set; } = [];
    public bool IsAdmin { get; private set; }
    public bool IsBusinessOwner { get; private set; }
    public int AdminTotalUsers { get; private set; }
    public int AdminTotalBusinesses { get; private set; }
    public int AdminPendingBusinesses { get; private set; }
    public int AdminPendingUpgrades { get; private set; }
    public string? StatusMessage => TempData.Peek("UpgradeMessage") as string
                                 ?? TempData.Peek("EditMessage") as string;

    public async Task OnGetAsync()
    {
        var userId = Guid.Parse(_userManager.GetUserId(User)!);
        var user = await _userManager.FindByIdAsync(userId.ToString());

        IsAdmin = user is not null && await _userManager.IsInRoleAsync(user, "Admin");
        IsBusinessOwner = user is not null && await _userManager.IsInRoleAsync(user, "BusinessOwner");

        // Load businesses for owner and regular users alike (all users can add businesses)
        MyBusinesses = await _businessService.GetByOwnerAsync(userId);

        SavedBusinesses = await _db.UserFavorites
            .Where(f => f.UserId == userId)
            .Include(f => f.Business)
            .ThenInclude(b => b!.Category)
            .Select(f => f.Business!)
            .OrderBy(b => b.Name)
            .ToListAsync();

        if (IsAdmin)
        {
            AdminTotalUsers = await _db.Users.CountAsync();
            AdminTotalBusinesses = await _db.Businesses.CountAsync();
            AdminPendingBusinesses = await _db.Businesses.CountAsync(b => b.Status == "pending");
            AdminPendingUpgrades = await _db.Listings.CountAsync(l => l.PaymentStatus == "pending");
        }
    }
}
