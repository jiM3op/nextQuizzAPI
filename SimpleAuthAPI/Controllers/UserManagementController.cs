namespace SimpleAuthAPI.Controllers;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using SimpleAuthAPI.Data;
using SimpleAuthAPI.Models;
using System;
using System.Linq;
using System.Threading.Tasks;


[Route("api/[controller]")]
[ApiController]
public class UserManagementController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<UserManagementController> _logger;

    public UserManagementController(ApplicationDbContext context, ILogger<UserManagementController> logger)
    {
        _context = context;
        _logger = logger;
    }



    // ✅ Store the user if not already in the database
    [AllowAnonymous] // ✅ Temporarily allow unauthenticated requests
    [HttpPost("store-user")]
    //[Authorize]
    
    public async Task<IActionResult> StoreUser([FromBody] UserDto userDto)
    {
        if (userDto == null)
        {
            _logger.LogWarning("❌ Received null user data.");
            return BadRequest("Invalid user data.");
        }

        // 🔍 Log the received user data for debugging
        _logger.LogInformation("📥 Received User Data: {@UserDto}", userDto);

        try
        {
            // ✅ Check if the user already exists
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.UserName == userDto.Username);

            if (existingUser != null)
            {
                _logger.LogInformation("✅ User {Username} already exists. Skipping creation.", userDto.Username);
                return Ok(new { Message = "User already exists.", User = existingUser });
            }

            // ✅ Convert UserDto to User entity
            var newUser = new User
            {
                UserName = userDto.Username, // Ensure this matches your DB column
                Email = userDto.Email,
                Role = userDto.Role
            };

            // 🔄 Store the new user in the database
            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            _logger.LogInformation("🎉 User {Username} successfully added!", newUser.UserName);
            return CreatedAtAction(nameof(StoreUser), new { id = newUser.Id }, newUser);
        }
        catch (Exception ex)
        {
            _logger.LogError("❌ Error storing user: {Message}", ex.Message);
            return StatusCode(500, "Internal Server Error: Unable to store user.");
        }
    }


    // ✅ Get a user's details
    [HttpGet("{userName}")]
    [Authorize]
    public async Task<IActionResult> GetUser(string userName)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == userName);
        if (user == null)
        {
            return NotFound();
        }

        return Ok(user);
    }

    // ✅ Get all users
    [HttpGet("all")]
    
    public async Task<IActionResult> GetAllUsers()
    {
        var users = await _context.Users.ToListAsync();
        return Ok(users);
    }

    // ✅ Update a user's role
    [HttpPatch("{userName}/role")]
    [Authorize]
    public async Task<IActionResult> UpdateUserRole(string userName, [FromBody] string newRole)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == userName);
        if (user == null)
        {
            return NotFound();
        }

        user.Role = newRole;
        await _context.SaveChangesAsync();

        return Ok(user);
    }
}
