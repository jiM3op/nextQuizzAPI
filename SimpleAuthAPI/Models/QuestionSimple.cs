using System.Text.Json.Serialization;

namespace SimpleAuthAPI.Models
{
    public class QuestionSimple
    {
        public int Id { get; set; }
        public required string QuestionBody { get; set; }
        public required string Category { get; set; }
        public int DifficultyLevel { get; set; }
        public bool QsChecked { get; set; }
        public List<AnswerSimple> Answers { get; set; } = new();
        public string? CreatedBy { get; set; }
        public DateTime Created { get; set; }
    }
}
