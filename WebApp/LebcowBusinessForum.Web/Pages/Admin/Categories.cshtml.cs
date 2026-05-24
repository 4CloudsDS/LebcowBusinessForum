using System.ComponentModel.DataAnnotations;
using LebcowBusinessForum.Web.Data;
using LebcowBusinessForum.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace LebcowBusinessForum.Web.Pages.Admin;

[Authorize(Roles = "Admin")]
public class CategoriesModel : PageModel
{
    private readonly ApplicationDbContext _db;

    public CategoriesModel(ApplicationDbContext db) => _db = db;

    public IReadOnlyList<Category> TopLevel { get; private set; } = [];

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        [Required, StringLength(100)]
        public string Name { get; set; } = string.Empty;

        public Guid? ParentCategoryId { get; set; }
    }

    public IReadOnlyList<Category> AllCategories { get; private set; } = [];

    public async Task OnGetAsync()
    {
        AllCategories = await _db.Categories.OrderBy(c => c.Name).ToListAsync();
        TopLevel = await _db.Categories
            .Include(c => c.SubCategories)
            .Where(c => c.ParentCategoryId == null)
            .OrderBy(c => c.Name)
            .ToListAsync();
    }

    public async Task<IActionResult> OnPostCreateAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadAsync();
            return Page();
        }

        _db.Categories.Add(new Category
        {
            CategoryId = Guid.NewGuid(),
            Name = Input.Name.Trim(),
            ParentCategoryId = Input.ParentCategoryId == Guid.Empty ? null : Input.ParentCategoryId,
        });

        await _db.SaveChangesAsync();
        TempData["Message"] = $"Category '{Input.Name}' created.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        var cat = await _db.Categories.Include(c => c.SubCategories).FirstOrDefaultAsync(c => c.CategoryId == id);
        if (cat is null) return NotFound();

        if (cat.SubCategories.Count > 0)
        {
            TempData["Error"] = $"Cannot delete '{cat.Name}': it has sub-categories. Delete them first.";
            return RedirectToPage();
        }

        bool hasBusinesses = await _db.Businesses.AnyAsync(b => b.CategoryId == id);
        if (hasBusinesses)
        {
            TempData["Error"] = $"Cannot delete '{cat.Name}': businesses are assigned to it.";
            return RedirectToPage();
        }

        _db.Categories.Remove(cat);
        await _db.SaveChangesAsync();
        TempData["Message"] = $"Category '{cat.Name}' deleted.";
        return RedirectToPage();
    }

    private async Task LoadAsync()
    {
        AllCategories = await _db.Categories.OrderBy(c => c.Name).ToListAsync();
        TopLevel = await _db.Categories
            .Include(c => c.SubCategories)
            .Where(c => c.ParentCategoryId == null)
            .OrderBy(c => c.Name)
            .ToListAsync();
    }
}
