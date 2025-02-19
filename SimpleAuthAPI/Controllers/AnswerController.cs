namespace SimpleAuthAPI.Controllers;

using Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using Models;

[Route("api/[controller]")]
[ApiController]
public class AnswerController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AnswerController> _logger;

    public AnswerController(ApplicationDbContext context, ILogger<AnswerController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // ✅ GET: Fetch a single answer
    [HttpGet("{id}")]
    public async Task<IActionResult> GetAnswer(int id)
    {
        var answer = await _context.Answers.FindAsync(id);
        if (answer == null) return NotFound();
        return Ok(answer);
    }

    // ✅ GET: Fetch answers for a specific question
    [HttpGet]
    public async Task<IActionResult> GetAnswers([FromQuery] int questionId)
    {
        var answers = await _context.Answers.Where(a => a.QuestionSimpleId == questionId).ToListAsync();
        return Ok(answers);
    }

    // ✅ PATCH: Update an answer
    [HttpPatch("{id}")]
    public async Task<IActionResult> UpdateAnswer(int id, [FromBody] AnswerSimple updatedAnswer)
    {
        var answer = await _context.Answers.FindAsync(id);
        if (answer == null) return NotFound();

        answer.AnswerBody = updatedAnswer.AnswerBody;
        answer.AnswerCorrect = updatedAnswer.AnswerCorrect;
        _context.Answers.Update(answer);
        await _context.SaveChangesAsync();

        return Ok(answer);
    }

    // ✅ DELETE: Remove an answer
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteAnswer(int id)
    {
        var answer = await _context.Answers.FindAsync(id);
        if (answer == null) return NotFound();

        _context.Answers.Remove(answer);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}