using System.ComponentModel.DataAnnotations;

namespace SimpleAuthAPI.Models;

public class UserDto
{
    public string Username { get; set; }  // ✅ Ensure this matches the request JSON
    public string Email { get; set; } = "someEmail";
    public string Role { get; set; } = "User";
}
