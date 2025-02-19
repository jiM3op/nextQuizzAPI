using System.DirectoryServices.AccountManagement;
using System.Security.Claims;
using System.Security.Principal;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Serilog;

[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
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

        // Check if the user belongs to a specific Windows Group
        bool isInQuizContributors = false;
        string groupName = "SU-CG-K2Dev-Workspace"; // Change this to your actual AD group

        using (var context = new PrincipalContext(ContextType.Domain))
        using (var principal = UserPrincipal.FindByIdentity(context, user.Name))
        {
            if (principal != null)
            {
                isInQuizContributors = principal.GetGroups()
                    .Any(g => g.Name.Equals(groupName, StringComparison.OrdinalIgnoreCase));
            }
        }

        var claims = new List<Claim>
    {
        new Claim(ClaimTypes.Name, user.Name),
        new Claim("IsInQuizContributers", isInQuizContributors.ToString()), // Set dynamically
    };

        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);
        var authProperties = new AuthenticationProperties
        {
            IsPersistent = true,
            ExpiresUtc = DateTime.UtcNow.AddDays(7),
        };

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, claimsPrincipal, authProperties);

        return Ok(new { User = user.Name, IsInQuizContributers = isInQuizContributors });
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
            HttpContext.User.Claims.FirstOrDefault(c => c.Type == "IsInQuizContributers")?.Value
            == "True";

        Log.Information("👤 User {User} accessed /api/user. Group: {Group}", user.Name, isInGroup);
        return Ok(new { User = user.Name, IsInQuizContributers = isInGroup });
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
    
    // Not usesd anymore!
    //private bool IsUserInGroup(WindowsIdentity? identity, string groupName)
    //{
    //    if (identity == null)
    //        return false;

    //    try
    //    {
    //        var groups = identity
    //            .Groups?.Select(g => g.Translate(typeof(NTAccount)).Value)
    //            .ToList();
    //        if (groups != null)
    //        {
    //            foreach (var g in groups)
    //            {
    //                Log.Information("🔍 Checking Group (SID-based): {GroupName}", g);
    //                if (g.EndsWith(groupName, StringComparison.OrdinalIgnoreCase))
    //                {
    //                    Log.Information(
    //                        "✅ User {User} IS IN GROUP: {GroupName}",
    //                        identity.Name,
    //                        groupName
    //                    );
    //                    return true;
    //                }
    //            }
    //        }
    //    }
    //    catch (Exception ex)
    //    {
    //        Log.Error("⚠ Error checking user groups: {Message}", ex.Message);
    //    }

    //    Log.Warning("❌ User {User} NOT in group {GroupName}", identity?.Name, groupName);
    //    return false;
    //}
}
