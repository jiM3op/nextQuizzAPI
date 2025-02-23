using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SimpleAuthAPI.Data;
using SimpleAuthAPI.Models;

namespace SimpleAuthAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CategoryController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CategoryController> _logger;

        public CategoryController(ApplicationDbContext context, ILogger<CategoryController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // ✅ GET: Fetch all categories
        [HttpGet]
        public async Task<IActionResult> GetCategories()
        {
            var categories = await _context.Categories.ToListAsync();
            return Ok(categories);
        }

        // ✅ POST: Create a new category
        [HttpPost]
        public async Task<IActionResult> CreateCategory([FromBody] Category newCategory)
        {
            if (newCategory == null || string.IsNullOrWhiteSpace(newCategory.Value) || string.IsNullOrWhiteSpace(newCategory.Label))
            {
                return BadRequest("Invalid category data.");
            }

            // ✅ Ensure Value is unique
            if (await _context.Categories.AnyAsync(c => c.Value == newCategory.Value))
            {
                return Conflict("Category with this Value already exists.");
            }

            _context.Categories.Add(newCategory);
            await _context.SaveChangesAsync();

            _logger.LogInformation("✅ Created Category: {Label}", newCategory.Label);
            return CreatedAtAction(nameof(GetCategories), new { id = newCategory.Id }, newCategory);
        }

        // ✅ PATCH: Update an existing category
        [HttpPatch("{id}")]
        public async Task<IActionResult> UpdateCategory(int id, [FromBody] Category updatedCategory)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null) return NotFound();

            category.Value = updatedCategory.Value;
            category.Label = updatedCategory.Label;

            _context.Categories.Update(category);
            await _context.SaveChangesAsync();

            return Ok(category);
        }

        // ✅ DELETE: Remove a category
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null) return NotFound();

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // Get Usagecount in QuestionSimple Objects
        [HttpGet("usage")]
        public async Task<IActionResult> GetCategoryUsage()
        {
            var categoryUsage = await _context.Categories
                .Select(c => new
                {
                    CategoryId = c.Id,
                    UsageCount = _context.Questions.Count(q => q.Categories.Contains(c.Id))
                })
                .ToListAsync();

            return Ok(categoryUsage);
        }
    }
}
