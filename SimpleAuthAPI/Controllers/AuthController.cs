using System.DirectoryServices.AccountManagement;
using System.Net.Http;
using System.Security.Claims;
using System.Security.Principal;
using System.Text.Json;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Serilog;

[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{

    // Constructor for Depenency Injection of HTTPClientFactory
    private readonly HttpClient _httpClient;
    
    public AuthController(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient("UserManagementAPI");
    }

    [HttpGet("authenticate")]
    [AllowAnonymous]
    public async Task<IActionResult> Authenticate()
    {
        // bla
        var windowsPrincipal = HttpContext.User as WindowsPrincipal;
        var user = windowsPrincipal?.Identity as WindowsIdentity;

        if (user == null || !user.IsAuthenticated)
        {
            Log.Warning("❌ Negotiate Authentication failed: No valid user identity.");
            return Unauthorized();
        }

        Log.Information("✅ User {User} authenticated.", user.Name);

        // Default values if we can't retrieve the actual names
        string firstName = "";
        string lastName = "";
        string displayName = "";
        string email = $"{user.Name}@example.com"; // Default email


        // Check if the user belongs to a specific Windows Group
        bool isInQuizContributors = false;
        string groupName = "SU-CG-K2Dev-Workspace"; // Change this to your actual AD group

        //using (var context = new PrincipalContext(ContextType.Domain))
        using (var context = new PrincipalContext(ContextType.Machine))
        using (var principal = UserPrincipal.FindByIdentity(context, user.Name))
        {
            if (principal != null)
            {
                isInQuizContributors = principal.GetGroups()
                    .Any(g => g.Name.Equals(groupName, StringComparison.OrdinalIgnoreCase));

                // Get user's personal details
                firstName = principal.GivenName ?? "";
                lastName = principal.Surname ?? "";
                displayName = principal.DisplayName ?? $"{firstName} {lastName}".Trim();

                // Try to get email from principal if available
                if (!string.IsNullOrEmpty(principal.EmailAddress))
                {
                    email = principal.EmailAddress;
                }

            }
        }

        var claims = new List<Claim>
    {
        new Claim(ClaimTypes.Name, user.Name),
        new Claim("IsInQuizContributors", isInQuizContributors.ToString()), // Set dynamically
        new Claim(ClaimTypes.GivenName, firstName),
        new Claim(ClaimTypes.Surname, lastName),
        new Claim("DisplayName", displayName),
        new Claim(ClaimTypes.Email, email)
    };

        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);
        var authProperties = new AuthenticationProperties
        {
            IsPersistent = true,
            ExpiresUtc = DateTime.UtcNow.AddDays(7),
        };

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, claimsPrincipal, authProperties);

        var userData = new { Username = user.Name,
            Email = email,
            FirstName = firstName,
            LastName = lastName,
            DisplayName = displayName,
            Role = "User"
        };
        var jsonContent = new StringContent(JsonSerializer.Serialize(userData), Encoding.UTF8, "application/json");

        await _httpClient.PostAsync("http://localhost:8080/api/UserManagement/store-user", jsonContent);


        return Ok(new
        {
            User = user.Name,
            IsInQuizContributors = isInQuizContributors,
            FirstName = firstName,
            LastName = lastName,
            DisplayName = displayName,
            Email = email
        });
    }

    [HttpGet("user")]
    [Authorize]
    public IActionResult GetUser()
    {
        Log.Information(
            "🔍 Checking Cookies in Request: {Cookies}",
            HttpContext.Request.Headers["Cookie"]
        );

        var user = HttpContext.User.Identity;
        if (user == null || !user.IsAuthenticated)
        {
            Log.Warning("❌ Unauthorized access to /api/user.");

            return Unauthorized();
        }

        bool isInGroup =
            HttpContext.User.Claims.FirstOrDefault(c => c.Type == "IsInQuizContributors")?.Value
            == "True";

        Log.Information("👤 User {User} accessed /api/user. Group: {Group}", user.Name, isInGroup);
        return Ok(new { User = user.Name, IsInQuizContributors = isInGroup });
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        Log.Information(
            "🚪 User {User} logged out. Cookies cleared.",
            HttpContext.User.Identity?.Name
        );
        return Ok(new { Message = "Logged out successfully" });
    }
}
