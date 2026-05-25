using MathTutor.Domain.Entities;
using MathTutor.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace MathTutor.Infrastructure.Persistence;

public sealed class MathTutorDbContext(DbContextOptions<MathTutorDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<EducationalMaterial> EducationalMaterials => Set<EducationalMaterial>();
    public DbSet<MathTask> MathTasks => Set<MathTask>();
    public DbSet<AnswerOption> AnswerOptions => Set<AnswerOption>();
    public DbSet<TestAttempt> TestAttempts => Set<TestAttempt>();
    public DbSet<TaskSubmission> TaskSubmissions => Set<TaskSubmission>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<EmailLog> EmailLogs => Set<EmailLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(x => x.Email).IsUnique();
            entity.Property(x => x.FirstName).HasMaxLength(80).IsRequired();
            entity.Property(x => x.LastName).HasMaxLength(80).IsRequired();
            entity.Property(x => x.Email).HasMaxLength(180).IsRequired();
            entity.Property(x => x.PasswordHash).HasMaxLength(300).IsRequired();
            entity.Property(x => x.Role).HasConversion<string>().HasMaxLength(32).IsRequired();
        });

        modelBuilder.Entity<EducationalMaterial>(entity =>
        {
            entity.Property(x => x.Title).HasMaxLength(160).IsRequired();
            entity.Property(x => x.Topic).HasMaxLength(120).HasDefaultValue(string.Empty);
            entity.Property(x => x.Section).HasMaxLength(120).HasDefaultValue(string.Empty);
            entity.Property(x => x.Description).HasMaxLength(800).IsRequired();
            entity.Property(x => x.TheoryContent).HasMaxLength(8000);
            entity.Property(x => x.Type).HasConversion<string>().HasMaxLength(32);
            entity.Property(x => x.Difficulty).HasConversion<string>().HasMaxLength(32);
            entity.HasOne(x => x.CreatedByUser).WithMany(x => x.CreatedMaterials).HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<MathTask>(entity =>
        {
            entity.Property(x => x.Title).HasMaxLength(160).IsRequired();
            entity.Property(x => x.Question).HasMaxLength(3000).IsRequired();
            entity.Property(x => x.CorrectAnswer).HasMaxLength(1000);
            entity.Property(x => x.Explanation).HasMaxLength(3000);
            entity.Property(x => x.AnswerType).HasConversion<string>().HasMaxLength(32);
            entity.Property(x => x.Difficulty).HasConversion<string>().HasMaxLength(32);
            entity.Property(x => x.MaxScore).HasPrecision(8, 2);
            entity.ToTable(t => t.HasCheckConstraint("CK_MathTasks_MaxScore", "[MaxScore] > 0"));
            entity.HasOne(x => x.EducationalMaterial).WithMany(x => x.Tasks).HasForeignKey(x => x.EducationalMaterialId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AnswerOption>(entity =>
        {
            entity.Property(x => x.Text).HasMaxLength(1000).IsRequired();
            entity.HasOne(x => x.MathTask).WithMany(x => x.AnswerOptions).HasForeignKey(x => x.MathTaskId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TaskSubmission>(entity =>
        {
            entity.Property(x => x.Answer).HasMaxLength(3000).IsRequired();
            entity.Property(x => x.Feedback).HasMaxLength(2000);
            entity.Property(x => x.Score).HasPrecision(8, 2);
            entity.Property(x => x.MaxScore).HasPrecision(8, 2);
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
            entity.ToTable(t =>
            {
                t.HasCheckConstraint("CK_TaskSubmissions_Score", "[Score] >= 0");
                t.HasCheckConstraint("CK_TaskSubmissions_MaxScore", "[MaxScore] > 0");
            });
            entity.HasOne(x => x.User).WithMany(x => x.TaskSubmissions).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.MathTask).WithMany(x => x.Submissions).HasForeignKey(x => x.MathTaskId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.TestAttempt).WithMany(x => x.Submissions).HasForeignKey(x => x.TestAttemptId).OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<TestAttempt>(entity =>
        {
            entity.Property(x => x.Score).HasPrecision(8, 2);
            entity.Property(x => x.MaxScore).HasPrecision(8, 2);
            entity.Property(x => x.Percent).HasPrecision(5, 2);
            entity.Property(x => x.Grade).HasDefaultValue(1);
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
            entity.ToTable(t =>
            {
                t.HasCheckConstraint("CK_TestAttempts_Grade", "[Grade] BETWEEN 1 AND 10");
                t.HasCheckConstraint("CK_TestAttempts_Score", "[Score] >= 0");
                t.HasCheckConstraint("CK_TestAttempts_MaxScore", "[MaxScore] > 0");
            });
            entity.HasOne(x => x.User).WithMany(x => x.TestAttempts).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.EducationalMaterial).WithMany(x => x.TestAttempts).HasForeignKey(x => x.EducationalMaterialId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Review>(entity =>
        {
            entity.Property(x => x.Comment).HasMaxLength(1200);
            entity.ToTable(t =>
            {
                t.HasCheckConstraint("CK_Reviews_DifficultyRating", "[DifficultyRating] BETWEEN 1 AND 5");
                t.HasCheckConstraint("CK_Reviews_UsefulnessRating", "[UsefulnessRating] BETWEEN 1 AND 5");
            });
            entity.HasOne(x => x.User).WithMany(x => x.Reviews).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.EducationalMaterial).WithMany(x => x.Reviews).HasForeignKey(x => x.EducationalMaterialId).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(x => x.MathTask).WithMany(x => x.Reviews).HasForeignKey(x => x.MathTaskId).OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.Property(x => x.Title).HasMaxLength(160).IsRequired();
            entity.Property(x => x.Message).HasMaxLength(1200).IsRequired();
            entity.Property(x => x.Type).HasConversion<string>().HasMaxLength(40);
            entity.Property(x => x.RelatedEntityType).HasMaxLength(80);
            entity.HasOne(x => x.User).WithMany(x => x.Notifications).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<EmailLog>(entity =>
        {
            entity.Property(x => x.ToEmail).HasMaxLength(180).IsRequired();
            entity.Property(x => x.Subject).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Body).HasMaxLength(4000).IsRequired();
            entity.Property(x => x.Status).HasMaxLength(40).IsRequired();
            entity.Property(x => x.ErrorMessage).HasMaxLength(2000);
        });
    }
}
