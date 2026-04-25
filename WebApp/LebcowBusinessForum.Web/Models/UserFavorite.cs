namespace LebcowBusinessForum.Web.Models;

/// <summary>Represents a user bookmarking/favouriting a business.</summary>
public class UserFavorite
{
    public Guid UserId { get; set; }
    public Guid BusinessId { get; set; }
    public DateTime SavedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ApplicationUser? User { get; set; }
    public Business? Business { get; set; }
}
