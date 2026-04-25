using System.ComponentModel.DataAnnotations;
using LebcowBusinessForum.Web.Data;
using LebcowBusinessForum.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace LebcowBusinessForum.Web.Pages;

public class NewsletterModel : PageModel
{
    private readonly ApplicationDbContext _db;

    public NewsletterModel(ApplicationDbContext db)
    {
        _db = db;
    }

    [BindProperty]
    [Required, EmailAddress]
    [Display(Name = "Email address")]
    public string Email { get; set; } = string.Empty;

    public bool Subscribed { get; private set; }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        var email = Email.Trim().ToLowerInvariant();
        var existing = await _db.NewsletterSubscriptions
            .FirstOrDefaultAsync(n => n.Email == email);

        if (existing is null)
        {
            _db.NewsletterSubscriptions.Add(new NewsletterSubscription
            {
                SubscriptionId = Guid.NewGuid(),
                Email = email,
                SubscribedAt = DateTime.UtcNow,
                IsActive = true
            });
            await _db.SaveChangesAsync();
        }
        else if (!existing.IsActive)
        {
            existing.IsActive = true;
            await _db.SaveChangesAsync();
        }

        Subscribed = true;
        return Page();
    }
}
