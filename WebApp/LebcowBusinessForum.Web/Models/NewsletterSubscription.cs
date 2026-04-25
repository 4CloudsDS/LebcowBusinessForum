namespace LebcowBusinessForum.Web.Models;

public class NewsletterSubscription
{
    public Guid SubscriptionId { get; set; }
    public string Email { get; set; } = string.Empty;
    public DateTime SubscribedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
}
