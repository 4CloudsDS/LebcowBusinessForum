using LebcowBusinessForum.Web.Models;

namespace LebcowBusinessForum.Web.Services;

public interface IBusinessService
{
    Task<IReadOnlyList<Business>> GetByOwnerAsync(Guid ownerId);
    Task<Business?> GetByIdAsync(Guid businessId);
    Task CreateAsync(Business business);
    Task UpdateAsync(Business business);
    Task<bool> ApproveAsync(Guid businessId, Guid adminUserId);
    Task<bool> RejectAsync(Guid businessId, Guid adminUserId);
    Task<bool> IsOwnerAsync(Guid businessId, Guid userId);
    Task<IReadOnlyList<Business>> SearchAsync(string? query, string? location, Guid? categoryId, int take = 100);
    Task<IReadOnlyList<Business>> GetFeaturedAsync(int take = 6);
}
