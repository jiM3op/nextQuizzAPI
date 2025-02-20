using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using SimpleAuthAPI.Models;
using SimpleAuthAPI.Data;

[Route("api/[controller]")]
[ApiController]
public class QuestionController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<QuestionController> _logger;

    public QuestionController(ApplicationDbContext context, ILogger<QuestionController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // ✅ GET: Get All Questions (Including Answers & Categories)
    [HttpGet]
    public async Task<IActionResult> GetAllQuestions()
    {
        _logger.LogInformation("📜 Retrieving all questions with answers and categories.");
        var questions = await _context.Questions
            .Include(q => q.Answers)
            .Include(q => q.Categories) // ✅ Ensure categories are included
            .ToListAsync();
        return Ok(questions);
    }

    // ✅ GET: Get Single Question by ID (Including Answers & Categories)
    [HttpGet("{id}")]
    public async Task<IActionResult> GetQuestion(int id)
    {
        _logger.LogInformation("📜 Retrieving question with ID {Id} (including answers and categories).", id);

        var question = await _context.Questions
            .Include(q => q.Answers)
            .Include(q => q.Categories) // ✅ Ensure categories are included
            .FirstOrDefaultAsync(q => q.Id == id);

        if (question == null)
        {
            _logger.LogWarning("❌ Question with ID {Id} not found.", id);
            return NotFound();
        }

        return Ok(question);
    }

    // ✅ POST: Create a New Question (With Answers & Categories)
    [HttpPost]
    public async Task<IActionResult> CreateQuestion([FromBody] QuestionSimple newQuestion)
    {
        if (newQuestion == null)
        {
            return BadRequest("Invalid question data.");
        }

        _logger.LogInformation("📩 Creating new question: {QuestionBody}", newQuestion.QuestionBody);

        newQuestion.Id = 0;
        newQuestion.Created = DateTime.UtcNow;

        // ✅ Ensure answers are correctly linked to this question
        foreach (var answer in newQuestion.Answers)
        {
            answer.Id = 0;  // Ensure new ID
            answer.QuestionSimpleId = newQuestion.Id; // Set question reference
        }

        // ✅ Save question WITH its answers (EF Core should automatically handle them)
        _context.Questions.Add(newQuestion);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetQuestion), new { id = newQuestion.Id }, newQuestion);
    }



    // ✅ PATCH: Update an Existing Question
    [HttpPatch("{id}")]
    public async Task<IActionResult> UpdateQuestion(int id, [FromBody] QuestionSimple updatedQuestion)
    {
        _logger.LogInformation("✏️ Updating question ID {Id}.", id);

        var existingQuestion = await _context.Questions
            .Include(q => q.Answers)
            .Include(q => q.Categories)
            .FirstOrDefaultAsync(q => q.Id == id);

        if (existingQuestion == null)
        {
            _logger.LogWarning("❌ Update failed: Question ID {Id} not found.", id);
            return NotFound();
        }

        // ✅ Update question fields
        existingQuestion.QuestionBody = updatedQuestion.QuestionBody;
        existingQuestion.DifficultyLevel = updatedQuestion.DifficultyLevel;
        existingQuestion.QsChecked = updatedQuestion.QsChecked;
        existingQuestion.CreatedBy = updatedQuestion.CreatedBy;

        // ✅ Handle Categories (Update the list)
        existingQuestion.Categories.Clear();
        foreach (var category in updatedQuestion.Categories)
        {
            var existingCategory = await _context.Categories.FirstOrDefaultAsync(c => c.Id == category.Id);
            if (existingCategory != null)
            {
                existingQuestion.Categories.Add(existingCategory);
            }
            else
            {
                existingQuestion.Categories.Add(category);
            }
        }

        // ✅ Handle Answers (Match existing, update, add missing, remove deleted)
        var incomingAnswers = updatedQuestion.Answers;
        var existingAnswers = existingQuestion.Answers.ToList();

        // 1️⃣ Remove answers that are missing in the update request
        foreach (var existingAnswer in existingAnswers)
        {
            if (!incomingAnswers.Any(a => a.Id == existingAnswer.Id))
            {
                _context.Answers.Remove(existingAnswer);
            }
        }

        // 2️⃣ Update existing answers or add new ones
        foreach (var newAnswer in incomingAnswers)
        {
            var existingAnswer = existingAnswers.FirstOrDefault(a => a.Id == newAnswer.Id);
            if (existingAnswer != null)
            {
                // Update existing answer
                existingAnswer.AnswerBody = newAnswer.AnswerBody;
                existingAnswer.AnswerCorrect = newAnswer.AnswerCorrect;
                existingAnswer.AnswerPosition = newAnswer.AnswerPosition;
            }
            else
            {
                // Add new answer
                newAnswer.Id = 0; // Ensure it's treated as new
                newAnswer.QuestionSimpleId = existingQuestion.Id;
                _context.Answers.Add(newAnswer);
            }
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("✅ Successfully updated Question ID {Id} with answers and categories.", id);
        return Ok(existingQuestion);
    }





}
