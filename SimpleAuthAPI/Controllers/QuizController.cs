using System;
using System.Collections.Generic;
using System.Linq;
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

    public QuizController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: api/Quiz
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Quiz>>> GetQuizzes()
    {
        return await _context.Quizzes
            .Include(q => q.Questions)
                .ThenInclude(qq => qq.Question)
                    .ThenInclude(q => q.Answers)
            .AsNoTracking()
            .ToListAsync();
    }

    // GET: api/Quiz/5
    [HttpGet("{id}")]
    public async Task<ActionResult<Quiz>> GetQuiz(int id)
    {
        var quiz = await _context.Quizzes
            .Include(q => q.Questions)
                .ThenInclude(qq => qq.Question)
                    .ThenInclude(q => q.Answers)
            .AsNoTracking()
            .FirstOrDefaultAsync(q => q.Id == id);

        if (quiz == null)
        {
            return NotFound();
        }

        return quiz;
    }

    // POST: api/Quiz
    [HttpPost]
    public async Task<ActionResult<Quiz>> CreateQuiz(QuizDTO quizDto)
    {
        if (!ModelState.IsValid)
        {
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

        // Add the questions to the quiz
        if (quizDto.QuestionIds != null && quizDto.QuestionIds.Any())
        {
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
            }
            await _context.SaveChangesAsync();
        }

        // Return the created quiz with all its data
        return CreatedAtAction(
            nameof(GetQuiz),
            new { id = quiz.Id },
            await GetQuiz(quiz.Id)
        );
    }

    // PUT: api/Quiz/5
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateQuiz(int id, QuizDTO quizDto)
    {
        if (id != quizDto.Id)
        {
            return BadRequest("ID mismatch");
        }

        var quiz = await _context.Quizzes
            .Include(q => q.Questions)
            .FirstOrDefaultAsync(q => q.Id == id);

        if (quiz == null)
        {
            return NotFound();
        }

        // Update basic properties
        quiz.QuizName = quizDto.QuizName;
        quiz.LastModifiedAt = DateTime.UtcNow;

        // Clear existing questions
        _context.QuizQuestions.RemoveRange(quiz.Questions);

        // Add new questions
        if (quizDto.QuestionIds != null && quizDto.QuestionIds.Any())
        {
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
            }
        }

        _context.Entry(quiz).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!QuizExists(id))
            {
                return NotFound();
            }
            else
            {
                throw;
            }
        }

        return NoContent();
    }

    // DELETE: api/Quiz/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteQuiz(int id)
    {
        var quiz = await _context.Quizzes
            .Include(q => q.Questions)
            .FirstOrDefaultAsync(q => q.Id == id);

        if (quiz == null)
        {
            return NotFound();
        }

        // Remove all question associations
        _context.QuizQuestions.RemoveRange(quiz.Questions);

        // Remove the quiz
        _context.Quizzes.Remove(quiz);
        await _context.SaveChangesAsync();

        return NoContent();
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

