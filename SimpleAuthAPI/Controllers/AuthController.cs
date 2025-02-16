﻿using System.DirectoryServices.AccountManagement;
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
        var windowsPrincipal = HttpContext.User as WindowsPrincipal;
        var user = windowsPrincipal?.Identity as WindowsIdentity;

        if (user == null || !user.IsAuthenticated)
        {
            Log.Warning("❌ Negotiate Authentication failed: No valid user identity.");

            // Log request headers for debugging
            var headers = string.Join(
                ", ",
                HttpContext.Request.Headers.Select(h => $"[{h.Key}: {h.Value}]")
            );
            Log.Warning("🔍 Request Headers: {Headers}", headers);
            return Unauthorized();
        }

        Log.Information("✅ User {User} authenticated.", user.Name);

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, user.Name),
            new Claim("IsInQuizContributers", "True"),
        };

        var claimsIdentity = new ClaimsIdentity(
            claims,
            CookieAuthenticationDefaults.AuthenticationScheme
        );

        var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);
        var authProperties = new AuthenticationProperties
        {
            IsPersistent = true,
            ExpiresUtc = DateTime.UtcNow.AddDays(7),
        };

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            claimsPrincipal,
            authProperties
        );

        var cookieHeader = HttpContext.Response.Headers["Set-Cookie"];
        Log.Information("🚀 API Response Headers: {Headers}", cookieHeader);

        return Ok(new { User = user.Name, IsInQuizContributers = true });
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

    private bool IsUserInGroup(WindowsIdentity? identity, string groupName)
    {
        if (identity == null)
            return false;

        try
        {
            var groups = identity
                .Groups?.Select(g => g.Translate(typeof(NTAccount)).Value)
                .ToList();
            if (groups != null)
            {
                foreach (var g in groups)
                {
                    Log.Information("🔍 Checking Group (SID-based): {GroupName}", g);
                    if (g.EndsWith(groupName, StringComparison.OrdinalIgnoreCase))
                    {
                        Log.Information(
                            "✅ User {User} IS IN GROUP: {GroupName}",
                            identity.Name,
                            groupName
                        );
                        return true;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error("⚠ Error checking user groups: {Message}", ex.Message);
        }

        Log.Warning("❌ User {User} NOT in group {GroupName}", identity?.Name, groupName);
        return false;
    }
}




//using Microsoft.AspNetCore.Mvc;
//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Authentication;
//using Microsoft.AspNetCore.Authentication.Cookies;
//using System.Security.Claims;
//using System.DirectoryServices.AccountManagement;
//using Serilog;
//using System.Security.Principal;

//[Route("api/[controller]")]
//[ApiController]
//public class AuthController : ControllerBase
//{
//    // ✅ Authenticate & Issue Cookie
//    [HttpGet("authenticate")]
//    [AllowAnonymous]
//    public async Task<IActionResult> Authenticate()
//    {
//        var windowsPrincipal = HttpContext.User as WindowsPrincipal;
//        var user = windowsPrincipal?.Identity as WindowsIdentity;

//        if (user == null || !user.IsAuthenticated)
//        {
//            Log.Warning("Unauthorized authentication attempt.");
//            return Unauthorized();
//        }

//        Log.Information("User {User} successfully authenticated.", user.Name);

//        bool isInGroup = IsUserInGroup(user, "QuizContributers");


//        var claims = new List<Claim>
//        {
//            new Claim(ClaimTypes.Name, user.Name),
//            new Claim("IsInQuizContributers", isInGroup.ToString())
//        };

//        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
//        var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

//        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, claimsPrincipal, new AuthenticationProperties
//        {
//            IsPersistent = true, // Keeps cookie valid across browser sessions
//            ExpiresUtc = DateTime.UtcNow.AddDays(7) // Set expiration to 7 days
//        });

//        Log.Information("User {User} signed in successfully. Group: {Group}", user.Name, isInGroup);
//        return Ok(new { User = user.Name, IsInQuizContributers = isInGroup });
//    }

//    // ✅ Get User Info From Cookie
//    [HttpGet("user")]
//    [Authorize]
//    public IActionResult GetUser()
//    {
//        var windowsPrincipal = HttpContext.User as WindowsPrincipal;
//        var user = windowsPrincipal?.Identity as WindowsIdentity;

//        if (user == null || !user.IsAuthenticated)
//        {
//            Log.Warning("❌ Unauthorized access to /api/user.");
//            return Unauthorized();
//        }

//        bool isInGroup = IsUserInGroup(user, "QuizContributers"); // ✅ Correctly re-checks group membership

//        Log.Information("👤 User {User} accessed /api/user. Group: {Group}", user.Name, isInGroup);
//        return Ok(new { User = user.Name, IsInQuizContributers = isInGroup });
//    }


//    // ✅ Logout Endpoint
//    [HttpPost("logout")]
//    public async Task<IActionResult> Logout()
//    {
//        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
//        Log.Information("User logged out.");
//        return Ok(new { Message = "Logged out successfully" });
//    }

//    private bool IsUserInGroup(WindowsIdentity? identity, string groupName)
//    {
//        if (identity == null) return false;

//        try
//        {
//            string machine = identity.Name.Split('\\')[0]; // Extract domain/machine name

//            using (var context = new PrincipalContext(ContextType.Machine, machine)) // 🔹 Local machine group check
//            using (var user = UserPrincipal.FindByIdentity(context, identity.Name))
//            {
//                if (user != null)
//                {
//                    foreach (var group in user.GetGroups())
//                    {
//                        Log.Information("🔍 Checking Group: {GroupName}", group.Name);

//                        if (group.Name.Equals(groupName, StringComparison.OrdinalIgnoreCase))
//                        {
//                            Log.Information("✅ User {User} IS IN GROUP: {GroupName}", identity.Name, groupName);
//                            return true;
//                        }
//                    }
//                }
//            }
//        }
//        catch (Exception ex)
//        {
//            Log.Error("⚠ Error checking user groups: {Message}", ex.Message);
//        }

//        Log.Warning("❌ User {User} NOT in group {GroupName}", identity?.Name, groupName);
//        return false;
//    }


//}
