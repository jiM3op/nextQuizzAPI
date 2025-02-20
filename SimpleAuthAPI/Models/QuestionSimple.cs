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
        public string? CreatedBy { get; set; }
        public DateTime Created { get; set; }

        // ✅ Ensure this is always initialized
        public List<AnswerSimple> Answers { get; set; } = new();

        // ✅ New Relationship to Category Model
        public List<Category> Categories { get; set; } = new();
    }
}