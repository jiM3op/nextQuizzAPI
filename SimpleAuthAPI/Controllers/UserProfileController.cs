using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SimpleAuthAPI.Data;
using SimpleAuthAPI.Models; // Ensure this namespace is included

namespace SimpleAuthAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
public class UserProfileController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<QuestionController> _logger;

    public UserProfileController(ApplicationDbContext context, ILogger<QuestionController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet("profile")]
    [Authorize]
    public async Task<IActionResult> GetUserProfile()
    {
        var userId = HttpContext.User.Identity.Name;

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.UserName == userId);

        if (user == null)
        {
            return NotFound();
        }

        // Create a new UserProfileDto directly instead of using the static method
        return Ok(new UserProfileDto
        {
            Id = user.Id,
            UserName = user.UserName,
            Email = user.Email ?? string.Empty,
            FirstName = user.FirstName ?? string.Empty,
            LastName = user.LastName ?? string.Empty,
            DisplayName = user.DisplayName ?? string.Empty,
            Role = user.Role ?? "User",
            CreatedAt = user.CreatedAt
        });
    }

    // Get user profile by username
    [HttpGet("profile/{username}")]
    [Authorize]
    public async Task<IActionResult> GetUserProfileByUsername(string username)
    {
        _logger.LogInformation("Fetching user profile for username: {Username}", username);

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.UserName == username);

        if (user == null)
        {
            _logger.LogWarning("User not found with username: {Username}", username);
            return NotFound();
        }

        // Create a new UserProfileDto directly
        return Ok(new UserProfileDto
        {
            Id = user.Id,
            UserName = user.UserName,
            Email = user.Email ?? string.Empty,
            FirstName = user.FirstName ?? string.Empty,
            LastName = user.LastName ?? string.Empty,
            DisplayName = user.DisplayName ?? string.Empty,
            Role = user.Role ?? "User",
            CreatedAt = user.CreatedAt
        });
    }

    // Get user profile by ID
    [HttpGet("{id}")]
    [Authorize]
    public async Task<IActionResult> GetUserProfileById(int id)
    {
        _logger.LogInformation("Fetching user profile for ID: {Id}", id);

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user == null)
        {
            _logger.LogWarning("User not found with ID: {Id}", id);
            return NotFound();
        }

        // Create a new UserProfileDto directly
        return Ok(new UserProfileDto
        {
            Id = user.Id,
            UserName = user.UserName,
            Email = user.Email ?? string.Empty,
            FirstName = user.FirstName ?? string.Empty,
            LastName = user.LastName ?? string.Empty,
            DisplayName = user.DisplayName ?? string.Empty,
            Role = user.Role ?? "User",
            CreatedAt = user.CreatedAt
        });
    }

    // Get summary for a user (less detailed information)
    [HttpGet("summary/{id}")]
    [Authorize]
    public async Task<IActionResult> GetUserSummary(int id)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user == null)
        {
            return NotFound();
        }

        // Create a new UserSummaryDto directly
        return Ok(new UserSummaryDto
        {
            Id = user.Id,
            UserName = user.UserName,
            DisplayName = user.DisplayName ?? user.UserName
        });
    }

    // Get all users (admin only)
    [HttpGet("all")]
    [Authorize(Roles = "Admin")] // Adjust the role as needed
    public async Task<IActionResult> GetAllUsers()
    {
        var users = await _context.Users
            .Select(u => new UserProfileDto
            {
                Id = u.Id,
                UserName = u.UserName,
                Email = u.Email ?? string.Empty,
                FirstName = u.FirstName ?? string.Empty,
                LastName = u.LastName ?? string.Empty,
                DisplayName = u.DisplayName ?? string.Empty,
                Role = u.Role ?? "User",
                CreatedAt = u.CreatedAt
            })
            .ToListAsync();

        return Ok(users);
    }

    // Get a user's contribution statistics
    [HttpGet("{username}/contributions")]
    [Authorize]
    public async Task<IActionResult> GetUserContributions(string username)
    {
        var user = await _context.Users
            .Include(u => u.CreatedQuestions)
            .Include(u => u.CreatedQuizzes)
            .FirstOrDefaultAsync(u => u.UserName == username);

        if (user == null)
        {
            return NotFound();
        }

        // Get the 5 most recent questions
        var recentQuestions = user.CreatedQuestions
            .OrderByDescending(q => q.Created)
            .Take(5)
            .Select(q => new
            {
                q.Id,
                q.QuestionBody,
                Created = q.Created.ToString("yyyy-MM-dd")
            })
            .ToList();

        // Create a new UserSummaryDto directly
        var userSummary = new UserSummaryDto
        {
            Id = user.Id,
            UserName = user.UserName,
            DisplayName = user.DisplayName ?? user.UserName
        };

        return Ok(new
        {
            UserSummary = userSummary,
            QuizzesCreated = user.CreatedQuizzes.Count,
            QuestionsCreated = user.CreatedQuestions.Count,
            RecentQuestions = recentQuestions
        });
    }
}