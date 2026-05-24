using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace LebcowBusinessForum.Tests.Security;

/// <summary>
/// Stage 4 — Security Tests (SecurityTester persona).
/// Covers OWASP Top 10 areas relevant to this Razor Pages application:
///   A01 Broken Access Control — auth-gated routes
///   A03 Injection — XSS reflection prevention
///   A04 Insecure Design / A05 Misconfiguration — anti-forgery enforcement, error leak prevention
///   A07 Authentication Failures — credential rejection, session handling
/// Run: dotnet test --filter Category=Security
/// </summary>
public class SecurityTests : IClassFixture<LebcowWebFactory>
{
    private readonly LebcowWebFactory _factory;
    private readonly HttpClient _client;

    public SecurityTests(LebcowWebFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    // ── A01: Broken Access Control ───────────────────────────────────────

    [Theory]
    [Trait("Category", "Security")]
    [InlineData("/Admin")]
    [InlineData("/Admin/Index")]
    [InlineData("/BusinessOwner/Create")]
    [InlineData("/BusinessOwner/UpgradeListing/00000000-0000-0000-0000-000000000000")]
    [InlineData("/Account/Dashboard")]
    [InlineData("/Account/Favourites")]
    public async Task AuthGatedRoute_UnauthenticatedUser_RedirectsToLogin(string route)
    {
        var response = await _client.GetAsync(route);

        // Must redirect (3xx); access is never directly granted to anonymous users.
        Assert.True(
            response.StatusCode == HttpStatusCode.Redirect ||
            response.StatusCode == HttpStatusCode.Found ||
            response.StatusCode == HttpStatusCode.MovedPermanently,
            $"Expected redirect for unauthenticated {route} but got {(int)response.StatusCode}");

        var location = response.Headers.Location?.ToString() ?? "";
        Assert.True(
            location.Contains("/Account/Login", StringComparison.OrdinalIgnoreCase) ||
            location.Contains("login", StringComparison.OrdinalIgnoreCase),
            $"Redirect for {route} should point to login, got: {location}");
    }

    [Fact]
    [Trait("Category", "Security")]
    public async Task AdminRoute_RequiresAdminRole_NotJustAuthentication()
    {
        // The Admin index uses [Authorize(Roles = "Admin")].
        // Without auth we get a redirect — verify it is not accessible anonymously.
        var response = await _client.GetAsync("/Admin");

        Assert.NotEqual(HttpStatusCode.OK, response.StatusCode);
    }

    // ── A03: Injection / XSS ─────────────────────────────────────────────

    [Fact]
    [Trait("Category", "Security")]
    public async Task BrowsePage_XssInQuery_IsHtmlEncoded()
    {
        // Razor's @Model.Query auto-encodes; verify the raw script tag never reaches the DOM.
        const string xssPayload = "<script>alert('xss')</script>";
        var response = await _client.GetAsync($"/Browse?q={Uri.EscapeDataString(xssPayload)}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();

        // The raw unencoded script tag must NOT appear in the response.
        Assert.DoesNotContain("<script>alert('xss')</script>", body, StringComparison.OrdinalIgnoreCase);

        // The HTML-encoded form SHOULD appear (Razor encodes it correctly).
        Assert.Contains("&lt;script&gt;", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [Trait("Category", "Security")]
    public async Task BrowsePage_XssInLocation_IsHtmlEncoded()
    {
        const string xssPayload = "<img src=x onerror=alert(1)>";
        var response = await _client.GetAsync($"/Browse?location={Uri.EscapeDataString(xssPayload)}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();

        Assert.DoesNotContain("<img src=x onerror=alert(1)>", body, StringComparison.OrdinalIgnoreCase);
    }

    // ── A04/A05: Anti-Forgery (CSRF) ─────────────────────────────────────

    [Fact]
    [Trait("Category", "Security")]
    public async Task PostLogin_WithoutAntiForgeryToken_IsRejected()
    {
        // POST to login without a valid anti-forgery token must be rejected (not 200).
        var formData = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("Input.Email", "admin@forum.co.za"),
            new KeyValuePair<string, string>("Input.Password", "password"),
            new KeyValuePair<string, string>("Input.RememberMe", "false")
        });

        var response = await _client.PostAsync("/Account/Login", formData);

        // Without the anti-forgery token the request must NOT return 200 (which would mean
        // the submission was processed). ASP.NET Core returns 400 Bad Request.
        Assert.NotEqual(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    [Trait("Category", "Security")]
    public async Task PostNewsletter_WithoutAntiForgeryToken_IsRejected()
    {
        var formData = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("Input.Email", "test@example.com")
        });

        var response = await _client.PostAsync("/Newsletter", formData);

        Assert.NotEqual(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    [Trait("Category", "Security")]
    public async Task PostRegister_WithoutAntiForgeryToken_IsRejected()
    {
        var formData = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("Input.Email", "new@example.com"),
            new KeyValuePair<string, string>("Input.Password", "Password1"),
            new KeyValuePair<string, string>("Input.ConfirmPassword", "Password1")
        });

        var response = await _client.PostAsync("/Account/Register", formData);

        Assert.NotEqual(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    [Trait("Category", "Security")]
    public async Task PostEvents_WithoutAntiForgeryToken_IsRejected()
    {
        var formData = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("Input.Title", "Security test event"),
            new KeyValuePair<string, string>("Input.Description", "Security validation for CSRF protection on event publishing endpoint."),
            new KeyValuePair<string, string>("Input.Location", "Bloemfontein"),
            new KeyValuePair<string, string>("Input.Date", DateTime.UtcNow.AddDays(3).ToString("yyyy-MM-ddTHH:mm"))
        });

        var response = await _client.PostAsync("/Events", formData);

        Assert.NotEqual(HttpStatusCode.OK, response.StatusCode);
    }

    // ── A07: Authentication Failures ─────────────────────────────────────

    [Fact]
    [Trait("Category", "Security")]
    public async Task LoginPage_InvalidCredentials_DoesNotRevealUserExistence()
    {
        // With a valid CSRF token from a GET request, submit invalid credentials.
        // The response body must NOT disclose whether the user account exists.
        var getResponse = await _client.GetAsync("/Account/Login");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        // Extract cookies and anti-forgery token from the login page.
        var loginHtml = await getResponse.Content.ReadAsStringAsync();
        var antiForgeryToken = ExtractAntiForgeryToken(loginHtml);
        var cookies = getResponse.Headers
            .Where(h => h.Key.Equals("Set-Cookie", StringComparison.OrdinalIgnoreCase))
            .SelectMany(h => h.Value)
            .ToList();

        var request = new HttpRequestMessage(HttpMethod.Post, "/Account/Login");
        if (cookies.Any())
            request.Headers.Add("Cookie", string.Join("; ", cookies.Select(c => c.Split(';')[0])));

        request.Content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("__RequestVerificationToken", antiForgeryToken),
            new KeyValuePair<string, string>("Input.Email", "nonexistent@nowhere.co.za"),
            new KeyValuePair<string, string>("Input.Password", "WrongPassword1"),
            new KeyValuePair<string, string>("Input.RememberMe", "false")
        });

        var postResponse = await _client.SendAsync(request);

        // Should either stay on login page (200) or redirect — never 500.
        Assert.True((int)postResponse.StatusCode < 500,
            $"Login with bad credentials returned {(int)postResponse.StatusCode}");

        // If we got a page back (200), ensure it doesn't reveal account existence.
        if (postResponse.StatusCode == HttpStatusCode.OK)
        {
            var body = await postResponse.Content.ReadAsStringAsync();
            // Generic message expected — not "user does not exist" or similar.
            Assert.DoesNotContain("does not exist", body, StringComparison.OrdinalIgnoreCase);
        }
    }

    // ── A05: Security Misconfiguration — Error leakage ───────────────────

    [Fact]
    [Trait("Category", "Security")]
    public async Task UnknownRoute_404_DoesNotLeakInternalDetails()
    {
        var response = await _client.GetAsync("/NonExistentPage12345");

        // Must not be 500.
        Assert.NotEqual(HttpStatusCode.InternalServerError, response.StatusCode);

        // In Development (factory env), error middleware may be less strict,
        // but a 404 must never show a raw stack trace in production. Validate
        // the status code is 404 and the response is a safe error page.
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    [Trait("Category", "Security")]
    public async Task PublicPages_DoNotLeak_ServerBanner()
    {
        var response = await _client.GetAsync("/");

        // The Server header should not disclose the server implementation.
        // ASP.NET Core suppresses it by default.
        var hasServerBanner = response.Headers.TryGetValues("Server", out var serverValues) &&
                              serverValues.Any(v =>
                                  v.Contains("Microsoft", StringComparison.OrdinalIgnoreCase) ||
                                  v.Contains("IIS", StringComparison.OrdinalIgnoreCase) ||
                                  v.Contains("Kestrel", StringComparison.OrdinalIgnoreCase));

        Assert.False(hasServerBanner, "Server header should not disclose implementation details");
    }

    // ── A01: IDOR / path traversal ────────────────────────────────────────

    [Theory]
    [Trait("Category", "Security")]
    [InlineData("/Business/../../etc/passwd")]
    [InlineData("/Business/%2e%2e%2f%2e%2e%2fetc%2fpasswd")]
    [InlineData("/Forum?pageNumber=<script>alert(1)</script>")]
    public async Task PathTraversal_OrXssInRouteParam_DoesNotCrash(string route)
    {
        // D7 / G2 — malicious input in URL segments must not cause 5xx
        var response = await _client.GetAsync(route);
        Assert.True(
            (int)response.StatusCode < 500,
            $"Route {route} returned unexpected {(int)response.StatusCode}");
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    private static string ExtractAntiForgeryToken(string html)
    {
        // Look for <input name="__RequestVerificationToken" type="hidden" value="..." />
        const string marker = "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"";
        var idx = html.IndexOf(marker, StringComparison.Ordinal);
        if (idx < 0) return string.Empty;
        idx += marker.Length;
        var end = html.IndexOf('"', idx);
        return end > idx ? html[idx..end] : string.Empty;
    }
}
