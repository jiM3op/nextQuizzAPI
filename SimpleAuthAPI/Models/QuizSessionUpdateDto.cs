namespace SimpleAuthAPI.Models;

public class QuizSessionUpdateDto
{
    public int? CurrentQuestionIndex { get; set; }
    public string Status { get; set; }
    public double? Score { get; set; }
    public int? MaxDuration { get; set; }
}