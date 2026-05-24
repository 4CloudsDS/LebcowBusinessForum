using LebcowBusinessForum.Web.Data;
using LebcowBusinessForum.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace LebcowBusinessForum.Web.Pages;

public class EventsModel : PageModel
{
    private readonly ApplicationDbContext _db;

    public EventsModel(ApplicationDbContext db)
    {
        _db = db;
    }

    public IReadOnlyList<BusinessEvent> Events { get; private set; } = [];
    public BusinessEvent? SelectedEvent { get; private set; }
    public bool CanCreateEvents { get; private set; }

    [BindProperty]
    public CreateEventInput Input { get; set; } = new();

    public class CreateEventInput
    {
        [Required, StringLength(200, MinimumLength = 5)]
        public string Title { get; set; } = string.Empty;

        [Required, StringLength(4000, MinimumLength = 20)]
        public string Description { get; set; } = string.Empty;

        [Required, StringLength(500, MinimumLength = 3)]
        public string Location { get; set; } = string.Empty;

        [Required]
        public DateTime Date { get; set; } = DateTime.UtcNow.AddDays(7);
    }

    public async Task<IActionResult> OnGetAsync(Guid? id)
    {
        CanCreateEvents = User.IsInRole("Admin") || User.IsInRole("BusinessOwner");

        if (id.HasValue)
        {
            SelectedEvent = await _db.Events
                .Include(e => e.Organizer)
                .FirstOrDefaultAsync(e => e.EventId == id.Value);

            if (SelectedEvent is null)
                return NotFound();

            return Page();
        }

        Events = await _db.Events
            .Include(e => e.Organizer)
            .OrderBy(e => e.Date)
            .ToListAsync();

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (User.Identity?.IsAuthenticated != true)
            return Challenge();

        if (!(User.IsInRole("Admin") || User.IsInRole("BusinessOwner")))
            return Forbid();

        if (Input.Date < DateTime.UtcNow.AddMinutes(5))
            ModelState.AddModelError("Input.Date", "Event date must be in the future.");

        if (!ModelState.IsValid)
        {
            CanCreateEvents = true;
            Events = await _db.Events
                .Include(e => e.Organizer)
                .OrderBy(e => e.Date)
                .ToListAsync();
            return Page();
        }

        Guid? organizerId = null;
        var rawUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (Guid.TryParse(rawUserId, out var parsedUserId))
            organizerId = parsedUserId;

        _db.Events.Add(new BusinessEvent
        {
            EventId = Guid.NewGuid(),
            Title = Input.Title.Trim(),
            Description = Input.Description.Trim(),
            Location = Input.Location.Trim(),
            Date = DateTime.SpecifyKind(Input.Date, DateTimeKind.Utc),
            OrganizerId = organizerId
        });

        await _db.SaveChangesAsync();
        return RedirectToPage();
    }
}
