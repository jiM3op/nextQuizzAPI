﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SimpleAuthAPI.Data;
using SimpleAuthAPI.Models;

namespace SimpleAuthAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
public class QuizController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<QuizController> _logger;

    public QuizController(ApplicationDbContext context, ILogger<QuizController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // GET: api/Quiz
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Quiz>>> GetQuizzes()
    {
        _logger.LogInformation("Getting all quizzes");
        return await _context.Quizzes
            .Include(q => q.Questions)
                .ThenInclude(qq => qq.Question)
            .AsNoTracking()
            .ToListAsync();
    }

    // GET: api/Quiz/5
    [HttpGet("{id}")]
    public async Task<ActionResult<Quiz>> GetQuiz(int id)
    {
        _logger.LogInformation("Getting quiz with ID: {QuizId}", id);

        var quiz = await _context.Quizzes
            .Include(q => q.Questions)
                .ThenInclude(qq => qq.Question)
            .AsNoTracking()
            .FirstOrDefaultAsync(q => q.Id == id);

        if (quiz == null)
        {
            _logger.LogWarning("Quiz with ID {QuizId} not found", id);
            return NotFound();
        }

        return quiz;
    }

    // GET: api/Quiz/5/withQuestions
    [HttpGet("{id}/withQuestions")]
    public async Task<ActionResult<Quiz>> GetQuizWithQuestions(int id)
    {
        _logger.LogInformation("Getting quiz with questions for ID: {QuizId}", id);

        // Get quiz with complete question and answer data
        var quiz = await _context.Quizzes
            .Include(q => q.Questions)
                .ThenInclude(qq => qq.Question)
                    .ThenInclude(q => q.Answers)
            .AsNoTracking()
            .FirstOrDefaultAsync(q => q.Id == id);

        if (quiz == null)
        {
            _logger.LogWarning("Quiz with ID {QuizId} not found when trying to get with questions", id);
            return NotFound();
        }

        return quiz;
    }

    // POST: api/Quiz
    [HttpPost]
    public async Task<ActionResult<Quiz>> CreateQuiz(QuizDTO quizDto)
    {
        try
        {
            // Log the incoming request payload
            _logger.LogInformation("Creating quiz with payload: {QuizPayload}",
                JsonSerializer.Serialize(quizDto, new JsonSerializerOptions { WriteIndented = true }));

            // Check model state and log validation errors
            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .Where(e => e.Value.Errors.Count > 0)
                    .Select(e => new {
                        Property = e.Key,
                        Errors = e.Value.Errors.Select(er => er.ErrorMessage).ToList()
                    });

                _logger.LogWarning("Quiz creation validation failed: {ValidationErrors}",
                    JsonSerializer.Serialize(errors));

                return BadRequest(ModelState);
            }

            // Create the quiz
            var quiz = new Quiz
            {
                QuizName = quizDto.QuizName,
                CreatedBy = quizDto.CreatedBy ?? "Anonymous",
                CreatedAt = DateTime.UtcNow
            };

            _context.Quizzes.Add(quiz);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Created new quiz with ID: {QuizId}", quiz.Id);

            // Add the questions to the quiz
            if (quizDto.QuestionIds != null && quizDto.QuestionIds.Any())
            {
                _logger.LogInformation("Adding {QuestionCount} questions to quiz ID: {QuizId}",
                    quizDto.QuestionIds.Count, quiz.Id);

                int orderIndex = 0;
                foreach (var questionId in quizDto.QuestionIds)
                {
                    var question = await _context.Questions.FindAsync(questionId);
                    if (question != null)
                    {
                        _context.QuizQuestions.Add(new QuizQuestion
                        {
                            QuizId = quiz.Id,
                            QuestionId = questionId,
                            OrderIndex = orderIndex++
                        });
                    }
                    else
                    {
                        _logger.LogWarning("Question with ID {QuestionId} not found when adding to quiz", questionId);
                    }
                }
                await _context.SaveChangesAsync();
            }

            // Return the created quiz with basic data to avoid circular references
            return CreatedAtAction(
                nameof(GetQuiz),
                new { id = quiz.Id },
                quiz
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating quiz: {ErrorMessage}", ex.Message);
            return StatusCode(500, "An error occurred while creating the quiz");
        }
    }

    // PUT: api/Quiz/5
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateQuiz(int id, QuizDTO quizDto)
    {
        try
        {
            _logger.LogInformation("Updating quiz ID: {QuizId} with payload: {QuizPayload}",
                id, JsonSerializer.Serialize(quizDto));

            if (id != quizDto.Id)
            {
                _logger.LogWarning("ID mismatch when updating quiz. URL ID: {UrlId}, DTO ID: {DtoId}",
                    id, quizDto.Id);
                return BadRequest("ID mismatch");
            }

            var quiz = await _context.Quizzes
                .Include(q => q.Questions)
                .FirstOrDefaultAsync(q => q.Id == id);

            if (quiz == null)
            {
                _logger.LogWarning("Quiz with ID {QuizId} not found when trying to update", id);
                return NotFound();
            }

            // Update basic properties
            quiz.QuizName = quizDto.QuizName;
            quiz.LastModifiedAt = DateTime.UtcNow;

            // Clear existing questions
            _logger.LogInformation("Removing {ExistingQuestionCount} existing questions from quiz ID: {QuizId}",
                quiz.Questions.Count, id);
            _context.QuizQuestions.RemoveRange(quiz.Questions);

            // Add new questions
            if (quizDto.QuestionIds != null && quizDto.QuestionIds.Any())
            {
                _logger.LogInformation("Adding {NewQuestionCount} questions to updated quiz ID: {QuizId}",
                    quizDto.QuestionIds.Count, id);

                int orderIndex = 0;
                foreach (var questionId in quizDto.QuestionIds)
                {
                    var question = await _context.Questions.FindAsync(questionId);
                    if (question != null)
                    {
                        _context.QuizQuestions.Add(new QuizQuestion
                        {
                            QuizId = quiz.Id,
                            QuestionId = questionId,
                            OrderIndex = orderIndex++
                        });
                    }
                    else
                    {
                        _logger.LogWarning("Question with ID {QuestionId} not found when updating quiz", questionId);
                    }
                }
            }

            _context.Entry(quiz).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("Successfully updated quiz ID: {QuizId}", id);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                if (!QuizExists(id))
                {
                    _logger.LogWarning("Concurrency exception: Quiz with ID {QuizId} no longer exists", id);
                    return NotFound();
                }
                else
                {
                    _logger.LogError(ex, "Concurrency exception when updating quiz ID: {QuizId}", id);
                    throw;
                }
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating quiz ID {QuizId}: {ErrorMessage}", id, ex.Message);
            return StatusCode(500, "An error occurred while updating the quiz");
        }
    }

    // POST: api/Quiz/5/questions/10
    [HttpPost("{quizId}/questions/{questionId}")]
    public async Task<IActionResult> AddQuestionToQuiz(int quizId, int questionId)
    {
        try
        {
            _logger.LogInformation("Adding question ID: {QuestionId} to quiz ID: {QuizId}", questionId, quizId);

            var quiz = await _context.Quizzes.FindAsync(quizId);
            if (quiz == null)
            {
                _logger.LogWarning("Quiz with ID {QuizId} not found when adding question", quizId);
                return NotFound("Quiz not found");
            }

            var question = await _context.Questions.FindAsync(questionId);
            if (question == null)
            {
                _logger.LogWarning("Question with ID {QuestionId} not found when adding to quiz", questionId);
                return NotFound("Question not found");
            }

            // Check if the relationship already exists
            var existingRelation = await _context.QuizQuestions
                .FirstOrDefaultAsync(qq => qq.QuizId == quizId && qq.QuestionId == questionId);

            if (existingRelation != null)
            {
                _logger.LogWarning("Question ID: {QuestionId} is already in quiz ID: {QuizId}", questionId, quizId);
                return BadRequest("Question is already in the quiz");
            }

            // Find the highest current OrderIndex
            var maxOrderIndex = await _context.QuizQuestions
                .Where(qq => qq.QuizId == quizId)
                .Select(qq => qq.OrderIndex)
                .DefaultIfEmpty(-1)
                .MaxAsync();

            // Add the new relationship with the next OrderIndex
            _context.QuizQuestions.Add(new QuizQuestion
            {
                QuizId = quizId,
                QuestionId = questionId,
                OrderIndex = maxOrderIndex + 1
            });

            await _context.SaveChangesAsync();
            _logger.LogInformation("Successfully added question ID: {QuestionId} to quiz ID: {QuizId}", questionId, quizId);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding question ID: {QuestionId} to quiz ID: {QuizId}: {ErrorMessage}",
                questionId, quizId, ex.Message);
            return StatusCode(500, "An error occurred while adding the question to the quiz");
        }
    }

    // DELETE: api/Quiz/5/questions/10
    [HttpDelete("{quizId}/questions/{questionId}")]
    public async Task<IActionResult> RemoveQuestionFromQuiz(int quizId, int questionId)
    {
        try
        {
            _logger.LogInformation("Removing question ID: {QuestionId} from quiz ID: {QuizId}", questionId, quizId);

            var quizQuestion = await _context.QuizQuestions
                .FirstOrDefaultAsync(qq => qq.QuizId == quizId && qq.QuestionId == questionId);

            if (quizQuestion == null)
            {
                _logger.LogWarning("Question ID: {QuestionId} is not in quiz ID: {QuizId}", questionId, quizId);
                return NotFound("Question is not in the quiz");
            }

            _context.QuizQuestions.Remove(quizQuestion);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Successfully removed question ID: {QuestionId} from quiz ID: {QuizId}", questionId, quizId);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing question ID: {QuestionId} from quiz ID: {QuizId}: {ErrorMessage}",
                questionId, quizId, ex.Message);
            return StatusCode(500, "An error occurred while removing the question from the quiz");
        }
    }

    // DELETE: api/Quiz/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteQuiz(int id)
    {
        try
        {
            _logger.LogInformation("Deleting quiz ID: {QuizId}", id);

            var quiz = await _context.Quizzes
                .Include(q => q.Questions)
                .FirstOrDefaultAsync(q => q.Id == id);

            if (quiz == null)
            {
                _logger.LogWarning("Quiz with ID {QuizId} not found when trying to delete", id);
                return NotFound();
            }

            // Remove all question associations
            _logger.LogInformation("Removing {QuestionCount} questions from quiz ID: {QuizId} before deletion",
                quiz.Questions.Count, id);
            _context.QuizQuestions.RemoveRange(quiz.Questions);

            // Remove the quiz
            _context.Quizzes.Remove(quiz);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Successfully deleted quiz ID: {QuizId}", id);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting quiz ID {QuizId}: {ErrorMessage}", id, ex.Message);
            return StatusCode(500, "An error occurred while deleting the quiz");
        }
    }

    private bool QuizExists(int id)
    {
        return _context.Quizzes.Any(e => e.Id == id);
    }
}

// DTO for quiz operations
public class QuizDTO
{
    public int Id { get; set; }
    public string QuizName { get; set; }
    public List<int> QuestionIds { get; set; }
    public string CreatedBy { get; set; }
}