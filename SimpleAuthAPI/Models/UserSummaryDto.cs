namespace SimpleAuthAPI.Models;

// A simplified version of UserProfileDto used when only basic user info is needed
public class UserSummaryDto
{
    public int Id { get; set; }
    public string UserName { get; set; }
    public string DisplayName { get; set; }

    // Static method to create from User model
    public static UserSummaryDto FromUser(User user)
    {
        return new UserSummaryDto
        {
            Id = user.Id,
            UserName = user.UserName,
            DisplayName = user.DisplayName ?? user.UserName // Fallback to username if display name is null
        };
    }
}