using LebcowBusinessForum.Web.Models;

namespace LebcowBusinessForum.Web.Services;

public interface IReviewService
{
    Task<IReadOnlyList<Review>> GetForBusinessAsync(Guid businessId);
    /// <summary>Returns false if the user has already reviewed this business.</summary>
    Task<bool> SubmitAsync(Guid businessId, Guid userId, int rating, string comment);
    Task<bool> HasReviewedAsync(Guid businessId, Guid userId);
}
