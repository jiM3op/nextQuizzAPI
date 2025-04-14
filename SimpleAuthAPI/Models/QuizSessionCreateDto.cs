namespace SimpleAuthAPI.Models;

public class QuizSessionCreateDto
{
    public int QuizId { get; set; }
    public int UserId { get; set; }
    public int? MaxDuration { get; set; }
    public string? Metadata { get; set; }
}
