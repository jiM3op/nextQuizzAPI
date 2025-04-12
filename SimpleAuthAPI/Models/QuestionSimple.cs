using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace SimpleAuthAPI.Models
{
    public class QuestionSimple
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string QuestionBody { get; set; } = string.Empty;

        [Required]
        public int DifficultyLevel { get; set; }
        public bool QsChecked { get; set; }

        // Foreign key to User table
        public int CreatedById { get; set; }

        // Navigation property - MAKE NULLABLE
        [ForeignKey("CreatedById")]
        [JsonIgnore] // This prevents the property from being required in JSON
        public User? Creator { get; set; }

        public DateTime Created { get; set; }

        // ✅ Ensure this is always initialized
        public List<AnswerSimple> Answers { get; set; } = new();

        // ✅ Relationship to Category Model
        [Required]
        public List<int> Categories { get; set; }  // ✅ Now stores just category IDs
    }
}