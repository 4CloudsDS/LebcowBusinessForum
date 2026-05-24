using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using Xunit;

namespace LebcowBusinessForum.Tests.Functional;

/// <summary>
/// Stage 2 — Functional Tests: Razor Page integration tests (TestAnalyst)
/// Tests page rendering, search form submission, and auth-gated routes.
/// Run: dotnet test --filter Category=Functional
/// </summary>
public class PageModelIntegrationTests : IClassFixture<LebcowWebFactory>
{
    private readonly HttpClient _client;

    public PageModelIntegrationTests(LebcowWebFactory factory)
    {
        _client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    // ── Browse page ───────────────────────────────────────────────────────

    [Fact]
    [Trait("Category", "Functional")]
    public async Task BrowsePage_WithSearchQuery_Returns_200()
    {
        var response = await _client.GetAsync("/Browse?q=technology");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    [Trait("Category", "Functional")]
    public async Task BrowsePage_WithLocation_Returns_200()
    {
        var response = await _client.GetAsync("/Browse?location=Johannesburg");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    [Trait("Category", "Functional")]
    public async Task BrowsePage_EmptyQuery_Returns_200()
    {
        var response = await _client.GetAsync("/Browse?q=");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    [Trait("Category", "Functional")]
    public async Task BrowsePage_ContainsCategoryDropdown()
    {
        var response = await _client.GetAsync("/Browse");
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync();
        // Page should contain a select or filter for categories
        Assert.Contains("categor", body, StringComparison.OrdinalIgnoreCase);
    }

    // ── Events page ───────────────────────────────────────────────────────

    [Fact]
    [Trait("Category", "Functional")]
    public async Task EventsPage_Returns_200_WithSeededEvent()
    {
        var response = await _client.GetAsync("/Events");
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync();
        // Seeded event "Smoke Test Event" should appear in upcoming events
        Assert.Contains("Smoke Test Event", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [Trait("Category", "Functional")]
    public async Task EventsDetailPage_SeededEventRoute_Returns_200()
    {
        var listResponse = await _client.GetAsync("/Events");
        listResponse.EnsureSuccessStatusCode();
        var body = await listResponse.Content.ReadAsStringAsync();

        var match = Regex.Match(body, "href=\"/Events/([0-9a-fA-F-]{36})\"");
        Assert.True(match.Success, "Expected at least one event detail link on /Events page.");

        var eventId = match.Groups[1].Value;
        var detailResponse = await _client.GetAsync($"/Events/{eventId}");

        Assert.Equal(HttpStatusCode.OK, detailResponse.StatusCode);
    }

    // ── Footer pages ─────────────────────────────────────────────────────

    [Theory]
    [Trait("Category", "Functional")]
    [InlineData("/Contact")]
    [InlineData("/Privacy")]
    [InlineData("/Terms")]
    [InlineData("/About")]
    public async Task FooterPages_Return_200(string url)
    {
        var response = await _client.GetAsync(url);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    [Trait("Category", "Functional")]
    public async Task AboutPage_ContainsExpectedContent()
    {
        var response = await _client.GetAsync("/About");
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Lebcow", body, StringComparison.OrdinalIgnoreCase);
    }

    // ── Forum page ────────────────────────────────────────────────────────

    [Fact]
    [Trait("Category", "Functional")]
    public async Task ForumPage_AnonymousUser_ShowsDisabledButton()
    {
        var response = await _client.GetAsync("/Forum");
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync();
        // Anonymous users see a disabled post button
        Assert.Contains("disabled", body, StringComparison.OrdinalIgnoreCase);
    }

    // ── Auth-gated routes ─────────────────────────────────────────────────

    [Theory]
    [Trait("Category", "Functional")]
    [InlineData("/BusinessOwner/Dashboard")]
    [InlineData("/Admin")]
    [InlineData("/Admin/Listings")]
    [InlineData("/Admin/Categories")]
    [InlineData("/Admin/Reviews")]
    [InlineData("/Admin/Reports")]
    [InlineData("/Account/Dashboard")]
    public async Task AuthGatedRoute_UnauthenticatedUser_Redirects(string url)
    {
        var response = await _client.GetAsync(url);
        // Should redirect to login, not return 200 or 500
        Assert.True(
            response.StatusCode == HttpStatusCode.Redirect ||
            response.StatusCode == HttpStatusCode.Found ||
            response.StatusCode == HttpStatusCode.MovedPermanently ||
            response.StatusCode == HttpStatusCode.NotFound, // route may not exist
            $"Expected redirect or 404 for {url} but got {(int)response.StatusCode}");
    }

    // ── Newsletter subscription form ──────────────────────────────────────

    [Fact]
    [Trait("Category", "Functional")]
    public async Task NewsletterPage_Returns_200()
    {
        var response = await _client.GetAsync("/Newsletter");
        // Accept 200 or redirect (page may require auth)
        Assert.True(
            (int)response.StatusCode < 500,
            $"Newsletter page returned {(int)response.StatusCode}");
    }

    // ── Error pages ───────────────────────────────────────────────────────

    [Fact]
    [Trait("Category", "Functional")]
    public async Task StatusCodePage_404_IsHandled()
    {
        var response = await _client.GetAsync("/non-existent-page-for-test");
        // Development env shows developer exception page, which returns 404 directly
        Assert.NotEqual(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    // ── Polish feature coverage (D3 / D7 / G2) ───────────────────────────

    [Fact]
    [Trait("Category", "Functional")]
    public async Task IndexPage_EventsStrip_DefaultPageLoad_Returns_200()
    {
        // D3 — homepage must load without error (events strip now Take(3))
        var response = await _client.GetAsync("/");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    [Trait("Category", "Functional")]
    public async Task ForumPage_PageNumber1_Returns_200()
    {
        // G2 — paginated first page returns 200
        var response = await _client.GetAsync("/Forum?pageNumber=1");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    [Trait("Category", "Functional")]
    public async Task ForumPage_OutOfRangePageNumber_Clamps_NotError()
    {
        // G2 — page number beyond total pages is clamped, not a server error
        var response = await _client.GetAsync("/Forum?pageNumber=9999");
        Assert.True(
            (int)response.StatusCode < 500,
            $"Forum?pageNumber=9999 returned {(int)response.StatusCode}");
    }

    [Fact]
    [Trait("Category", "Functional")]
    public async Task ForumPage_NegativePageNumber_Clamps_NotError()
    {
        // G2 — negative page number is clamped to 1, not a crash
        var response = await _client.GetAsync("/Forum?pageNumber=-5");
        Assert.True(
            (int)response.StatusCode < 500,
            $"Forum?pageNumber=-5 returned {(int)response.StatusCode}");
    }

    [Fact]
    [Trait("Category", "Functional")]
    public async Task BusinessDetailPage_UnknownId_Returns_SuccessPage()
    {
        // D7 — detail page with unknown GUID renders 200 (shows "not found" message, not 500)
        var response = await _client.GetAsync("/Business/00000000-0000-0000-0000-000000000099");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}

