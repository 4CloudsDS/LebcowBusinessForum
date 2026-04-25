using LebcowBusinessForum.Web.Data;
using LebcowBusinessForum.Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace LebcowBusinessForum.Web.Pages;

public class ForumModel : PageModel
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public ForumModel(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public IReadOnlyList<ForumPost> Posts { get; private set; } = [];
    public int TotalPages { get; private set; }
    private const int PageSize = 10;

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    [BindProperty]
    public string NewPostTitle { get; set; } = string.Empty;

    [BindProperty]
    public string NewPostBody { get; set; } = string.Empty;

    public async Task OnGetAsync()
    {
        var totalCount = await _db.ForumPosts.CountAsync();
        TotalPages = (int)Math.Ceiling(totalCount / (double)PageSize);
        PageNumber = Math.Max(1, Math.Min(PageNumber, TotalPages == 0 ? 1 : TotalPages));

        Posts = await _db.ForumPosts
            .Include(p => p.Author)
            .OrderByDescending(p => p.CreatedAt)
            .Skip((PageNumber - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return Challenge();
        }

        if (string.IsNullOrWhiteSpace(NewPostTitle) || string.IsNullOrWhiteSpace(NewPostBody))
        {
            ModelState.AddModelError(string.Empty, "Title and body are required.");
            await OnGetAsync();
            return Page();
        }

        var userId = Guid.Parse(_userManager.GetUserId(User)!);
        _db.ForumPosts.Add(new ForumPost
        {
            PostId = Guid.NewGuid(),
            Title = NewPostTitle.Trim()[..Math.Min(NewPostTitle.Length, 200)],
            Body = NewPostBody.Trim(),
            AuthorId = userId,
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
        return RedirectToPage();
    }
}
