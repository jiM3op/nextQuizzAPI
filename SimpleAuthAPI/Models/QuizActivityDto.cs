namespace SimpleAuthAPI.Models;

public class QuizActivityDto
{
    public int Id { get; set; }
    public int QuizId { get; set; }
    public string QuizName { get; set; }
    public string StartedAt { get; set; }
    public string CompletedAt { get; set; }
    public double Score { get; set; }
    public int QuestionsTotal { get; set; }
    public int QuestionsAnswered { get; set; }
    public string Status { get; set; } 
}
