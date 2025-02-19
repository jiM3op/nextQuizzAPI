namespace SimpleAuthAPI.Controllers;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Threading.Tasks;
using Models;
using Data;

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

    // Get All Questions (Including Answers)
    [HttpGet]
    public async Task<IActionResult> GetAllQuestions()
    {
        _logger.LogInformation("📜 Retrieving all questions with answers.");
        var questions = await _context.Questions.Include(q => q.Answers).ToListAsync();
        return Ok(questions);
    }

    // Get Single Question by ID (Including Answers)
    [HttpGet("{id}")]
    public async Task<IActionResult> GetQuestion(int id)
    {
        _logger.LogInformation("📜 Retrieving question with ID {Id} (including answers).", id);

        var question = await _context.Questions
            .Include(q => q.Answers) // ✅ Ensure answers are included
            .FirstOrDefaultAsync(q => q.Id == id);

        if (question == null)
        {
            _logger.LogWarning("❌ Question with ID {Id} not found.", id);
            return NotFound();
        }

        if (question.Answers == null || !question.Answers.Any())
        {
            _logger.LogWarning("⚠️ Question with ID {Id} has NO answers linked.", id);
        }
        else
        {
            _logger.LogInformation("✅ Question with ID {Id} has {Count} answers.", id, question.Answers.Count);
        }

        return Ok(question);
    }
    
    // Create Question
    [HttpPost]
    public async Task<IActionResult> CreateQuestion([FromBody] QuestionSimple newQuestion)
    {
        if (newQuestion == null)
        {
            return BadRequest("Invalid question data.");
        }

        _logger.LogInformation("📩 Parsed Question: {QuestionBody}, Answers: {AnswerCount}",
            newQuestion.QuestionBody, newQuestion.Answers.Count);

        // ✅ Ensure question ID is zero to allow EF Core to auto-generate it
        newQuestion.Id = 0;
        newQuestion.Created = DateTime.UtcNow;

        // ✅ Ensure each answer is properly linked
        foreach (var answer in newQuestion.Answers)
        {
            answer.Id = 0; // ✅ Reset ID (let EF Core handle it)
            answer.QuestionSimpleId = newQuestion.Id; // ✅ Link answer to the question
        }

        // ✅ Save Question (EF Core will automatically save related answers)
        _context.Questions.Add(newQuestion);
        await _context.SaveChangesAsync();
        
        // ✅ Fetch the question again to confirm answers are saved
        var savedQuestion = await _context.Questions
            .Include(q => q.Answers)
            .FirstOrDefaultAsync(q => q.Id == newQuestion.Id);

        if (savedQuestion == null)
        {
            _logger.LogError("❌ ERROR: Question was NOT saved to the database!");
            return StatusCode(500, "Internal Server Error: Question was not saved.");
        }
        else if (savedQuestion.Answers.Count == 0)
        {
            _logger.LogWarning("⚠️ WARNING: Question was saved but has NO linked answers!");
        }
        else
        {
            _logger.LogInformation("✅ Successfully saved Question with {AnswerCount} answers!", savedQuestion.Answers.Count);
        }

        return CreatedAtAction(nameof(GetQuestion), new { id = savedQuestion.Id }, savedQuestion);
    }



}