using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SimpleAuthAPI.Models;

public class Quiz
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string QuizName { get; set; }

    // Navigation property for quiz questions
    public ICollection<QuizQuestion> Questions { get; set; } = new List<QuizQuestion>();

    // Metadata
    public string CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastModifiedAt { get; set; }
}

public class QuizQuestion
{
    [Key]
    public int Id { get; set; }

    // Foreign keys
    public int QuizId { get; set; }
    public int QuestionId { get; set; }

    // Ordering within the quiz
    public int OrderIndex { get; set; }

    // Navigation properties
    [ForeignKey("QuizId")]
    public Quiz Quiz { get; set; }

    [ForeignKey("QuestionId")]
    public QuestionSimple Question { get; set; }
}

