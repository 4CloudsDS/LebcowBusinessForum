using LebcowBusinessForum.Web.Data;
using LebcowBusinessForum.Web.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace LebcowBusinessForum.Tests;

/// <summary>
/// WebApplicationFactory wired to use an in-memory database so tests
/// run without a live PostgreSQL instance.
/// </summary>
public class LebcowWebFactory : WebApplicationFactory<Program>
{
    // Shared root so all DbContext instances across this factory use the same store.
    private readonly InMemoryDatabaseRoot _dbRoot = new();
    private readonly string _dbName = "LebcowTest_" + Guid.NewGuid();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureServices(services =>
        {
            // Remove the Npgsql DbContext registration.
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
            if (descriptor != null) services.Remove(descriptor);

            var ctxDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(ApplicationDbContext));
            if (ctxDescriptor != null) services.Remove(ctxDescriptor);

            // Add InMemory database with shared root so all scopes see the same data.
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseInMemoryDatabase(_dbName, _dbRoot));
        });
    }

    // Called after the host is built — seed via the real service provider.
    protected override void ConfigureClient(HttpClient client)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.Database.EnsureCreated();
        SeedTestData(db);
    }

    private static void SeedTestData(ApplicationDbContext db)
    {
        // Guard specifically on the test event — SeedData.InitializeAsync already seeds categories
        // in Development, so checking Categories.Any() would skip this entire method.
        if (db.Events.Any(e => e.Title == "Smoke Test Event")) return;

        // Ensure a Technology category exists (SeedData may have added it already).
        var cat = db.Categories.FirstOrDefault(c => c.Name == "Technology");
        if (cat == null)
        {
            cat = new Category { CategoryId = Guid.NewGuid(), Name = "Technology" };
            db.Categories.Add(cat);
            db.SaveChanges();
        }

        db.Events.Add(new BusinessEvent
        {
            EventId = Guid.NewGuid(),
            Title = "Smoke Test Event",
            Description = "Auto-seeded for smoke testing",
            Location = "Sandton, GP",
            Date = DateTime.UtcNow.AddDays(7),
            OrganizerId = null
        });

        db.SaveChanges();
    }
}
