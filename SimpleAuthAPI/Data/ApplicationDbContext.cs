using Microsoft.EntityFrameworkCore;
using SimpleAuthAPI.Models;

namespace SimpleAuthAPI.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<QuestionSimple> Questions { get; set; }
        public DbSet<AnswerSimple> Answers { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AnswerSimple>()
                .HasOne(a => a.Question)
                .WithMany(q => q.Answers)
                .HasForeignKey(a => a.QuestionSimpleId)
                .OnDelete(DeleteBehavior.Cascade); // ✅ Ensure answers are deleted when their question is deleted
        }

    }
}