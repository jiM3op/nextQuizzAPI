using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace SimpleAuthAPI.Models;

public class QuizSession
{
    [Key]
    public int Id { get; set; }

    // Foreign key to Quiz
    public int QuizId { get; set; }

    // Navigation property
    [ForeignKey("QuizId")]
    public Quiz Quiz { get; set; }

    // Foreign key to User
    public int UserId { get; set; }

    // Navigation property
    [ForeignKey("UserId")]
    public User User { get; set; }

    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int? CurrentQuestionIndex { get; set; }
    public string Status { get; set; } // 'in-progress', 'completed', 'abandoned'
    public double? Score { get; set; }
    public int? MaxDuration { get; set; } // Optional parameter for time limit in minutes

    public string? Metadata { get; set; } // Stored as JSON string

    public ICollection<UserAnswer> UserAnswers { get; set; } = new List<UserAnswer>();
}
