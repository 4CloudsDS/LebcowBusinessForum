namespace LebcowBusinessForum.Web.Models;

public class BusinessEvent
{
    public Guid EventId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string Location { get; set; } = string.Empty;
    public Guid? OrganizerId { get; set; }

    // Navigation
    public ApplicationUser? Organizer { get; set; }
}
