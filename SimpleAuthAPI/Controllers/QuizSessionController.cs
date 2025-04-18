﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using SimpleAuthAPI.Models;
using SimpleAuthAPI.Data;
using Microsoft.AspNetCore.Authorization;

namespace SimpleAuthAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
public class QuizSessionController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<QuizSessionController> _logger;

    public QuizSessionController(ApplicationDbContext context, ILogger<QuizSessionController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // GET: Get All Quiz Sessions (filtered by user ID if provided)
    [HttpGet]
    public async Task<IActionResult> GetQuizSessions([FromQuery] int? userId)
    {
        _logger.LogInformation("📜 Retrieving quiz sessions" + (userId.HasValue ? $" for user {userId}" : ""));

        var query = _context.QuizSessions.AsQueryable();

        if (userId.HasValue)
        {
            query = query.Where(qs => qs.UserId == userId.Value);
        }

        var sessions = await query
            .Include(qs => qs.UserAnswers)
            .OrderByDescending(qs => qs.StartedAt)
            .ToListAsync();

        return Ok(sessions);
    }

    // GET: Get a specific Quiz Session by ID
    [HttpGet("{id}")]
    public async Task<IActionResult> GetQuizSession(int id)
    {
        _logger.LogInformation("📜 Retrieving quiz session with ID {Id}", id);

        var session = await _context.QuizSessions
            .Include(qs => qs.UserAnswers)
            .FirstOrDefaultAsync(qs => qs.Id == id);

        if (session == null)
        {
            _logger.LogWarning("❌ Quiz session with ID {Id} not found", id);
            return NotFound();
        }

        return Ok(session);
    }

    // POST: Create a new Quiz Session
    [HttpPost]
    public async Task<IActionResult> CreateQuizSession([FromBody] QuizSessionCreateDto dto)
    {
        if (dto == null)
        {
            return BadRequest("Invalid quiz session data");
        }

        _logger.LogInformation("📩 Creating new quiz session for quiz ID {QuizId} and user ID {UserId}",
            dto.QuizId, dto.UserId);

        // Validate quiz exists
        var quizExists = await _context.Quizzes.AnyAsync(q => q.Id == dto.QuizId);
        if (!quizExists)
        {
            return BadRequest($"Quiz with ID {dto.QuizId} does not exist");
        }

        // Validate user exists
        var userExists = await _context.Users.AnyAsync(u => u.Id == dto.UserId);
        if (!userExists)
        {
            return BadRequest($"User with ID {dto.UserId} does not exist");
        }

        // Create new session entity
        var newSession = new QuizSession
        {
            QuizId = dto.QuizId,
            UserId = dto.UserId,
            MaxDuration = dto.MaxDuration,
            StartedAt = DateTime.UtcNow,
            Status = "in-progress",
            CurrentQuestionIndex = 0,
            UserAnswers = new List<UserAnswer>()
        };

        // Process metadata
        if (dto.Metadata != null && !dto.Metadata.StartsWith("{"))
        {
            // This means it came as an object from the client and needs serializing
            try
            {
                newSession.Metadata = System.Text.Json.JsonSerializer.Serialize(dto.Metadata);
            }
            catch
            {
                // If serialization fails, just use it as is - it might already be a string
                newSession.Metadata = dto.Metadata;
            }
        }
        else
        {
            newSession.Metadata = dto.Metadata;
        }

        _context.QuizSessions.Add(newSession);
        await _context.SaveChangesAsync();

        // Include navigation properties for the response
        var sessionWithDetails = await _context.QuizSessions
            .Include(qs => qs.Quiz)
            .Include(qs => qs.User)
            .FirstOrDefaultAsync(qs => qs.Id == newSession.Id);

        return CreatedAtAction(nameof(GetQuizSession), new { id = newSession.Id }, sessionWithDetails);
    }

    // PATCH: Update an existing Quiz Session
    [HttpPatch("{id}")]
    public async Task<IActionResult> UpdateQuizSession(int id, [FromBody] QuizSessionUpdateDto dto)
    {
        _logger.LogInformation("✏️ Updating quiz session ID {Id}", id);

        var existingSession = await _context.QuizSessions.FindAsync(id);
        if (existingSession == null)
        {
            _logger.LogWarning("❌ Update failed: Quiz session ID {Id} not found", id);
            return NotFound();
        }

        // Update only the fields that should be updatable
        if (dto.CurrentQuestionIndex.HasValue)
        {
            existingSession.CurrentQuestionIndex = dto.CurrentQuestionIndex;
        }

        if (!string.IsNullOrEmpty(dto.Status))
        {
            existingSession.Status = dto.Status;

            // If session is being completed, set the completion time
            if (dto.Status == "completed" && !existingSession.CompletedAt.HasValue)
            {
                existingSession.CompletedAt = DateTime.UtcNow;
            }
        }

        if (dto.Score.HasValue)
        {
            existingSession.Score = dto.Score;
        }

        if (dto.MaxDuration.HasValue)
        {
            existingSession.MaxDuration = dto.MaxDuration;
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("✅ Successfully updated quiz session ID {Id}", id);
        return Ok(existingSession);
    }

    // POST: Add an answer to a quiz session
    [HttpPost("{sessionId}/answers")]
    public async Task<IActionResult> AddAnswer(int sessionId, [FromBody] UserAnswer newAnswer)
    {
        _logger.LogInformation("📩 Adding answer to quiz session ID {SessionId}", sessionId);

        var session = await _context.QuizSessions.FindAsync(sessionId);
        if (session == null)
        {
            _logger.LogWarning("❌ Quiz session with ID {SessionId} not found", sessionId);
            return NotFound("Quiz session not found");
        }

        if (session.Status != "in-progress")
        {
            return BadRequest("Cannot add answers to a completed or abandoned quiz session");
        }

        // Validate that the question exists and belongs to the quiz
        var questionExists = await _context.Questions.FindAsync(newAnswer.QuestionId);
        if (questionExists == null)
        {
            return BadRequest($"Question with ID {newAnswer.QuestionId} does not exist");
        }

        // Set default values
        newAnswer.Id = 0;
        newAnswer.AnsweredAt = DateTime.UtcNow;

        // Check if answer is correct
        var correctAnswerIds = await _context.Answers
            .Where(a => a.QuestionSimpleId == newAnswer.QuestionId && a.AnswerCorrect)
            .Select(a => a.Id)
            .ToListAsync();

        // Compare selected answer IDs with correct answer IDs
        bool isCorrect = true;

        // Check if all selected answers are correct
        foreach (var answerId in newAnswer.SelectedAnswerIds)
        {
            if (!correctAnswerIds.Contains(answerId))
            {
                isCorrect = false;
                break;
            }
        }

        // Check if all correct answers were selected
        if (correctAnswerIds.Count != newAnswer.SelectedAnswerIds.Count)
        {
            isCorrect = false;
        }

        newAnswer.IsCorrect = isCorrect;

        // Add the answer to the session
        _context.UserAnswers.Add(newAnswer);
        await _context.SaveChangesAsync();

        return Ok(newAnswer);
    }

    // GET: Get quiz session results
    [HttpGet("{id}/results")]
    public async Task<IActionResult> GetQuizSessionResults(int id)
    {
        _logger.LogInformation("📊 Retrieving results for quiz session ID {Id}", id);

        var session = await _context.QuizSessions
            .Include(qs => qs.UserAnswers)
            .FirstOrDefaultAsync(qs => qs.Id == id);

        if (session == null)
        {
            _logger.LogWarning("❌ Quiz session with ID {Id} not found", id);
            return NotFound();
        }

        // Get the associated quiz with questions
        var quiz = await _context.Quizzes
            .Include(q => q.Questions)
                .ThenInclude(qq => qq.Question)
            .FirstOrDefaultAsync(q => q.Id == session.QuizId);

        if (quiz == null)
        {
            _logger.LogWarning("❌ Quiz with ID {QuizId} not found", session.QuizId);
            return NotFound("Associated quiz not found");
        }

        // Calculate result statistics
        int totalQuestions = quiz.Questions.Count;
        int correctAnswers = session.UserAnswers.Count(ua => ua.IsCorrect == true);
        int incorrectAnswers = session.UserAnswers.Count(ua => ua.IsCorrect == false);
        int unansweredQuestions = totalQuestions - session.UserAnswers.Count;

        double scorePercentage = totalQuestions > 0
            ? Math.Round((double)correctAnswers / totalQuestions * 100, 2)
            : 0;

        // Calculate time taken
        var timeTaken = session.CompletedAt.HasValue
            ? (int)(session.CompletedAt.Value - session.StartedAt).TotalSeconds
            : (int)(DateTime.UtcNow - session.StartedAt).TotalSeconds;

        var result = new
        {
            QuizSession = session,
            Quiz = new
            {
                quiz.Id,
                quiz.QuizName,
                quiz.CreatedById,
                quiz.CreatedAt
            },
            TotalQuestions = totalQuestions,
            CorrectAnswers = correctAnswers,
            IncorrectAnswers = incorrectAnswers,
            UnansweredQuestions = unansweredQuestions,
            ScorePercentage = scorePercentage,
            TimeTaken = timeTaken
        };

        return Ok(result);
    }


    /// <summary>
    /// Get detailed review data for a quiz session, including questions and answers
    /// </summary>
    [HttpGet("{id}/review")]
    public async Task<ActionResult> GetQuizSessionReview(int id)
    {
        _logger.LogInformation("📊 Retrieving review for quiz session ID {Id}", id);

        // Get the quiz session with user answers
        var quizSession = await _context.QuizSessions
            .Include(q => q.UserAnswers)
            .FirstOrDefaultAsync(q => q.Id == id);

        if (quizSession == null)
        {
            return NotFound($"Quiz session with ID {id} not found");
        }

        // Get the quiz
        var quiz = await _context.Quizzes
            .FirstOrDefaultAsync(q => q.Id == quizSession.QuizId);

        if (quiz == null)
        {
            return NotFound($"Quiz with ID {quizSession.QuizId} not found");
        }

        // Get all question IDs from user answers
        var questionIds = quizSession.UserAnswers.Select(ua => ua.QuestionId).ToList();

        // Get the questions with their answers
        var questions = await _context.Questions
            .Where(q => questionIds.Contains(q.Id))
            .Include(q => q.Answers)
            .ToListAsync();

        // Get all categories
        var allCategoryIds = questions.SelectMany(q => q.Categories).Distinct().ToList();
        var categories = await _context.Categories
            .Where(c => allCategoryIds.Contains(c.Id))
            .ToListAsync();

        // Calculate statistics
        int totalQuestions = quizSession.UserAnswers.Count;
        int totalCorrectAnswers = quizSession.UserAnswers.Count(ua => ua.IsCorrect == true);
        int incorrectAnswers = totalQuestions - totalCorrectAnswers;
        int unansweredQuestions = 0; // Assume all questions were answered

        double scorePercentage = totalQuestions > 0
            ? Math.Round((double)totalCorrectAnswers / totalQuestions * 100, 1)
            : 0;

        // Calculate time taken
        TimeSpan timeTaken = quizSession.CompletedAt.HasValue
            ? quizSession.CompletedAt.Value - quizSession.StartedAt
            : TimeSpan.Zero;

        // Build the question reviews
        var questionReviews = new List<object>();

        foreach (var userAnswer in quizSession.UserAnswers)
        {
            var question = questions.FirstOrDefault(q => q.Id == userAnswer.QuestionId);

            if (question == null) continue;

            // Get question categories
            var questionCategoryIds = question.Categories;
            var questionCategories = categories
                .Where(c => questionCategoryIds.Contains(c.Id))
                .Select(c => new { id = c.Id, name = c.Label })
                .ToList();

            // Get correct answer details
            var questionCorrectAnswers = question.Answers
                .Where(a => a.AnswerCorrect)
                .Select(a => new
                {
                    id = a.Id,
                    body = a.AnswerBody,
                    position = a.AnswerPosition,
                    isCorrect = a.AnswerCorrect
                })
                .ToList();

            // Create the question review object
            questionReviews.Add(new
            {
                questionId = question.Id,
                questionBody = question.QuestionBody,
                questionType = "multichoice", // Default value
                categories = questionCategories,
                difficultyLevel = question.DifficultyLevel,
                userAnswer = new
                {
                    id = userAnswer.Id,
                    selectedAnswerIds = userAnswer.SelectedAnswerIds,
                    answeredAt = userAnswer.AnsweredAt,
                    timeSpent = userAnswer.TimeSpent
                },
                possibleAnswers = question.Answers.Select(a => new
                {
                    id = a.Id,
                    body = a.AnswerBody,
                    position = a.AnswerPosition,
                    isCorrect = a.AnswerCorrect
                }).ToList(),
                correctAnswers = questionCorrectAnswers,
                isCorrect = userAnswer.IsCorrect == true
            });
        }

        // Create the review response
        var reviewResponse = new
        {
            // Session information
            sessionId = quizSession.Id,
            quizId = quizSession.QuizId,
            userId = quizSession.UserId,
            startedAt = quizSession.StartedAt,
            completedAt = quizSession.CompletedAt,
            status = quizSession.Status,

            // Quiz information
            quizName = quiz.QuizName,

            // Statistics
            totalQuestions,
            correctAnswers = totalCorrectAnswers,
            incorrectAnswers,
            unansweredQuestions,
            scorePercentage,
            timeTaken = (int)timeTaken.TotalSeconds,

            // Questions with answers
            questionReviews
        };

        return Ok(reviewResponse);
    }

    [HttpGet("user/{userId}")]
    [Authorize]
    public async Task<IActionResult> GetUserQuizSessions(int userId)
    {
        _logger.LogInformation("Fetching quiz sessions for user ID: {UserId}", userId);

        // Get the current username instead of trying to parse it as an integer
        var currentUsername = HttpContext.User.Identity.Name;

        // Fetch the current user to get their ID
        var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.UserName == currentUsername);
        if (currentUser == null)
        {
            _logger.LogWarning("User not found with username: {Username}", currentUsername);
            return NotFound("Current user not found");
        }

        var isAdmin = HttpContext.User.IsInRole("Admin");

        // Check if requesting user has permission (is admin or is the user themselves)
        if (!isAdmin && currentUser.Id != userId)
        {
            _logger.LogWarning("Unauthorized attempt to access user {UserId} quiz sessions by user {CurrentUserId}",
                userId, currentUser.Id);
            return Forbid();
        }

        // Get quiz sessions with related quiz data
        var quizSessions = await _context.QuizSessions
            .Include(qs => qs.Quiz)
            .Where(qs => qs.UserId == userId)
            .OrderByDescending(qs => qs.CompletedAt)
            .Select(qs => new QuizActivityDto
            {
                Id = qs.Id,
                QuizId = qs.QuizId,
                QuizName = qs.Quiz.QuizName,
                StartedAt = qs.StartedAt.ToString("yyyy-MM-ddTHH:mm:ss"),
                CompletedAt = qs.CompletedAt.HasValue ? qs.CompletedAt.Value.ToString("yyyy-MM-ddTHH:mm:ss") : null,
                Score = qs.Score ?? 0,
                QuestionsTotal = qs.Quiz.Questions.Count,
                QuestionsAnswered = qs.UserAnswers.Count
            })
            .ToListAsync();

        return Ok(quizSessions);
    }

    [HttpPost("complete/{sessionId}")]
    [Authorize]
    public async Task<IActionResult> CompleteQuizSession(int sessionId)
    {
        _logger.LogInformation("Completing quiz session with ID: {SessionId}", sessionId);

        try
        {
            // Get the session with user answers
            var session = await _context.QuizSessions
                .Include(qs => qs.Quiz)
                .Include(qs => qs.UserAnswers)
                .FirstOrDefaultAsync(qs => qs.Id == sessionId);

            if (session == null)
            {
                _logger.LogWarning("Quiz session not found with ID: {SessionId}", sessionId);
                return NotFound("Quiz session not found");
            }

            // Skip if already completed
            if (session.CompletedAt.HasValue && session.Status == "completed")
            {
                _logger.LogInformation("Quiz session {SessionId} already marked as completed", sessionId);
                return Ok(new
                {
                    sessionId = session.Id,
                    score = session.Score,
                    status = session.Status
                });
            }

            // Mark the session as completed
            session.CompletedAt = DateTime.UtcNow;
            session.Status = "completed";

            // Calculate the score
            double correctAnswers = 0;
            int totalAnsweredQuestions = session.UserAnswers.Count;

            // Fetch all required question and answer data for score calculation
            foreach (var userAnswer in session.UserAnswers)
            {
                // Fetch the question with its answers
                var question = await _context.Questions
                    .Include(q => q.Answers)
                    .FirstOrDefaultAsync(q => q.Id == userAnswer.QuestionId);

                if (question == null) continue;

                // Get the correct answer IDs for this question
                var correctAnswerIds = question.Answers
                    .Where(a => a.AnswerCorrect)
                    .Select(a => a.Id)
                    .ToList();

                // Check if the user selected all correct answers and only correct answers
                bool isCorrect = userAnswer.SelectedAnswerIds.Count == correctAnswerIds.Count &&
                                  userAnswer.SelectedAnswerIds.All(id => correctAnswerIds.Contains(id));

                if (isCorrect)
                {
                    correctAnswers++;
                    userAnswer.IsCorrect = true;
                }
                else
                {
                    userAnswer.IsCorrect = false;
                }
            }

            // Calculate percentage score (handle division by zero)
            double scorePercentage = totalAnsweredQuestions > 0
                ? (correctAnswers / totalAnsweredQuestions) * 100
                : 0;

            // Round to nearest integer and save
            session.Score = Math.Round(scorePercentage);

            _logger.LogInformation(
                "Quiz session {SessionId} completed. Score: {Score}% ({Correct}/{Total})",
                sessionId, session.Score, correctAnswers, totalAnsweredQuestions);

            await _context.SaveChangesAsync();

            return Ok(new
            {
                sessionId = session.Id,
                score = session.Score,
                correctAnswers,
                totalQuestions = totalAnsweredQuestions
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing quiz session {SessionId}", sessionId);
            return StatusCode(500, "An error occurred while completing the quiz session");
        }
    }



}


