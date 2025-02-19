using System.ComponentModel.DataAnnotations;

namespace SimpleAuthAPI.Models
{
    public class Category
    {
        [Key]
        [Required]
        public int Id { get; set; } // ✅ Auto-incremented primary key

        [Required]
        public string Value { get; set; } = string.Empty; // ✅ Unique identifier

        [Required]
        public string Label { get; set; } = string.Empty; // ✅ Display name
    }
}