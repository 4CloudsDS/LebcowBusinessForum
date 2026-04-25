namespace LebcowBusinessForum.Web.Models;

public class Listing
{
    public Guid ListingId { get; set; }
    public Guid BusinessId { get; set; }
    public string Tier { get; set; } = "free"; // free | premium | featured
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string PaymentStatus { get; set; } = "unpaid";

    // Navigation
    public Business? Business { get; set; }
}
