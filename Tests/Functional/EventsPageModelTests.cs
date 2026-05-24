using System.Security.Claims;
using LebcowBusinessForum.Web.Data;
using LebcowBusinessForum.Web.Pages;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LebcowBusinessForum.Tests.Functional;

public class EventsPageModelTests
{
    private static ApplicationDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase("EventsModelTests_" + Guid.NewGuid())
            .Options;
        return new ApplicationDbContext(options);
    }

    private static PageContext CreatePageContext(params Claim[] claims)
    {
        var identity = claims.Length > 0
            ? new ClaimsIdentity(claims, "TestAuth")
            : new ClaimsIdentity();

        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(identity)
        };

        return new PageContext { HttpContext = httpContext };
    }

    [Fact]
    public async Task OnPostAsync_Unauthenticated_ReturnsChallenge()
    {
        await using var db = CreateDb();
        var model = new EventsModel(db)
        {
            PageContext = CreatePageContext(),
            Input = new EventsModel.CreateEventInput
            {
                Title = "Legit event title",
                Description = "This is a valid description with enough length.",
                Location = "Johannesburg",
                Date = DateTime.UtcNow.AddDays(3)
            }
        };

        var result = await model.OnPostAsync();

        Assert.IsType<ChallengeResult>(result);
    }

    [Fact]
    public async Task OnPostAsync_AuthenticatedNonPrivilegedUser_ReturnsForbid()
    {
        await using var db = CreateDb();
        var model = new EventsModel(db)
        {
            PageContext = CreatePageContext(
                new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.Role, "User")
            ),
            Input = new EventsModel.CreateEventInput
            {
                Title = "Legit event title",
                Description = "This is a valid description with enough length.",
                Location = "Johannesburg",
                Date = DateTime.UtcNow.AddDays(3)
            }
        };

        var result = await model.OnPostAsync();

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task OnPostAsync_BusinessOwnerWithPastDate_ReturnsPageAndNoInsert()
    {
        await using var db = CreateDb();
        var model = new EventsModel(db)
        {
            PageContext = CreatePageContext(
                new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.Role, "BusinessOwner")
            ),
            Input = new EventsModel.CreateEventInput
            {
                Title = "Legit event title",
                Description = "This is a valid description with enough length.",
                Location = "Johannesburg",
                Date = DateTime.UtcNow.AddMinutes(-1)
            }
        };

        var result = await model.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.False(model.ModelState.IsValid);
        Assert.Equal(0, await db.Events.CountAsync());
    }

    [Fact]
    public async Task OnPostAsync_AdminValidInput_NormalizesDateToUtc_CreatesEventAndRedirects()
    {
        await using var db = CreateDb();
        var adminId = Guid.NewGuid();
        var localInput = DateTime.SpecifyKind(DateTime.UtcNow.AddDays(2), DateTimeKind.Unspecified);
        var model = new EventsModel(db)
        {
            PageContext = CreatePageContext(
                new Claim(ClaimTypes.NameIdentifier, adminId.ToString()),
                new Claim(ClaimTypes.Role, "Admin")
            ),
            Input = new EventsModel.CreateEventInput
            {
                Title = "Township Growth Summit",
                Description = "A valid event description that exceeds the minimum character requirement.",
                Location = "Bloemfontein",
                Date = localInput
            }
        };

        var result = await model.OnPostAsync();

        Assert.IsType<RedirectToPageResult>(result);
        var created = await db.Events.SingleAsync();
        Assert.Equal("Township Growth Summit", created.Title);
        Assert.Equal(adminId, created.OrganizerId);
        Assert.Equal(DateTimeKind.Utc, created.Date.Kind);
        Assert.Equal(localInput, created.Date);
    }
}
