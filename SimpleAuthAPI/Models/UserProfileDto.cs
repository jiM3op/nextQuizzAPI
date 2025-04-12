using System.Text.Json.Serialization;
namespace SimpleAuthAPI.Models;

public class UserProfileDto
{
    public int Id { get; set; }
    public string UserName { get; set; }
    public string Email { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string DisplayName { get; set; }
    public string Role { get; set; }

    // Additional properties can be added as needed
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DateTime? CreatedAt { get; set; }

    // Static method to create from User model
    public static UserProfileDto FromUser(User user)
    {
        return new UserProfileDto
        {
            Id = user.Id,
            UserName = user.UserName,
            Email = user.Email ?? string.Empty,
            FirstName = user.FirstName ?? string.Empty,
            LastName = user.LastName ?? string.Empty,
            DisplayName = user.DisplayName ?? string.Empty,
            Role = user.Role ?? "User",
            CreatedAt = user.CreatedAt
        };
    }
}