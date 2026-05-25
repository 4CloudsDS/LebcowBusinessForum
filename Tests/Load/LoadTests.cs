using System.Diagnostics;
using System.Net;
using LebcowBusinessForum.Web.Data;
using LebcowBusinessForum.Web.Models;
using LebcowBusinessForum.Web.Services;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LebcowBusinessForum.Tests.Load;

/// <summary>
/// Stage 3 — Load / Performance Tests (LoadTester persona).
/// Two suites:
///   1. HTTP-level concurrency tests via WebApplicationFactory.
///   2. Service-level throughput tests against an in-memory DB with bulk data.
/// Run: dotnet test --filter Category=Load
/// </summary>

// ─── HTTP concurrency suite ─────────────────────────────────────────────────

public class HttpLoadTests : IClassFixture<LebcowWebFactory>
{
    private readonly LebcowWebFactory _factory;

    public HttpLoadTests(LebcowWebFactory factory)
    {
        _factory = factory;
    }

    // ── Response-time budgets ─────────────────────────────────────────────

    [Fact]
    [Trait("Category", "Load")]
    public async Task HomePage_ResponseTime_Under2000ms()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        // Warm-up request so JIT/startup latency does not skew the measurement.
        await client.GetAsync("/");

        var sw = Stopwatch.StartNew();
        var response = await client.GetAsync("/");
        sw.Stop();

        Assert.True(response.IsSuccessStatusCode, $"GET / returned {(int)response.StatusCode}");
        Assert.True(sw.ElapsedMilliseconds < 2000,
            $"GET / took {sw.ElapsedMilliseconds} ms — budget 2000 ms");
    }

    [Fact]
    [Trait("Category", "Load")]
    public async Task BrowsePage_ResponseTime_Under2000ms()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        await client.GetAsync("/Browse"); // warm-up

        var sw = Stopwatch.StartNew();
        var response = await client.GetAsync("/Browse");
        sw.Stop();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(sw.ElapsedMilliseconds < 2000,
            $"GET /Browse took {sw.ElapsedMilliseconds} ms — budget 2000 ms");
    }

    [Fact]
    [Trait("Category", "Load")]
    public async Task EventsPage_ResponseTime_Under2000ms()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        await client.GetAsync("/Events"); // warm-up

        var sw = Stopwatch.StartNew();
        var response = await client.GetAsync("/Events");
        sw.Stop();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(sw.ElapsedMilliseconds < 2000,
            $"GET /Events took {sw.ElapsedMilliseconds} ms — budget 2000 ms");
    }

    // ── Concurrency ───────────────────────────────────────────────────────

    [Fact]
    [Trait("Category", "Load")]
    public async Task ConcurrentRequests_HomePage_10Users_AllSucceed()
    {
        const int concurrency = 10;
        // Each concurrent "user" gets its own HttpClient (realistic isolation).
        var tasks = Enumerable.Range(0, concurrency)
            .Select(_ =>
            {
                var c = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
                return c.GetAsync("/");
            });

        var responses = await Task.WhenAll(tasks);

        Assert.All(responses, r => Assert.True(r.IsSuccessStatusCode,
            $"Concurrent GET / returned {(int)r.StatusCode}"));
    }

    [Fact]
    [Trait("Category", "Load")]
    public async Task ConcurrentRequests_BrowsePage_10Users_AllSucceed()
    {
        const int concurrency = 10;
        var tasks = Enumerable.Range(0, concurrency)
            .Select(_ =>
            {
                var c = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
                return c.GetAsync("/Browse?q=test");
            });

        var responses = await Task.WhenAll(tasks);

        Assert.All(responses, r => Assert.Equal(HttpStatusCode.OK, r.StatusCode));
    }

    [Fact]
    [Trait("Category", "Load")]
    public async Task ConcurrentRequests_EventsPage_10Users_AllSucceed()
    {
        const int concurrency = 10;
        var tasks = Enumerable.Range(0, concurrency)
            .Select(_ =>
            {
                var c = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
                return c.GetAsync("/Events");
            });

        var responses = await Task.WhenAll(tasks);

        Assert.All(responses, r => Assert.Equal(HttpStatusCode.OK, r.StatusCode));
    }

    [Fact]
    [Trait("Category", "Load")]
    public async Task ForumPage_Paginated_10Users_AllSucceed()
    {
        // G2 — concurrent paginated forum requests must not cause 5xx under load
        const int concurrency = 10;
        var tasks = Enumerable.Range(1, concurrency)
            .Select(i =>
            {
                var c = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
                return c.GetAsync($"/Forum?pageNumber={i}");
            });

        var responses = await Task.WhenAll(tasks);

        Assert.All(responses, r =>
            Assert.True((int)r.StatusCode < 500,
                $"Forum paginated GET returned {(int)r.StatusCode}"));
    }

    [Fact]
    [Trait("Category", "Load")]
    public async Task ForumPage_PrioritizeOfficial_10Users_AllSucceed()
    {
        const int concurrency = 10;
        var tasks = Enumerable.Range(1, concurrency)
            .Select(i =>
            {
                var c = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
                return c.GetAsync($"/Forum?prioritizeOfficial=true&pageNumber={i}");
            });

        var responses = await Task.WhenAll(tasks);

        Assert.All(responses, r =>
            Assert.True((int)r.StatusCode < 500,
                $"Forum prioritizeOfficial GET returned {(int)r.StatusCode}"));
    }
}

// ─── Service-level throughput suite ─────────────────────────────────────────

public class ServiceLoadTests
{
    private static ApplicationDbContext BuildDb(string name)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(name)
            .Options;
        return new ApplicationDbContext(options);
    }

    // ── BusinessService bulk search ───────────────────────────────────────

    [Fact]
    [Trait("Category", "Load")]
    public async Task BusinessService_SearchAsync_100Businesses_UnderBudget()
    {
        using var db = BuildDb("Load_Search_100_" + Guid.NewGuid());
        var svc = new BusinessService(db, new AuditService(db), null!);

        var cat = new Category { CategoryId = Guid.NewGuid(), Name = "Tech" };
        db.Categories.Add(cat);
        db.Businesses.AddRange(Enumerable.Range(0, 100).Select(i => new Business
        {
            BusinessId = Guid.NewGuid(),
            Name = $"Business {i:D3}",
            Description = $"Description for business {i}",
            Address = $"{i} Test Street, Johannesburg",
            Phone = "+27 11 000 0000",
            Email = $"biz{i}@test.co.za",
            CategoryId = cat.CategoryId,
            Status = "approved"
        }));
        await db.SaveChangesAsync();

        var sw = Stopwatch.StartNew();
        var results = await svc.SearchAsync(null, null, null);
        sw.Stop();

        Assert.Equal(100, results.Count);
        Assert.True(sw.ElapsedMilliseconds < 500,
            $"SearchAsync(100 businesses) took {sw.ElapsedMilliseconds} ms — budget 500 ms");
    }

    [Fact]
    [Trait("Category", "Load")]
    public async Task BusinessService_SearchAsync_500Businesses_UnderBudget()
    {
        using var db = BuildDb("Load_Search_500_" + Guid.NewGuid());
        var svc = new BusinessService(db, new AuditService(db), null!);

        var cat = new Category { CategoryId = Guid.NewGuid(), Name = "Tech" };
        db.Categories.Add(cat);
        db.Businesses.AddRange(Enumerable.Range(0, 500).Select(i => new Business
        {
            BusinessId = Guid.NewGuid(),
            Name = $"Business {i:D4}",
            Description = $"Description for business {i}",
            Address = $"{i} Test Road, Cape Town",
            Phone = "+27 21 000 0000",
            Email = $"biz{i}@test500.co.za",
            CategoryId = cat.CategoryId,
            Status = "approved"
        }));
        await db.SaveChangesAsync();

        // SearchAsync has a default take=100, so query should page — verifying it doesn't degrade.
        var sw = Stopwatch.StartNew();
        var results = await svc.SearchAsync(null, null, null, take: 500);
        sw.Stop();

        Assert.Equal(500, results.Count);
        Assert.True(sw.ElapsedMilliseconds < 1000,
            $"SearchAsync(500 businesses) took {sw.ElapsedMilliseconds} ms — budget 1000 ms");
    }

    [Fact]
    [Trait("Category", "Load")]
    public async Task BusinessService_SearchAsync_TextFilter_ReturnsCorrectSubset()
    {
        using var db = BuildDb("Load_Search_Filter_" + Guid.NewGuid());
        var svc = new BusinessService(db, new AuditService(db), null!);

        var cat = new Category { CategoryId = Guid.NewGuid(), Name = "Food" };
        db.Categories.Add(cat);
        db.Businesses.AddRange(Enumerable.Range(0, 50).Select(i => new Business
        {
            BusinessId = Guid.NewGuid(),
            Name = i % 5 == 0 ? $"Pizza Palace {i}" : $"Generic Store {i}",
            Description = "Sample description",
            Address = "Test Rd, Durban",
            Phone = "+27 31 000 0000",
            Email = $"b{i}@food.co.za",
            CategoryId = cat.CategoryId,
            Status = "approved"
        }));
        await db.SaveChangesAsync();

        var sw = Stopwatch.StartNew();
        var results = await svc.SearchAsync("Pizza", null, null, take: 50);
        sw.Stop();

        // 50 businesses, every 5th has "Pizza" in the name → 10 matches.
        Assert.Equal(10, results.Count);
        Assert.True(sw.ElapsedMilliseconds < 500,
            $"Filtered SearchAsync took {sw.ElapsedMilliseconds} ms — budget 500 ms");
    }

    // ── Concurrent service calls ──────────────────────────────────────────

    [Fact]
    [Trait("Category", "Load")]
    public async Task BusinessService_ConcurrentSearchCalls_AllReturnResults()
    {
        const int parallelism = 20;
        // Use a fixed shared name so all parallel DbContext instances read from the same store.
        var dbName = "Load_ConcurrentSearch_" + Guid.NewGuid();

        // Pre-seed via a dedicated context.
        using (var seedDb = BuildDb(dbName))
        {
            var cat = new Category { CategoryId = Guid.NewGuid(), Name = "Services" };
            seedDb.Categories.Add(cat);
            seedDb.Businesses.AddRange(Enumerable.Range(0, 50).Select(i => new Business
            {
                BusinessId = Guid.NewGuid(),
                Name = $"Concurrent Biz {i}",
                Description = "Test",
                Address = "Sandton, GP",
                Phone = "+27 11 000 0001",
                Email = $"con{i}@test.co.za",
                CategoryId = cat.CategoryId,
                Status = "approved"
            }));
            await seedDb.SaveChangesAsync();
        }

        // Each parallel call gets its own DbContext instance on the same InMemory store.
        var tasks = Enumerable.Range(0, parallelism).Select(async _ =>
        {
            using var localDb = BuildDb(dbName);
            var localSvc = new BusinessService(localDb, new AuditService(localDb), null!);
            return await localSvc.SearchAsync(null, null, null, take: 50);
        });

        var allResults = await Task.WhenAll(tasks);

        Assert.All(allResults, r => Assert.Equal(50, r.Count));
    }

    // ── Featured businesses throughput ────────────────────────────────────

    [Fact]
    [Trait("Category", "Load")]
    public async Task BusinessService_GetFeaturedAsync_With200Businesses_UnderBudget()
    {
        using var db = BuildDb("Load_Featured_" + Guid.NewGuid());
        var svc = new BusinessService(db, new AuditService(db), null!);

        var cat = new Category { CategoryId = Guid.NewGuid(), Name = "Mixed" };
        db.Categories.Add(cat);
        db.Businesses.AddRange(Enumerable.Range(0, 200).Select(i => new Business
        {
            BusinessId = Guid.NewGuid(),
            Name = $"Featured Biz {i}",
            Description = "Featured",
            Address = "Pretoria, GP",
            Phone = "+27 12 000 0000",
            Email = $"feat{i}@test.co.za",
            CategoryId = cat.CategoryId,
            Status = "approved"
        }));
        await db.SaveChangesAsync();

        var sw = Stopwatch.StartNew();
        var featured = await svc.GetFeaturedAsync(take: 6);
        sw.Stop();

        Assert.Equal(6, featured.Count);
        Assert.True(sw.ElapsedMilliseconds < 500,
            $"GetFeaturedAsync took {sw.ElapsedMilliseconds} ms — budget 500 ms");
    }
}
