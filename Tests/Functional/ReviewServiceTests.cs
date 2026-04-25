using LebcowBusinessForum.Web.Data;
using LebcowBusinessForum.Web.Models;
using LebcowBusinessForum.Web.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LebcowBusinessForum.Tests.Functional;

/// <summary>
/// Stage 2 — Functional Tests: ReviewService unit tests (TestAnalyst)
/// Run: dotnet test --filter Category=Functional
/// </summary>
public class ReviewServiceTests : IDisposable
{
    private readonly ApplicationDbContext _db;
    private readonly ReviewService _sut;

    public ReviewServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase("ReviewServiceTests_" + Guid.NewGuid())
            .Options;
        _db = new ApplicationDbContext(options);
        _sut = new ReviewService(_db, new AuditService(_db));
    }

    public void Dispose() => _db.Dispose();

    private async Task<(Guid bizId, Guid userId)> SeedBusinessAndUserAsync()
    {
        var cat = new Category { CategoryId = Guid.NewGuid(), Name = "Food" };
        _db.Categories.Add(cat);
        var biz = new Business
        {
            BusinessId = Guid.NewGuid(), Name = "Test Restaurant", Description = "Good food",
            Address = "Sandton, GP", Phone = "+27 11 000 0005", Email = "food@test.co.za",
            CategoryId = cat.CategoryId, Status = "approved"
        };
        _db.Businesses.Add(biz);
        await _db.SaveChangesAsync();
        return (biz.BusinessId, Guid.NewGuid());
    }

    // ── SubmitAsync ──────────────────────────────────────────────────────

    [Fact]
    [Trait("Category", "Functional")]
    public async Task SubmitAsync_PersistsReview_WithValidInputs()
    {
        var (bizId, userId) = await SeedBusinessAndUserAsync();

        var result = await _sut.SubmitAsync(bizId, userId, 4, "Great service!");

        Assert.True(result);
        var review = await _db.Reviews.FirstOrDefaultAsync(r => r.BusinessId == bizId);
        Assert.NotNull(review);
        Assert.Equal(4, review.Rating);
        Assert.Equal("Great service!", review.Comment);
    }

    [Theory]
    [Trait("Category", "Functional")]
    [InlineData(0)]
    [InlineData(6)]
    [InlineData(-1)]
    public async Task SubmitAsync_ReturnsFalse_ForInvalidRating(int rating)
    {
        var (bizId, userId) = await SeedBusinessAndUserAsync();

        var result = await _sut.SubmitAsync(bizId, userId, rating, "Bad rating");

        Assert.False(result);
        Assert.Empty(await _db.Reviews.ToListAsync());
    }

    [Fact]
    [Trait("Category", "Functional")]
    public async Task SubmitAsync_ReturnsFalse_ForDuplicateReview()
    {
        var (bizId, userId) = await SeedBusinessAndUserAsync();
        await _sut.SubmitAsync(bizId, userId, 3, "First review");

        // Second attempt by same user
        var result = await _sut.SubmitAsync(bizId, userId, 5, "Duplicate");

        Assert.False(result);
        Assert.Equal(1, await _db.Reviews.CountAsync(r => r.BusinessId == bizId));
    }

    [Theory]
    [Trait("Category", "Functional")]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(5)]
    public async Task SubmitAsync_AcceptsAllValidRatings(int rating)
    {
        var (bizId, userId) = await SeedBusinessAndUserAsync();

        var result = await _sut.SubmitAsync(bizId, userId, rating, "Valid rating test");

        Assert.True(result);
    }

    [Fact]
    [Trait("Category", "Functional")]
    public async Task SubmitAsync_TrimsComment()
    {
        var (bizId, userId) = await SeedBusinessAndUserAsync();

        await _sut.SubmitAsync(bizId, userId, 4, "  Padded comment  ");

        var review = await _db.Reviews.FirstAsync(r => r.BusinessId == bizId);
        Assert.Equal("Padded comment", review.Comment);
    }

    // ── HasReviewedAsync ─────────────────────────────────────────────────

    [Fact]
    [Trait("Category", "Functional")]
    public async Task HasReviewedAsync_ReturnsFalse_BeforeSubmit()
    {
        var (bizId, userId) = await SeedBusinessAndUserAsync();

        var result = await _sut.HasReviewedAsync(bizId, userId);

        Assert.False(result);
    }

    [Fact]
    [Trait("Category", "Functional")]
    public async Task HasReviewedAsync_ReturnsTrue_AfterSubmit()
    {
        var (bizId, userId) = await SeedBusinessAndUserAsync();
        await _sut.SubmitAsync(bizId, userId, 5, "Excellent");

        var result = await _sut.HasReviewedAsync(bizId, userId);

        Assert.True(result);
    }

    // ── GetForBusinessAsync ──────────────────────────────────────────────

    [Fact]
    [Trait("Category", "Functional")]
    public async Task GetForBusinessAsync_ReturnsReviews_OrderedByNewest()
    {
        var (bizId, _) = await SeedBusinessAndUserAsync();
        var user1Id = Guid.NewGuid();
        var user2Id = Guid.NewGuid();
        var user3Id = Guid.NewGuid();

        // GetForBusinessAsync uses .Include(r => r.User); the InMemory provider uses inner-join
        // semantics for required FKs so the ApplicationUser rows must exist.
        _db.Users.AddRange(
            new ApplicationUser { Id = user1Id, UserName = "u1@test.com", NormalizedUserName = "U1@TEST.COM", Email = "u1@test.com", NormalizedEmail = "U1@TEST.COM", FullName = "User One", CreatedAt = DateTime.UtcNow, IsActive = true, SecurityStamp = Guid.NewGuid().ToString() },
            new ApplicationUser { Id = user2Id, UserName = "u2@test.com", NormalizedUserName = "U2@TEST.COM", Email = "u2@test.com", NormalizedEmail = "U2@TEST.COM", FullName = "User Two", CreatedAt = DateTime.UtcNow, IsActive = true, SecurityStamp = Guid.NewGuid().ToString() },
            new ApplicationUser { Id = user3Id, UserName = "u3@test.com", NormalizedUserName = "U3@TEST.COM", Email = "u3@test.com", NormalizedEmail = "U3@TEST.COM", FullName = "User Three", CreatedAt = DateTime.UtcNow, IsActive = true, SecurityStamp = Guid.NewGuid().ToString() }
        );
        var now = DateTime.UtcNow;
        _db.Reviews.AddRange(
            new Review { ReviewId = Guid.NewGuid(), BusinessId = bizId, UserId = user1Id, Rating = 3, Comment = "Old", CreatedAt = now.AddDays(-2) },
            new Review { ReviewId = Guid.NewGuid(), BusinessId = bizId, UserId = user2Id, Rating = 4, Comment = "Mid", CreatedAt = now.AddDays(-1) },
            new Review { ReviewId = Guid.NewGuid(), BusinessId = bizId, UserId = user3Id, Rating = 5, Comment = "New", CreatedAt = now }
        );
        await _db.SaveChangesAsync();

        var results = await _sut.GetForBusinessAsync(bizId);

        Assert.Equal(3, results.Count);
        Assert.Equal("New", results[0].Comment);
        Assert.Equal("Old", results[2].Comment);
    }
}
