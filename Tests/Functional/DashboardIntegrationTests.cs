using System.Net;
using System.Security.Claims;
using System.Text.Encodings.Web;
using LebcowBusinessForum.Web.Data;
using LebcowBusinessForum.Web.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace LebcowBusinessForum.Tests.Functional;

public class DashboardIntegrationTests : IClassFixture<TestAuthWebFactory>
{
    private readonly TestAuthWebFactory _factory;
    private readonly HttpClient _client;

    public DashboardIntegrationTests(TestAuthWebFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    [Fact]
    [Trait("Category", "Functional")]
    public async Task Dashboard_BusinessOwner_LoadsAndShowsOwnerAndSavedSections()
    {
        var userId = Guid.NewGuid();
        const string ownerRole = "BusinessOwner";

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await EnsureUserWithRoleAsync(db, userId, ownerRole, "owner.dashboard@test.co.za");

            var category = new Category { CategoryId = Guid.NewGuid(), Name = "OwnerDashCat_" + Guid.NewGuid().ToString("N")[..8] };
            db.Categories.Add(category);

            var ownedBusiness = new Business
            {
                BusinessId = Guid.NewGuid(),
                Name = "Owner Dashboard Business",
                Description = "Owned business for dashboard test",
                Address = "Bloemfontein",
                Phone = "051-000-0001",
                Email = "ownerbiz@test.co.za",
                CategoryId = category.CategoryId,
                OwnerId = userId,
                Status = "approved"
            };

            var savedBusiness = new Business
            {
                BusinessId = Guid.NewGuid(),
                Name = "Saved Dashboard Business",
                Description = "Saved business for dashboard test",
                Address = "Johannesburg",
                Phone = "011-000-0001",
                Email = "savedbiz@test.co.za",
                CategoryId = category.CategoryId,
                Status = "approved"
            };

            db.Businesses.AddRange(ownedBusiness, savedBusiness);
            db.UserFavorites.Add(new UserFavorite { UserId = userId, BusinessId = savedBusiness.BusinessId });
            await db.SaveChangesAsync();
        }

        using var request = new HttpRequestMessage(HttpMethod.Get, "/Account/Dashboard");
        request.Headers.Add("X-Test-UserId", userId.ToString());
        request.Headers.Add("X-Test-Role", ownerRole);

        var response = await _client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Business Owner Dashboard", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Owner Dashboard Business", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Saved Dashboard Business", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [Trait("Category", "Functional")]
    public async Task Dashboard_Admin_LoadsAndShowsAdminOverview()
    {
        var userId = Guid.NewGuid();
        const string adminRole = "Admin";

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await EnsureUserWithRoleAsync(db, userId, adminRole, "admin.dashboard@test.co.za");
        }

        using var request = new HttpRequestMessage(HttpMethod.Get, "/Account/Dashboard");
        request.Headers.Add("X-Test-UserId", userId.ToString());
        request.Headers.Add("X-Test-Role", adminRole);

        var response = await _client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Admin Overview", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Open Admin Dashboard", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [Trait("Category", "Functional")]
    public async Task HomePage_AuthenticatedUser_ShowsWelcomeBackName()
    {
        var userId = Guid.NewGuid();
        const string fullName = "Welcome User";

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await EnsureUserWithRoleAsync(db, userId, "User", "welcome.user@test.co.za");
            var user = await db.Users.FirstAsync(u => u.Id == userId);
            user.FullName = fullName;
            await db.SaveChangesAsync();
        }

        using var request = new HttpRequestMessage(HttpMethod.Get, "/");
        request.Headers.Add("X-Test-UserId", userId.ToString());
        request.Headers.Add("X-Test-Role", "User");

        var response = await _client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Welcome back:", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(fullName, body, StringComparison.OrdinalIgnoreCase);
    }

    private static async Task EnsureUserWithRoleAsync(ApplicationDbContext db, Guid userId, string roleName, string email)
    {
        var normalizedRole = roleName.ToUpperInvariant();
        var role = await db.Roles.FirstOrDefaultAsync(r => r.NormalizedName == normalizedRole);
        if (role is null)
        {
            role = new ApplicationRole
            {
                Id = Guid.NewGuid(),
                Name = roleName,
                NormalizedName = normalizedRole,
                ConcurrencyStamp = Guid.NewGuid().ToString()
            };
            db.Roles.Add(role);
        }

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null)
        {
            user = new ApplicationUser
            {
                Id = userId,
                UserName = email,
                NormalizedUserName = email.ToUpperInvariant(),
                Email = email,
                NormalizedEmail = email.ToUpperInvariant(),
                EmailConfirmed = true,
                FullName = "Dashboard Test User",
                CreatedAt = DateTime.UtcNow,
                IsActive = true,
                SecurityStamp = Guid.NewGuid().ToString()
            };
            db.Users.Add(user);
        }

        if (!await db.UserRoles.AnyAsync(ur => ur.UserId == userId && ur.RoleId == role.Id))
        {
            db.UserRoles.Add(new IdentityUserRole<Guid> { UserId = userId, RoleId = role.Id });
        }

        await db.SaveChangesAsync();
    }
}

public class TestAuthWebFactory : LebcowWebFactory
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        builder.ConfigureServices(services =>
        {
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = TestAuthHandler.Scheme;
                options.DefaultChallengeScheme = TestAuthHandler.Scheme;
            }).AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.Scheme, _ => { });
        });
    }
}

public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string Scheme = "TestAuth";

    public TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ISystemClock clock)
        : base(options, logger, encoder, clock)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("X-Test-UserId", out var userId))
        {
            return Task.FromResult(AuthenticateResult.Fail("Missing X-Test-UserId header."));
        }

        var role = Request.Headers.TryGetValue("X-Test-Role", out var roleHeader)
            ? roleHeader.ToString()
            : "User";

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Name, $"test-{userId}"),
            new(ClaimTypes.Role, role)
        };

        var identity = new ClaimsIdentity(claims, Scheme);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
