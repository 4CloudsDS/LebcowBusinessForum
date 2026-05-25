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
    public HashSet<Guid> AdminAuthorIds { get; private set; } = [];
    public int TotalPages { get; private set; }
    public string? CurrentUserId { get; private set; }
    private const int PageSize = 10;

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    [BindProperty(SupportsGet = true)]
    public bool PrioritizeOfficial { get; set; }

    [BindProperty]
    public string NewPostTitle { get; set; } = string.Empty;

    [BindProperty]
    public string NewPostBody { get; set; } = string.Empty;

    [BindProperty]
    public string ReplyBody { get; set; } = string.Empty;

    public async Task OnGetAsync()
    {
        CurrentUserId = User.Identity?.IsAuthenticated == true
            ? _userManager.GetUserId(User)
            : null;

        var adminRoleId = await _db.Roles
            .Where(r => r.Name == "Admin")
            .Select(r => r.Id)
            .FirstOrDefaultAsync();

        if (adminRoleId != Guid.Empty)
        {
            var adminUserIds = await _db.UserRoles
                .Where(ur => ur.RoleId == adminRoleId)
                .Select(ur => ur.UserId)
                .ToListAsync();

            AdminAuthorIds = adminUserIds.ToHashSet();
        }

        var totalCount = await _db.ForumPosts.CountAsync();
        TotalPages = (int)Math.Ceiling(totalCount / (double)PageSize);
        PageNumber = Math.Max(1, Math.Min(PageNumber, TotalPages == 0 ? 1 : TotalPages));

        Posts = await _db.ForumPosts
            .Include(p => p.Author)
            .Include(p => p.Replies.OrderBy(r => r.CreatedAt))
                .ThenInclude(r => r.Author)
            .OrderByDescending(p => p.CreatedAt)
            .Skip((PageNumber - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync();

        if (PrioritizeOfficial && Posts.Count > 0)
        {
            Posts = Posts
                .OrderByDescending(p => AdminAuthorIds.Contains(p.AuthorId))
                .ThenByDescending(p => p.CreatedAt)
                .ToList();
        }
    }

    public async Task<IActionResult> OnPostDeletePostAsync(Guid postId)
    {
        if (!User.IsInRole("Admin")) return Forbid();
        var post = await _db.ForumPosts.FindAsync(postId);
        if (post is not null)
        {
            _db.ForumPosts.Remove(post);
            await _db.SaveChangesAsync();
        }
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostReplyAsync(Guid postId)
    {
        if (User.Identity?.IsAuthenticated != true) return Challenge();
        if (string.IsNullOrWhiteSpace(ReplyBody))
        {
            ModelState.AddModelError(string.Empty, "Reply cannot be empty.");
            await OnGetAsync();
            return Page();
        }
        var post = await _db.ForumPosts.FindAsync(postId);
        if (post is null) return NotFound();

        var userId = Guid.Parse(_userManager.GetUserId(User)!);
        _db.ForumReplies.Add(new ForumReply
        {
            ReplyId = Guid.NewGuid(),
            PostId = postId,
            AuthorId = userId,
            Body = ReplyBody.Trim()[..Math.Min(ReplyBody.Trim().Length, 5000)],
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
        return RedirectToPage(new { pageNumber = PageNumber, prioritizeOfficial = PrioritizeOfficial });
    }

    public async Task<IActionResult> OnPostFlagAsync(Guid postId)
    {
        if (User.Identity?.IsAuthenticated != true) return Challenge();
        var post = await _db.ForumPosts.FindAsync(postId);
        if (post is null) return NotFound();
        post.IsFlagged = true;
        await _db.SaveChangesAsync();
        return RedirectToPage(new { pageNumber = PageNumber, prioritizeOfficial = PrioritizeOfficial });
    }

    public async Task<IActionResult> OnPostUnflagAsync(Guid postId)
    {
        if (!User.IsInRole("Admin")) return Forbid();
        var post = await _db.ForumPosts.FindAsync(postId);
        if (post is null) return NotFound();
        post.IsFlagged = false;
        await _db.SaveChangesAsync();
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteReplyAsync(Guid replyId)
    {
        if (!User.IsInRole("Admin")) return Forbid();
        var reply = await _db.ForumReplies.FindAsync(replyId);
        if (reply is not null)
        {
            _db.ForumReplies.Remove(reply);
            await _db.SaveChangesAsync();
        }
        return RedirectToPage();
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
