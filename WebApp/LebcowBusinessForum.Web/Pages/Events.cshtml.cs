using LebcowBusinessForum.Web.Data;
using LebcowBusinessForum.Web.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace LebcowBusinessForum.Web.Pages;

public class EventsModel : PageModel
{
    private readonly ApplicationDbContext _db;

    public EventsModel(ApplicationDbContext db)
    {
        _db = db;
    }

    public IReadOnlyList<BusinessEvent> Events { get; private set; } = [];

    public async Task OnGetAsync()
    {
        Events = await _db.Events
            .Where(e => e.Date >= DateTime.UtcNow)
            .OrderBy(e => e.Date)
            .ToListAsync();
    }
}
