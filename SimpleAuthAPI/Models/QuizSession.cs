namespace SimpleAuthAPI.Models;

public class QuizSession
{
    public int Id { get; set; }
    public int QuizId { get; set; }
    public int UserId { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int? CurrentQuestionIndex { get; set; }
    public string Status { get; set; } // 'in-progress', 'completed', 'abandoned'
    public double? Score { get; set; }
    public int? MaxDuration { get; set; } // Optional parameter for time limit in minutes

    public string? Metadata { get; set; } // Stored as JSON string

    public ICollection<UserAnswer> UserAnswers { get; set; } = new List<UserAnswer>();
}
