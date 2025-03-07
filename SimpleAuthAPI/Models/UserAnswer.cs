namespace SimpleAuthAPI.Models;

public class UserAnswer
{
    public int Id { get; set; }
    public int QuizSessionId { get; set; }
    public int QuestionId { get; set; }
    public List<int> SelectedAnswerIds { get; set; } = new List<int>();
    public bool? IsCorrect { get; set; }
    public DateTime AnsweredAt { get; set; }
    public int? TimeSpent { get; set; }

    
}
