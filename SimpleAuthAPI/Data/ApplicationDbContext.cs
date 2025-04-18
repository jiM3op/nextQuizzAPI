﻿using Microsoft.EntityFrameworkCore;
using SimpleAuthAPI.Models;

namespace SimpleAuthAPI.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<QuestionSimple> Questions { get; set; }
        public DbSet<AnswerSimple> Answers { get; set; }
        public DbSet<Category> Categories { get; set; }

        public DbSet<User> Users { get; set; }

        public DbSet<Quiz> Quizzes { get; set; }
        public DbSet<QuizQuestion> QuizQuestions { get; set; }

        public DbSet<QuizSession> QuizSessions { get; set; }
        public DbSet<UserAnswer> UserAnswers { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // ✅ Ensure each Answer belongs to a Question & is deleted when Question is deleted
            modelBuilder.Entity<AnswerSimple>()
                .HasOne(a => a.Question)
                .WithMany(q => q.Answers)
                .HasForeignKey(a => a.QuestionSimpleId)
                .OnDelete(DeleteBehavior.Cascade); // ✅ Delete answers when question is deleted

            // ✅ Ensure unique category values
            modelBuilder.Entity<Category>().HasIndex(c => c.Value).IsUnique();

            // ❌ REMOVE MANY-TO-MANY RELATIONSHIP (Since `categories` is now a list of strings)
            // modelBuilder.Entity<QuestionSimple>()
            //     .HasMany(q => q.Categories)
            //     .WithMany(c => c.Questions); 

            // ✅ Prevent cascade delete conflicts with User
            modelBuilder.Entity<QuestionSimple>()
                .HasOne(q => q.Creator)
                .WithMany(u => u.CreatedQuestions)
                .HasForeignKey(q => q.CreatedById)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Quiz>()
                .HasOne(q => q.Creator)
                .WithMany(u => u.CreatedQuizzes)
                .HasForeignKey(q => q.CreatedById)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<QuizSession>()
                .HasOne(qs => qs.User)
                .WithMany(u => u.QuizSessions)
                .HasForeignKey(qs => qs.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            base.OnModelCreating(modelBuilder);
        }
    }
}