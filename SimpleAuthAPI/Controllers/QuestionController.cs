using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Threading.Tasks;
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

    // ✅ GET: Get All Questions (Including Answers)
    [HttpGet]
    public async Task<IActionResult> GetAllQuestions()
    {
        _logger.LogInformation("📜 Retrieving all questions with answers.");
        var questions = await _context.Questions.Include(q => q.Answers).ToListAsync();
        return Ok(questions);
    }

    // ✅ GET: Get Single Question by ID (Including Answers)
    [HttpGet("{id}")]
    public async Task<IActionResult> GetQuestion(int id)
    {
        _logger.LogInformation("📜 Retrieving question with ID {Id} (including answers).", id);

        var question = await _context.Questions
            .Include(q => q.Answers)
            .FirstOrDefaultAsync(q => q.Id == id);

        if (question == null)
        {
            _logger.LogWarning("❌ Question with ID {Id} not found.", id);
            return NotFound();
        }

        return Ok(question);
    }

    // ✅ POST: Create a Question
    [HttpPost]
    public async Task<IActionResult> CreateQuestion([FromBody] QuestionSimple newQuestion)
    {
        if (newQuestion == null)
        {
            return BadRequest("Invalid question data.");
        }

        _logger.LogInformation("📩 Parsed Question: {QuestionBody}, Answers: {AnswerCount}",
            newQuestion.QuestionBody, newQuestion.Answers.Count);

        newQuestion.Id = 0;
        newQuestion.Created = DateTime.UtcNow;

        foreach (var answer in newQuestion.Answers)
        {
            answer.Id = 0;
            answer.QuestionSimpleId = newQuestion.Id;
        }

        _context.Questions.Add(newQuestion);
        await _context.SaveChangesAsync();

        var savedQuestion = await _context.Questions
            .Include(q => q.Answers)
            .FirstOrDefaultAsync(q => q.Id == newQuestion.Id);

        if (savedQuestion == null)
        {
            _logger.LogError("❌ ERROR: Question was NOT saved to the database!");
            return StatusCode(500, "Internal Server Error: Question was not saved.");
        }

        return CreatedAtAction(nameof(GetQuestion), new { id = savedQuestion.Id }, savedQuestion);
    }

    // ✅ PATCH: Update an Existing Question
    [HttpPatch("{id}")]
    public async Task<IActionResult> UpdateQuestion(int id, [FromBody] QuestionSimple updatedQuestion)
    {
        _logger.LogInformation("✏️ Updating question ID {Id}.", id);

        var existingQuestion = await _context.Questions
            .Include(q => q.Answers)
            .FirstOrDefaultAsync(q => q.Id == id);

        if (existingQuestion == null)
        {
            _logger.LogWarning("❌ Update failed: Question ID {Id} not found.", id);
            return NotFound();
        }

        // ✅ Update fields
        existingQuestion.QuestionBody = updatedQuestion.QuestionBody;
        existingQuestion.Category = updatedQuestion.Category;
        existingQuestion.DifficultyLevel = updatedQuestion.DifficultyLevel;
        existingQuestion.QsChecked = updatedQuestion.QsChecked;
        existingQuestion.CreatedBy = updatedQuestion.CreatedBy;

        _context.Questions.Update(existingQuestion);
        await _context.SaveChangesAsync();

        _logger.LogInformation("✅ Successfully updated Question ID {Id}.", id);
        return Ok(existingQuestion);
    }
}
