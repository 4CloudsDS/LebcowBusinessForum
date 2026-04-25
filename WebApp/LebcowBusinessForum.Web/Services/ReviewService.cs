using LebcowBusinessForum.Web.Data;
using LebcowBusinessForum.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace LebcowBusinessForum.Web.Services;

public class ReviewService : IReviewService
{
    private readonly ApplicationDbContext _db;
    private readonly IAuditService _audit;

    public ReviewService(ApplicationDbContext db, IAuditService audit)
    {
        _db = db;
        _audit = audit;
    }

    public async Task<IReadOnlyList<Review>> GetForBusinessAsync(Guid businessId)
        => await _db.Reviews
            .Include(r => r.User)
            .Where(r => r.BusinessId == businessId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

    public async Task<bool> HasReviewedAsync(Guid businessId, Guid userId)
        => await _db.Reviews.AnyAsync(r => r.BusinessId == businessId && r.UserId == userId);

    public async Task<bool> SubmitAsync(Guid businessId, Guid userId, int rating, string comment)
    {
        if (rating < 1 || rating > 5) return false;
        if (await HasReviewedAsync(businessId, userId)) return false;

        _db.Reviews.Add(new Review
        {
            ReviewId = Guid.NewGuid(),
            BusinessId = businessId,
            UserId = userId,
            Rating = rating,
            Comment = comment.Trim(),
            CreatedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();
        await _audit.LogAsync(userId, $"Submitted review for business id={businessId}, rating={rating}");
        return true;
    }
}
