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

    public async Task OnGetAsync()
    {
        var userId = Guid.Parse(_userManager.GetUserId(User)!);

        MyBusinesses = await _businessService.GetByOwnerAsync(userId);

        SavedBusinesses = await _db.UserFavorites
            .Where(f => f.UserId == userId)
            .Select(f => f.Business!)
            .Include(b => b.Category)
            .OrderBy(b => b.Name)
            .ToListAsync();
    }
}
