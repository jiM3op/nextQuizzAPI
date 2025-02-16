using System.Text.Json.Serialization;

namespace SimpleAuthAPI.Models
{
    public class AnswerSimple
    {
        public int Id { get; set; }
        public required string AnswerBody { get; set; }
        public bool AnswerCorrect { get; set; }
        public required string AnswerPosition { get; set; }

        public int QuestionSimpleId { get; set; }

        [JsonIgnore] // Prevents circular reference in JSON serialization
        public QuestionSimple? Question { get; set; }
    }
}
