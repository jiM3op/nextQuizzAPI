namespace SimpleAuthAPI.Models
{
    public class User
    {
        public int Id { get; set; }
        public string UserName { get; set; }  // 🚨 Note: Different naming from UserDto

        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = "User"; // Default role
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}