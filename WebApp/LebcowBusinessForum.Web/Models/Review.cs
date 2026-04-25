namespace LebcowBusinessForum.Web.Models;

public class Review
{
    public Guid ReviewId { get; set; }
    public Guid BusinessId { get; set; }
    public Guid UserId { get; set; }
    public int Rating { get; set; } // 1–5
    public string Comment { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Business? Business { get; set; }
    public ApplicationUser? User { get; set; }
}
