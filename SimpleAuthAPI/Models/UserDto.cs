using System.ComponentModel.DataAnnotations;

namespace SimpleAuthAPI.Models;

public class UserDto
{
    // public string Username { get; set; }  // ✅ Ensure this matches the request JSON
    public string UserName { get; set; }  // ✅ modified when using FK Relation
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string DisplayName { get; set; }
    public string Email { get; set; } = "someEmail";
    public string Role { get; set; } = "User";
}
