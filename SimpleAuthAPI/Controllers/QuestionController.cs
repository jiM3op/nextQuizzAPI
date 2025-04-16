using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using SimpleAuthAPI.Models;
using SimpleAuthAPI.Data;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Threading.Channels;

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


//    Key changes:

//Updated property names:

//Changed CreatedBy to CreatedById(foreign key)
//Added references to the Creator navigation property


//Included related data:

//Used Include(q => q.Creator) to load the user who created the question
//Added creator's username in response objects


//Enhanced authorization:

//Added checks to ensure only the creator or admin users can update questions
//    Added a new endpoint for retrieving questions created by a specific user


//Added foreign key usage:

//Set the foreign key directly: newQuestion.CreatedById = user.Id;
//Used navigation properties to access related data



//These changes maintain backward compatibility with your frontend by providing the same fields in API responses while leveraging the improved database structure with foreign keys.

    // ✅ GET: Get All Questions (Including Answers & Categories - now ID only)
    [HttpGet]
    [Authorize(Policy = "ContributorOnly")]
    public async Task<IActionResult> GetAllQuestions()
    {
        _logger.LogInformation("📜 Retrieving all questions with answers and categories.");

        var user = HttpContext.User.Identity?.Name;
        _logger.LogInformation("User {User} is accessing questions", user ?? "Unknown");

        // Is this actually authenticated?
        _logger.LogInformation("Is authenticated: {IsAuthenticated}",
            HttpContext.User.Identity?.IsAuthenticated ?? false);

        // What claims does this user have?
        var claims = HttpContext.User.Claims.Select(c => new { c.Type, c.Value }).ToList();
        _logger.LogInformation("Claims: {@Claims}", claims);

        var questions = await _context.Questions
            .Include(q => q.Answers)
            .Include(q => q.Creator) // Include the creator (User)
            .ToListAsync();

        // ✅ Manually map category IDs to actual Category objects
        var categoryDictionary = await _context.Categories.ToDictionaryAsync(c => c.Id);

        var questionsWithCategories = questions.Select(q => new
        {
            q.Id,
            q.QuestionBody,
            q.DifficultyLevel,
            q.QsChecked,
            CreatedById = q.CreatedById,
            // Include the creator's displayable information
            Creator = new
            {
                Id = q.Creator.Id,
                UserName = q.Creator.UserName,
                DisplayName = q.Creator.DisplayName
            },
            q.Created,
            Answers = q.Answers,
            Categories = q.Categories
                .Select(id => categoryDictionary.ContainsKey(id) ? categoryDictionary[id] : null)
                .Where(c => c != null)
                .ToList()
        }).ToList();

        return Ok(questionsWithCategories);
    }

    // ✅ GET: Get Single Question by ID (Including Answers & Categories ID only)
    [HttpGet("{id}")]
    [Authorize]
    public async Task<IActionResult> GetQuestion(int id)
    {
        _logger.LogInformation("📜 Retrieving question with ID {Id} (including answers and categories).", id);

        var question = await _context.Questions
            .Include(q => q.Answers)
            .Include(q => q.Creator) // Include the creator (User)
            .FirstOrDefaultAsync(q => q.Id == id);

        if (question == null)
        {
            _logger.LogWarning("❌ Question with ID {Id} not found.", id);
            return NotFound();
        }

        // ✅ Fetch categories manually
        var categoryDictionary = await _context.Categories.ToDictionaryAsync(c => c.Id);
        var resolvedCategories = question.Categories
            .Select(id => categoryDictionary.ContainsKey(id) ? categoryDictionary[id] : null)
            .Where(c => c != null)
            .ToList();

        var response = new
        {
            question.Id,
            question.QuestionBody,
            question.DifficultyLevel,
            question.QsChecked,
            CreatedById = question.CreatedById, // Use the new property name
            CreatedBy = question.Creator != null ? question.Creator.UserName : "Unknown", // Include creator's name
            question.Created,
            Answers = question.Answers, // ✅ Keep answers
            Categories = resolvedCategories // ✅ Attach resolved categories
        };

        return Ok(response);
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> CreateQuestion([FromBody] QuestionSimple newQuestion)
    {
        if (newQuestion == null) return BadRequest("Invalid question data.");

        _logger.LogInformation("📩 Received Question: {QuestionBody}, Categories: {CategoryCount}",
            newQuestion.QuestionBody, newQuestion.Categories.Count);

        // Get the current user
        var userName = HttpContext.User.Identity?.Name;
        var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == userName);

        if (user == null)
        {
            return BadRequest("User not found or not authenticated");
        }

        // Set the required CreatedById
        newQuestion.CreatedById = user.Id;

        // IMPORTANT: The Creator navigation property should NOT be set here
        // EF Core will handle the relationship based on the foreign key
        // Setting Creator to null to ensure we don't try to insert a duplicate user
        newQuestion.Creator = null;

        newQuestion.Id = 0;
        newQuestion.Created = DateTime.UtcNow;

        // ✅ Ensure each answer is properly linked
        foreach (var answer in newQuestion.Answers)
        {
            answer.Id = 0;
            answer.QuestionSimpleId = newQuestion.Id;
        }

        // ✅ Ensure all categories exist before assigning them to the question
        List<Category> resolvedCategories = new List<Category>();

        foreach (var category in newQuestion.Categories)
        {
            var existingCategory = await _context.Categories.FindAsync(category);
            if (existingCategory != null)
            {
                resolvedCategories.Add(existingCategory);
            }
        }

        newQuestion.Categories = resolvedCategories.Select(c => c.Id).ToList();

        _context.Questions.Add(newQuestion);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetQuestion), new { id = newQuestion.Id }, newQuestion);
    }

    // ✅ PATCH: Update an Existing Question - only use Category IDs 
    [HttpPatch("{id}")]
    [Authorize]
    public async Task<IActionResult> UpdateQuestion(int id, [FromBody] QuestionSimple updatedQuestion)
    {
        _logger.LogInformation("✏️ Updating question ID {Id}.", id);

        var existingQuestion = await _context.Questions
            .Include(q => q.Answers)
            .Include(q => q.Creator) // Include the creator
            .FirstOrDefaultAsync(q => q.Id == id);

        if (existingQuestion == null)
        {
            _logger.LogWarning("❌ Update failed: Question ID {Id} not found.", id);
            return NotFound();
        }

        // Optional: Check if the current user is the creator or has admin rights
        var currentUserName = HttpContext.User.Identity?.Name;
        var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.UserName == currentUserName);

        bool isAdmin = HttpContext.User.Claims
            .Any(c => c.Type == "IsInQuizContributors" && c.Value == "True");

        // Only allow the creator or admins to update the question
        if (existingQuestion.CreatedById != currentUser?.Id && !isAdmin)
        {
            return Forbid("Only the creator or administrators can update this question");
        }

        // ✅ Update question fields
        existingQuestion.QuestionBody = updatedQuestion.QuestionBody;
        existingQuestion.DifficultyLevel = updatedQuestion.DifficultyLevel;
        existingQuestion.QsChecked = updatedQuestion.QsChecked;
        // Don't update CreatedById - keep the original creator

        // ✅ Directly assign new category IDs
        existingQuestion.Categories = updatedQuestion.Categories;

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

    [HttpGet("quiz/{id}")]
    [Authorize] // Any authenticated user can access
    public async Task<IActionResult> GetQuestionForQuiz(int id)
    {
        _logger.LogInformation("🎮 Retrieving quiz question with ID {Id} (with answers but no correctness info)", id);

        var question = await _context.Questions
            .Include(q => q.Answers)
            .Include(q => q.Creator) // Include the creator
            .FirstOrDefaultAsync(q => q.Id == id);

        if (question == null)
        {
            _logger.LogWarning("❌ Question with ID {Id} not found.", id);
            return NotFound();
        }

        // ✅ Fetch categories manually
        var categoryDictionary = await _context.Categories.ToDictionaryAsync(c => c.Id);
        var resolvedCategories = question.Categories
            .Select(id => categoryDictionary.ContainsKey(id) ? categoryDictionary[id] : null)
            .Where(c => c != null)
            .ToList();

        // Convert to quiz question DTO (without correctness info)
        var quizQuestion = QuizQuestionDto.FromQuestionSimple(question);
        quizQuestion.Categories = resolvedCategories;
        //quizQuestion.CreatedBy = question.Creator?.UserName; // Include creator name for display

        return Ok(quizQuestion);
    }

    // Add a new endpoint to get questions created by a specific user
    [HttpGet("created-by/{userId}")]
    [Authorize]
    public async Task<IActionResult> GetQuestionsByUser(int userId)
    {
        // Check if the current user is authorized to view this user's questions
        var currentUserName = HttpContext.User.Identity?.Name;
        var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.UserName == currentUserName);

        bool isAdmin = HttpContext.User.Claims
            .Any(c => c.Type == "IsInQuizContributors" && c.Value == "True");

        // Only allow users to see their own questions or admins to see any user's questions
        if (currentUser?.Id != userId && !isAdmin)
        {
            return Forbid("You can only view your own created questions");
        }

        var questions = await _context.Questions
            .Where(q => q.CreatedById == userId)
            .Include(q => q.Answers)
            .OrderByDescending(q => q.Created)
            .ToListAsync();

        return Ok(questions);
    }

    [HttpPost("bulk-import")]
    [Authorize(Policy = "ContributorOnly")]
    public async Task<IActionResult> BulkImportQuestions([FromBody] List<QuestionSimple> questions)
    {
        if (questions == null || !questions.Any())
        {
            return BadRequest("No questions provided for import.");
        }

        _logger.LogInformation("📥 Bulk importing {Count} questions", questions.Count);

        // Get the current user
        var userName = HttpContext.User.Identity?.Name;
        var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == userName);

        if (user == null)
        {
            return BadRequest("User not found or not authenticated");
        }

        int importedCount = 0;
        List<int> createdQuestionIds = new List<int>();
        List<string> errors = new List<string>();

        foreach (var question in questions)
        {
            try
            {
                // Reset IDs to ensure we're creating new records
                question.Id = 0;

                // Set creation metadata
                question.CreatedById = user.Id;
                question.Creator = null; // Don't import the creator object, use the current user instead
                question.Created = DateTime.UtcNow;

                // Reset answer IDs and ensure they're linked to the question
                foreach (var answer in question.Answers)
                {
                    answer.Id = 0;
                    answer.QuestionSimpleId = 0; // This will be set by EF Core after the question is saved
                }

                // Validate categories exist
                List<Category> resolvedCategories = new List<Category>();
                if (question.Categories != null)
                {
                    foreach (var categoryId in question.Categories)
                    {
                        var existingCategory = await _context.Categories.FindAsync(categoryId);
                        if (existingCategory != null)
                        {
                            resolvedCategories.Add(existingCategory);
                        }
                        else
                        {
                            _logger.LogWarning("Category with ID {CategoryId} not found, skipping", categoryId);
                        }
                    }
                    question.Categories = resolvedCategories.Select(c => c.Id).ToList();
                }

                // Add question to context
                _context.Questions.Add(question);
                await _context.SaveChangesAsync();

                importedCount++;
                createdQuestionIds.Add(question.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing question: {QuestionBody}", question.QuestionBody);
                errors.Add($"Error importing question: {question.QuestionBody?.Substring(0, Math.Min(50, question.QuestionBody?.Length ?? 0))}... - {ex.Message}");
            }
        }

        return Ok(new
        {
            Success = true,
            ImportedCount = importedCount,
            CreatedQuestionIds = createdQuestionIds,
            Errors = errors
        });
    }

    // Add this endpoint to the QuestionController class

    [HttpGet("export")]
    [Authorize(Policy = "ContributorOnly")]
    public async Task<IActionResult> ExportQuestions()
    {
        _logger.LogInformation("📤 Exporting all questions as JSON");

        try
        {
            var questions = await _context.Questions
                .Include(q => q.Answers)
                .Include(q => q.Creator) // Include the creator
                .ToListAsync();

            // Resolve categories for each question
            var categoryDictionary = await _context.Categories.ToDictionaryAsync(c => c.Id);

            foreach (var question in questions)
            {
                // Ensure categories are correctly populated
                question.Categories = question.Categories
                    .Where(id => categoryDictionary.ContainsKey(id))
                    .ToList();
            }

            // Set content type and suggested filename
            Response.Headers.Add("Content-Disposition", "attachment; filename=\"quiz-questions-export.json\"");

            return Ok(questions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting questions");
            return StatusCode(500, "An error occurred while exporting questions");
        }
    }
}