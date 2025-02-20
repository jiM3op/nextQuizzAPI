using System.ComponentModel.DataAnnotations;

namespace SimpleAuthAPI.Models
{
    public class Category
    {
        [Key]
        public int Id { get; set; } // ✅ Auto-incrementing primary key

        [Required]
        public string Value { get; set; } = string.Empty; // ✅ Unique identifier (e.g., "sql", "linux")

        [Required]
        public string Label { get; set; } = string.Empty; // ✅ Display name (e.g., "SQL", "Linux")
    }
}