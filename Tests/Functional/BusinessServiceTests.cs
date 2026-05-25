using LebcowBusinessForum.Web.Data;
using LebcowBusinessForum.Web.Models;
using LebcowBusinessForum.Web.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LebcowBusinessForum.Tests.Functional;

/// <summary>
/// Stage 2 — Functional Tests: BusinessService unit tests (TestAnalyst)
/// Run: dotnet test --filter Category=Functional
/// </summary>
public class BusinessServiceTests : IDisposable
{
    private readonly ApplicationDbContext _db;
    private readonly BusinessService _sut;
    private readonly AuditService _audit;

    public BusinessServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase("BusinessServiceTests_" + Guid.NewGuid())
            .Options;
        _db = new ApplicationDbContext(options);
        _audit = new AuditService(_db);
        _sut = new BusinessService(_db, _audit, null!);
    }

    public void Dispose() => _db.Dispose();

    // ── Helpers ─────────────────────────────────────────────────────────

    private async Task<(Category cat, Business biz)> SeedApprovedBusinessAsync(
        string name = "Test Biz", string address = "Johannesburg, GP", string? region = null)
    {
        var cat = new Category { CategoryId = Guid.NewGuid(), Name = "Tech" };
        _db.Categories.Add(cat);

        var biz = new Business
        {
            BusinessId = Guid.NewGuid(),
            Name = name,
            Description = "A description for " + name,
            Address = address,
            Phone = "+27 11 000 0001",
            Email = "info@test.co.za",
            CategoryId = cat.CategoryId,
            Status = "approved",
            Region = region
        };
        _db.Businesses.Add(biz);
        await _db.SaveChangesAsync();
        return (cat, biz);
    }

    // ── CreateAsync ──────────────────────────────────────────────────────

    [Fact]
    [Trait("Category", "Functional")]
    public async Task CreateAsync_PersistsBusiness()
    {
        var cat = new Category { CategoryId = Guid.NewGuid(), Name = "Retail" };
        _db.Categories.Add(cat);
        await _db.SaveChangesAsync();

        var biz = new Business
        {
            BusinessId = Guid.NewGuid(),
            Name = "New Shop",
            Description = "A new retail shop",
            Address = "Cape Town, WC",
            Phone = "+27 21 000 0001",
            Email = "shop@test.co.za",
            CategoryId = cat.CategoryId,
            Status = "pending"
        };

        await _sut.CreateAsync(biz);

        var persisted = await _db.Businesses.FindAsync(biz.BusinessId);
        Assert.NotNull(persisted);
        Assert.Equal("New Shop", persisted.Name);
        Assert.Equal("pending", persisted.Status);
    }

    [Fact]
    [Trait("Category", "Functional")]
    public async Task CreateAsync_WritesAuditLog()
    {
        var cat = new Category { CategoryId = Guid.NewGuid(), Name = "Health" };
        _db.Categories.Add(cat);
        await _db.SaveChangesAsync();
        var ownerId = Guid.NewGuid();
        var biz = new Business
        {
            BusinessId = Guid.NewGuid(), Name = "Clinic", Description = "Test",
            Address = "Durban, KZN", Phone = "+27 31 000 0002", Email = "clinic@test.co.za",
            CategoryId = cat.CategoryId, Status = "pending", OwnerId = ownerId
        };

        await _sut.CreateAsync(biz);

        var log = await _db.AuditLogs.FirstOrDefaultAsync(l => l.UserId == ownerId);
        Assert.NotNull(log);
        Assert.Contains("Clinic", log.Action);
    }

    // ── ApproveAsync / RejectAsync ────────────────────────────────────────

    [Fact]
    [Trait("Category", "Functional")]
    public async Task ApproveAsync_SetsStatusApproved()
    {
        var (_, biz) = await SeedApprovedBusinessAsync();
        biz.Status = "pending";
        await _db.SaveChangesAsync();

        var result = await _sut.ApproveAsync(biz.BusinessId, Guid.NewGuid());

        Assert.True(result);
        var updated = await _db.Businesses.FindAsync(biz.BusinessId);
        Assert.Equal("approved", updated!.Status);
    }

    [Fact]
    [Trait("Category", "Functional")]
    public async Task ApproveAsync_ReturnsFalse_WhenNotFound()
    {
        var result = await _sut.ApproveAsync(Guid.NewGuid(), Guid.NewGuid());
        Assert.False(result);
    }

    [Fact]
    [Trait("Category", "Functional")]
    public async Task RejectAsync_SetsStatusRejected()
    {
        var (_, biz) = await SeedApprovedBusinessAsync();

        var result = await _sut.RejectAsync(biz.BusinessId, Guid.NewGuid());

        Assert.True(result);
        var updated = await _db.Businesses.FindAsync(biz.BusinessId);
        Assert.Equal("rejected", updated!.Status);
    }

    // ── SearchAsync ──────────────────────────────────────────────────────

    [Fact]
    [Trait("Category", "Functional")]
    public async Task SearchAsync_NoFilters_ReturnsOnlyApproved()
    {
        await SeedApprovedBusinessAsync("Approved One");
        var cat2 = new Category { CategoryId = Guid.NewGuid(), Name = "Other" };
        _db.Categories.Add(cat2);
        _db.Businesses.Add(new Business
        {
            BusinessId = Guid.NewGuid(), Name = "Pending Biz", Description = "x",
            Address = "Somewhere", Phone = "0", Email = "x@x.com",
            CategoryId = cat2.CategoryId, Status = "pending"
        });
        await _db.SaveChangesAsync();

        var results = await _sut.SearchAsync(null, null, null);

        Assert.All(results, b => Assert.Equal("approved", b.Status));
        Assert.DoesNotContain(results, b => b.Name == "Pending Biz");
    }

    [Fact]
    [Trait("Category", "Functional")]
    public async Task SearchAsync_QueryFilter_MatchesNameAndDescription()
    {
        await SeedApprovedBusinessAsync("Sunrise Bakery");
        await SeedApprovedBusinessAsync("Tech Corp", "Pretoria, GP");

        var results = await _sut.SearchAsync("Sunrise", null, null);

        Assert.Single(results);
        Assert.Equal("Sunrise Bakery", results[0].Name);
    }

    [Fact]
    [Trait("Category", "Functional")]
    public async Task SearchAsync_CategoryFilter_ReturnsOnlyMatchingCategory()
    {
        var (cat, _) = await SeedApprovedBusinessAsync("Alpha");
        await SeedApprovedBusinessAsync("Beta", "Cape Town, WC");

        var results = await _sut.SearchAsync(null, null, cat.CategoryId);

        Assert.All(results, b => Assert.Equal(cat.CategoryId, b.CategoryId));
    }

    [Fact]
    [Trait("Category", "Functional")]
    public async Task SearchAsync_LocationFilter_MatchesAddress()
    {
        await SeedApprovedBusinessAsync("Cape Biz", "Cape Town, WC");
        await SeedApprovedBusinessAsync("JHB Biz", "Johannesburg, GP");

        var results = await _sut.SearchAsync(null, "Cape Town", null);

        Assert.Single(results);
        Assert.Equal("Cape Biz", results[0].Name);
    }

    // ── GetByIdAsync ──────────────────────────────────────────────────────

    [Fact]
    [Trait("Category", "Functional")]
    public async Task GetByIdAsync_ReturnsCorrectBusiness()
    {
        var (_, biz) = await SeedApprovedBusinessAsync("Find Me");

        var result = await _sut.GetByIdAsync(biz.BusinessId);

        Assert.NotNull(result);
        Assert.Equal("Find Me", result.Name);
    }

    [Fact]
    [Trait("Category", "Functional")]
    public async Task GetByIdAsync_ReturnsNull_WhenNotFound()
    {
        var result = await _sut.GetByIdAsync(Guid.NewGuid());
        Assert.Null(result);
    }

    // ── IsOwnerAsync ─────────────────────────────────────────────────────

    [Fact]
    [Trait("Category", "Functional")]
    public async Task IsOwnerAsync_ReturnsTrue_ForOwner()
    {
        var ownerId = Guid.NewGuid();
        var (_, biz) = await SeedApprovedBusinessAsync("Owned Biz");
        biz.OwnerId = ownerId;
        await _db.SaveChangesAsync();

        var result = await _sut.IsOwnerAsync(biz.BusinessId, ownerId);

        Assert.True(result);
    }

    [Fact]
    [Trait("Category", "Functional")]
    public async Task IsOwnerAsync_ReturnsFalse_ForNonOwner()
    {
        var (_, biz) = await SeedApprovedBusinessAsync();

        var result = await _sut.IsOwnerAsync(biz.BusinessId, Guid.NewGuid());

        Assert.False(result);
    }
}
