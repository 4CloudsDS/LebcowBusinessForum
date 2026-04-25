using System.Net;
using Xunit;

namespace LebcowBusinessForum.Tests.Smoke;

/// <summary>
/// Stage 1 — Smoke Tests (SmokeTester)
/// Validates that core public routes render without server errors.
/// Run: dotnet test --filter Category=Smoke
/// </summary>
public class SmokeTests : IClassFixture<LebcowWebFactory>
{
    private readonly HttpClient _client;

    public SmokeTests(LebcowWebFactory factory)
    {
        _client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    [Theory]
    [Trait("Category", "Smoke")]
    [InlineData("/")]
    [InlineData("/Browse")]
    [InlineData("/Events")]
    [InlineData("/Forum")]
    [InlineData("/Account/Login")]
    [InlineData("/Account/Register")]
    public async Task PublicRoute_Returns_SuccessOrRedirect(string url)
    {
        var response = await _client.GetAsync(url);

        // Accept 200 OK or 302/301 redirect (e.g. middleware redirect to HTTPS, auth, etc.)
        Assert.True(
            response.StatusCode == HttpStatusCode.OK ||
            response.StatusCode == HttpStatusCode.Redirect ||
            response.StatusCode == HttpStatusCode.MovedPermanently ||
            response.StatusCode == HttpStatusCode.Found,
            $"Expected 200/301/302 for {url} but got {(int)response.StatusCode} {response.StatusCode}");
    }

    [Fact]
    [Trait("Category", "Smoke")]
    public async Task HomePage_Contains_HeroTitle()
    {
        var response = await _client.GetAsync("/");
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("LebcowBusiness", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [Trait("Category", "Smoke")]
    public async Task LoginPage_Contains_Form()
    {
        var response = await _client.GetAsync("/Account/Login");
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("method=\"post\"", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("password", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [Trait("Category", "Smoke")]
    public async Task LoginPost_InvalidCredentials_Does_NotReturn_500()
    {
        // Arrange — get antiforgery token first
        var getResponse = await _client.GetAsync("/Account/Login");
        var html = await getResponse.Content.ReadAsStringAsync();
        var token = ExtractAntiForgeryToken(html);

        var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("Input.Email", "nonexistent@test.com"),
            new KeyValuePair<string, string>("Input.Password", "WrongPassword1"),
            new KeyValuePair<string, string>("__RequestVerificationToken", token ?? ""),
        });

        // Act
        var response = await _client.PostAsync("/Account/Login", content);

        // Assert — must not be a server error
        Assert.True(
            (int)response.StatusCode < 500,
            $"Login with invalid credentials produced a 5xx error: {(int)response.StatusCode}");
    }

    [Fact]
    [Trait("Category", "Smoke")]
    public async Task BrowsePage_Returns_SearchForm()
    {
        var response = await _client.GetAsync("/Browse");
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("search", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [Trait("Category", "Smoke")]
    public async Task UnknownRoute_Returns_404_NotServerError()
    {
        var response = await _client.GetAsync("/does-not-exist-xyz");
        Assert.NotEqual(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    [Trait("Category", "Smoke")]
    public async Task ForumPage_PaginationRoute_Returns_Success()
    {
        // G2 — pagination querystring must not crash the page
        var response = await _client.GetAsync("/Forum?pageNumber=1");
        Assert.True(
            response.StatusCode == HttpStatusCode.OK ||
            response.StatusCode == HttpStatusCode.Redirect ||
            response.StatusCode == HttpStatusCode.Found,
            $"Forum?pageNumber=1 returned {(int)response.StatusCode}");
    }

    [Fact]
    [Trait("Category", "Smoke")]
    public async Task BusinessDetailPage_WithKnownId_Returns_SuccessOrNotFound()
    {
        // D7 — business detail page (with rating hero) must not 500
        var response = await _client.GetAsync("/Business/00000000-0000-0000-0000-000000000001");
        Assert.NotEqual(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    private static string? ExtractAntiForgeryToken(string html)
    {
        const string marker = "__RequestVerificationToken\" type=\"hidden\" value=\"";
        var idx = html.IndexOf(marker, StringComparison.Ordinal);
        if (idx < 0) return null;
        idx += marker.Length;
        var end = html.IndexOf('"', idx);
        return end > idx ? html[idx..end] : null;
    }
}
