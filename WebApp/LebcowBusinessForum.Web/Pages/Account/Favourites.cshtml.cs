using LebcowBusinessForum.Web.Data;
using LebcowBusinessForum.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace LebcowBusinessForum.Web.Pages.Account;

[Authorize]
public class FavouritesModel : PageModel
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public FavouritesModel(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    /// <summary>POST /Account/Favourites?handler=Toggle&businessId={id}</summary>
    public async Task<IActionResult> OnPostToggleAsync(Guid businessId)
    {
        var userId = Guid.Parse(_userManager.GetUserId(User)!);
        var existing = await _db.UserFavorites
            .FirstOrDefaultAsync(f => f.UserId == userId && f.BusinessId == businessId);

        if (existing is null)
        {
            _db.UserFavorites.Add(new UserFavorite
            {
                UserId = userId,
                BusinessId = businessId,
                SavedAt = DateTime.UtcNow
            });
        }
        else
        {
            _db.UserFavorites.Remove(existing);
        }

        await _db.SaveChangesAsync();

        // Return to the calling page (or dashboard)
        string? returnUrl = Request.Headers.Referer.ToString();
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

        return RedirectToPage("/Account/Dashboard");
    }
}
