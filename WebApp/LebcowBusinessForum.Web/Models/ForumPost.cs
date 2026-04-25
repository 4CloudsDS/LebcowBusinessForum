namespace LebcowBusinessForum.Web.Models;

public class ForumPost
{
    public Guid PostId { get; set; }
    public Guid AuthorId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ApplicationUser? Author { get; set; }
}
