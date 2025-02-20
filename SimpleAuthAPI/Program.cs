using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Negotiate;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Serilog;
using SimpleAuthAPI.Data;
using SimpleAuthAPI.Models;

var builder = WebApplication.CreateBuilder(args);

// ✅ Configure Serilog for logging
string logDirectory = Path.Combine(AppContext.BaseDirectory, "logs");
if (!Directory.Exists(logDirectory))
    Directory.CreateDirectory(logDirectory);
string logFilePath = Path.Combine(logDirectory, "api_log.txt");

Log.Logger = new LoggerConfiguration()
    .WriteTo.File(logFilePath, rollingInterval: RollingInterval.Day)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

// ✅ Setup Authentication (Prioritizing CookieAuth)
builder
    .Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.None; // 🔹 Change to "Always" in production
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.IsEssential = true;
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.SlidingExpiration = false;
        options.LoginPath = "/api/Auth/authenticate";
        options.AccessDeniedPath = "/api/Auth/forbidden";
        options.Events.OnRedirectToLogin = context =>
        {
            context.Response.StatusCode = 401; // 🔹 Prevent automatic redirection
            return Task.CompletedTask;
        };
    })
    .AddNegotiate(options =>
    {
        options.Events = new NegotiateEvents
        {
            OnAuthenticated = async context =>
            {
                Log.Information("🟢 OnAuthenticated Triggered.");

                if (
                    context?.Principal?.Identity == null
                    || !context.Principal.Identity.IsAuthenticated
                )
                {
                    Log.Warning("❌ User is NOT authenticated in `OnAuthenticated`.");
                    return;
                }

                string username = context.Principal.Identity.Name ?? "Unknown";
                Log.Information("✅ User authenticated via Windows: {User}", username);

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, username),
                    new Claim("IsInQuizContributers", "False"),
                };

                var identity = new ClaimsIdentity(
                    claims,
                    CookieAuthenticationDefaults.AuthenticationScheme
                );
                var principal = new ClaimsPrincipal(identity);

                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTime.UtcNow.AddDays(7),
                };

                // ✅ Issue authentication cookie here
                Log.Information("🚀 Issuing Authentication Cookie for: {User}", username);
                await context.HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    principal,
                    authProperties
                );
                Log.Information("🍪 Authentication Cookie Set Successfully.");
            },
        };
    });

// ✅ Authorization Policies (Required for Authentication)
builder.Services.AddAuthorization();

// ✅ Enable CORS for Next.js frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy(
        "AllowNextJs",
        policy =>
        {
            policy
                .WithOrigins(
                    "http://localhost:3000",
                    "https://primus.dev.local",
                    "http://192.168.2.88:3000"
                )
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        }
    );
});

// ✅ Add Controllers
builder.Services.AddControllers();

// ✅ Add Database Context
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseInMemoryDatabase("QuestionDb")
);

// ✅ Add HTTP Client to update Users at login
builder.Services.AddHttpClient();

// ✅ Add Swagger for API documentation
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();
Log.Information("🚀 API is starting up...");

// ✅ Correct Middleware Order
app.UseCors("AllowNextJs");
app.UseRouting();
app.UseAuthentication(); // ✅ Must come before authorization
app.UseAuthorization();

app.UseSwagger();
app.UseSwaggerUI();
app.MapControllers();

// ✅ Debug Route: List All API Endpoints
app.MapGet(
    "/routes",
    ([FromServices] EndpointDataSource endpointDataSource) =>
    {
        var routes = endpointDataSource
            .Endpoints.OfType<RouteEndpoint>()
            .Select(e => e.RoutePattern.RawText)
            .ToList();

        return Results.Ok(routes);
    }
);


// ✅ Start API
app.Run();
Log.CloseAndFlush();
