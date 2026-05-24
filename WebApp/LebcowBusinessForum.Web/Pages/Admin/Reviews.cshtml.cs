using LebcowBusinessForum.Web.Data;
using LebcowBusinessForum.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace LebcowBusinessForum.Web.Pages.Admin;

[Authorize(Roles = "Admin")]
public class ReviewsModel : PageModel
{
    private readonly ApplicationDbContext _db;

    public ReviewsModel(ApplicationDbContext db) => _db = db;

    public IReadOnlyList<Review> Reviews { get; private set; } = [];

    public async Task OnGetAsync()
    {
        Reviews = await _db.Reviews
            .Include(r => r.Business)
            .Include(r => r.User)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        var review = await _db.Reviews.FindAsync(id);
        if (review is not null)
        {
            _db.Reviews.Remove(review);
            await _db.SaveChangesAsync();
            TempData["Message"] = "Review removed.";
        }
        return RedirectToPage();
    }
}
