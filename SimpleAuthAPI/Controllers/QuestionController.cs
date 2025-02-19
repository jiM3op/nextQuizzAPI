namespace SimpleAuthAPI.Controllers;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using SimpleAuthAPI.Models;

[Route("api/[controller]")]
[ApiController]
public class QuestionController : ControllerBase
{
    private static readonly List<QuestionSimple> _questions = new List<QuestionSimple>
    {
        new QuestionSimple {
            Id = 1,
            QuestionBody = "What is the capital of France?",
            Category = "Geography",
            DifficultyLevel = 1,
            QsChecked = true,
            CreatedBy = "Admin",
            Created = DateTime.UtcNow,
            Answers = new List<AnswerSimple> {
                new AnswerSimple { Id = 1, AnswerBody = "Paris", AnswerCorrect = true, AnswerPosition = "A", QuestionSimpleId = 1 },
                new AnswerSimple { Id = 2, AnswerBody = "Berlin", AnswerCorrect = false, AnswerPosition = "B", QuestionSimpleId = 1 }
            }
        },
        new QuestionSimple {
            Id = 2,
            QuestionBody = "Who wrote '1984'?",
            Category = "Literature",
            DifficultyLevel = 2,
            QsChecked = true,
            CreatedBy = "Admin",
            Created = DateTime.UtcNow,
            Answers = new List<AnswerSimple> {
                new AnswerSimple { Id = 3, AnswerBody = "George Orwell", AnswerCorrect = true, AnswerPosition = "A", QuestionSimpleId = 2 },
                new AnswerSimple { Id = 4, AnswerBody = "Aldous Huxley", AnswerCorrect = false, AnswerPosition = "B", QuestionSimpleId = 2 }
            }
        }
    };

    private readonly ILogger<QuestionController> _logger;

    public QuestionController(ILogger<QuestionController> logger)
    {
        _logger = logger;
    }

    // Get All Questions
    [HttpGet]
    public IActionResult GetAllQuestions()
    {
        _logger.LogInformation("📜 Retrieving all questions.");
        return Ok(_questions);
    }

    // Create Question
    [HttpPost]
    public IActionResult CreateQuestion([FromBody] QuestionSimple newQuestion)
    {
        if (newQuestion == null)
        {
            return BadRequest("Invalid question data.");
        }

        newQuestion.Id = _questions.Count + 1;
        newQuestion.Created = DateTime.UtcNow;
        _questions.Add(newQuestion);

        _logger.LogInformation("🆕 New question created: {QuestionBody}", newQuestion.QuestionBody);
        return CreatedAtAction(nameof(GetAllQuestions), new { id = newQuestion.Id }, newQuestion);
    }
}
