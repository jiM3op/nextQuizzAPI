using System.ComponentModel.DataAnnotations;

namespace SimpleAuthAPI.Models
{
    public class User
    {

        [Key]
        public int Id { get; set; }

        [Required]
        public string UserName { get; set; } // 🚨 Note: Different naming from UserDto

        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string DisplayName { get; set; }
        public string Role { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual ICollection<QuestionSimple> CreatedQuestions { get; set; } = new List<QuestionSimple>();
        public virtual ICollection<Quiz> CreatedQuizzes { get; set; } = new List<Quiz>();
        public virtual ICollection<QuizSession> QuizSessions { get; set; } = new List<QuizSession>();


        
        
        
    }
}