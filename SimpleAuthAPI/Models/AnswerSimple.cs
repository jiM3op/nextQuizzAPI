using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations; // ✅ Fix for [Key]
using System.ComponentModel.DataAnnotations.Schema; // ✅ Fix for [DatabaseGenerated]



namespace SimpleAuthAPI.Models
{
    public class AnswerSimple
    {
        [Key] // Ensures EF Core treats this as the primary key
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // ✅ Auto-increments the ID
        public int Id { get; set; }

        public required string AnswerBody { get; set; }
        public bool AnswerCorrect { get; set; }
        public required string AnswerPosition { get; set; }

        public int QuestionSimpleId { get; set; }

        [JsonIgnore]
        public QuestionSimple? Question { get; set; }
    }
}
