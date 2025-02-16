using Microsoft.EntityFrameworkCore;
using SimpleAuthAPI.Data;
using SimpleAuthAPI.Models;

namespace SimpleAuthAPI.Data
{
    public class QuestionDbContext : DbContext
    {
        public QuestionDbContext(DbContextOptions<QuestionDbContext> options) : base(options) { }

        public DbSet<QuestionSimple> Questions { get; set; }
        public DbSet<AnswerSimple> Answers { get; set; }
    }
}
