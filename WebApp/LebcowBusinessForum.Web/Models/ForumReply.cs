namespace LebcowBusinessForum.Web.Models;

public class ForumReply
{
    public Guid ReplyId { get; set; }
    public Guid PostId { get; set; }
    public Guid AuthorId { get; set; }
    public string Body { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ForumPost? Post { get; set; }
    public ApplicationUser? Author { get; set; }
}
