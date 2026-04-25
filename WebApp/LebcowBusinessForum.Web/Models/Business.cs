namespace LebcowBusinessForum.Web.Models;

public class Business
{
    public Guid BusinessId { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid CategoryId { get; set; }
    public string Address { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Website { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? LogoUrl { get; set; }
    public string Status { get; set; } = "pending";
    public string? Region { get; set; }
    public Guid? OwnerId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Category? Category { get; set; }
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
    public ICollection<Listing> Listings { get; set; } = new List<Listing>();
    public ICollection<UserFavorite> Favorites { get; set; } = new List<UserFavorite>();
}

