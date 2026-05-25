using LebcowBusinessForum.Web.Data;
using LebcowBusinessForum.Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace LebcowBusinessForum.Web.Services;

public class BusinessService : IBusinessService
{
    private readonly ApplicationDbContext _db;
    private readonly IAuditService _audit;
    private readonly UserManager<ApplicationUser> _userManager;

    public BusinessService(ApplicationDbContext db, IAuditService audit, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _audit = audit;
        _userManager = userManager;
    }

    public async Task<IReadOnlyList<Business>> GetByOwnerAsync(Guid ownerId)
        => await _db.Businesses
            .Include(b => b.Category)
            .Include(b => b.Listings)
            .Where(b => b.OwnerId == ownerId)
            .OrderBy(b => b.Name)
            .ToListAsync();

    public async Task UpdateAsync(Business business)
    {
        _db.Businesses.Update(business);
        await _db.SaveChangesAsync();
        await _audit.LogAsync(business.OwnerId ?? Guid.Empty, $"Updated business '{business.Name}' (id={business.BusinessId})");
    }

    public async Task<Business?> GetByIdAsync(Guid businessId)
        => await _db.Businesses
            .Include(b => b.Category)
            .FirstOrDefaultAsync(b => b.BusinessId == businessId);

    public async Task CreateAsync(Business business)
    {
        _db.Businesses.Add(business);
        await _db.SaveChangesAsync();
        await _audit.LogAsync(business.OwnerId ?? Guid.Empty, $"Created business '{business.Name}' (id={business.BusinessId})");
    }

    public async Task<bool> ApproveAsync(Guid businessId, Guid adminUserId)
    {
        var business = await _db.Businesses.FindAsync(businessId);
        if (business is null) return false;
        business.Status = "approved";
        await _db.SaveChangesAsync();

        if (business.OwnerId.HasValue)
        {
            var owner = await _userManager.FindByIdAsync(business.OwnerId.Value.ToString());
            if (owner is not null && !await _userManager.IsInRoleAsync(owner, "BusinessOwner"))
            {
                await _userManager.AddToRoleAsync(owner, "BusinessOwner");
            }
        }

        await _audit.LogAsync(adminUserId, $"Approved business '{business.Name}' (id={businessId})");
        return true;
    }

    public async Task<bool> RejectAsync(Guid businessId, Guid adminUserId)
    {
        var business = await _db.Businesses.FindAsync(businessId);
        if (business is null) return false;
        business.Status = "rejected";
        await _db.SaveChangesAsync();

        if (business.OwnerId.HasValue)
        {
            var hasOtherApproved = await _db.Businesses.AnyAsync(b =>
                b.OwnerId == business.OwnerId &&
                b.BusinessId != businessId &&
                b.Status == "approved");

            if (!hasOtherApproved)
            {
                var owner = await _userManager.FindByIdAsync(business.OwnerId.Value.ToString());
                if (owner is not null && await _userManager.IsInRoleAsync(owner, "BusinessOwner"))
                {
                    await _userManager.RemoveFromRoleAsync(owner, "BusinessOwner");
                }
            }
        }

        await _audit.LogAsync(adminUserId, $"Rejected business '{business.Name}' (id={businessId})");
        return true;
    }

    public async Task<bool> IsOwnerAsync(Guid businessId, Guid userId)
        => await _db.Businesses.AnyAsync(b => b.BusinessId == businessId && b.OwnerId == userId);

    public async Task<IReadOnlyList<Business>> SearchAsync(string? query, string? location, Guid? categoryId, int take = 100)
    {
        var q = _db.Businesses
            .Include(b => b.Category)
            .Where(b => b.Status == "approved");

        if (!string.IsNullOrWhiteSpace(query))
            q = q.Where(b => b.Name.Contains(query) || b.Description.Contains(query));

        if (!string.IsNullOrWhiteSpace(location))
            q = q.Where(b => b.Address.Contains(location) || b.Region == location);

        if (categoryId.HasValue)
            q = q.Where(b => b.CategoryId == categoryId.Value);

        return await q.OrderBy(b => b.Name).Take(take).ToListAsync();
    }

    public async Task<bool> DeleteAsync(Guid businessId, Guid actorUserId)
    {
        var business = await _db.Businesses.FindAsync(businessId);
        if (business is null) return false;

        // Apply demotion before removing the record
        if (business.OwnerId.HasValue)
        {
            var hasOtherApproved = await _db.Businesses.AnyAsync(b =>
                b.OwnerId == business.OwnerId &&
                b.BusinessId != businessId &&
                b.Status == "approved");

            if (!hasOtherApproved)
            {
                var owner = await _userManager.FindByIdAsync(business.OwnerId.Value.ToString());
                if (owner is not null && await _userManager.IsInRoleAsync(owner, "BusinessOwner"))
                    await _userManager.RemoveFromRoleAsync(owner, "BusinessOwner");
            }
        }

        _db.Businesses.Remove(business);
        await _db.SaveChangesAsync();
        await _audit.LogAsync(actorUserId, $"Permanently deleted business '{business.Name}' (id={businessId})");
        return true;
    }

    public async Task<bool> DelistAsync(Guid businessId, Guid actorUserId)
    {
        var business = await _db.Businesses.FindAsync(businessId);
        if (business is null) return false;
        business.Status = "delisted";
        await _db.SaveChangesAsync();

        if (business.OwnerId.HasValue)
        {
            var hasOtherApproved = await _db.Businesses.AnyAsync(b =>
                b.OwnerId == business.OwnerId &&
                b.BusinessId != businessId &&
                b.Status == "approved");

            if (!hasOtherApproved)
            {
                var owner = await _userManager.FindByIdAsync(business.OwnerId.Value.ToString());
                if (owner is not null && await _userManager.IsInRoleAsync(owner, "BusinessOwner"))
                {
                    await _userManager.RemoveFromRoleAsync(owner, "BusinessOwner");
                }
            }
        }

        await _audit.LogAsync(actorUserId, $"Delisted business '{business.Name}' (id={businessId})");
        return true;
    }

    public async Task<IReadOnlyList<Business>> GetFeaturedAsync(int take = 6)
        => await _db.Businesses
            .Include(b => b.Category)
            .Where(b => b.Status == "approved")
            .OrderByDescending(b => b.CreatedAt)
            .Take(take)
            .ToListAsync();
}
