using FluentAssertions;
using ClosedXML.Excel;
using MathTutor.Application.DTOs;
using MathTutor.Application.Services;
using MathTutor.Domain.Enums;
using MathTutor.Infrastructure.Persistence;
using MathTutor.Infrastructure.Seed;
using MathTutor.Infrastructure.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace MathTutor.IntegrationTests.Persistence;

public sealed class DatabaseAndAuthTests
{
    [Fact]
    public async Task SeedData_creates_admin_materials_tasks_and_notifications()
    {
        await using var fixture = await SqliteFixture.CreateAsync();
        var db = fixture.Context;
        var hasher = new Pbkdf2PasswordHasher();

        await new SeedData(db, hasher).SeedAsync();
        await new SeedData(db, hasher).SeedAsync();

        (await db.Users.CountAsync()).Should().BeGreaterThanOrEqualTo(3);
        (await db.Users.CountAsync(x => x.Email == "admin@mathtutor.local")).Should().Be(1);
        (await db.EducationalMaterials.CountAsync()).Should().BeGreaterThanOrEqualTo(4);
        (await db.MathTasks.CountAsync()).Should().BeGreaterThanOrEqualTo(3);
        (await db.Notifications.CountAsync()).Should().BeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task SeedData_groups_tests_by_topics_and_creates_full_tests()
    {
        await using var fixture = await SqliteFixture.CreateAsync();
        var db = fixture.Context;
        var hasher = new Pbkdf2PasswordHasher();

        await new SeedData(db, hasher).SeedAsync();

        var testGroups = await db.EducationalMaterials
            .Include(x => x.Tasks)
            .Where(x => x.Type == MaterialType.Test)
            .GroupBy(x => x.Topic)
            .Select(x => new { Topic = x.Key, TestsCount = x.Count(), MinTaskCount = x.Min(t => t.Tasks.Count) })
            .ToListAsync();

        testGroups.Should().Contain(x => x.Topic == "линейные уравнения" && x.TestsCount >= 2);
        testGroups.Should().Contain(x => x.Topic == "проценты и доли" && x.TestsCount >= 2);
        testGroups.Should().OnlyContain(x => x.MinTaskCount >= 10);
    }

    [Fact]
    public async Task AuthService_registers_student_and_blocks_invalid_login()
    {
        await using var fixture = await SqliteFixture.CreateAsync();
        var db = fixture.Context;
        var service = new AuthService(db, new Pbkdf2PasswordHasher(), new PasswordValidator());

        var registered = await service.RegisterAsync(new RegisterRequest("Ivan", "Petrov", "ivan@test.local", "Student123!", "Student123!"));
        registered.Succeeded.Should().BeTrue(registered.Message);
        registered.Value!.Role.Should().Be(UserRole.Student);

        var duplicate = await service.RegisterAsync(new RegisterRequest("Ivan", "Petrov", "ivan@test.local", "Student123!", "Student123!"));
        duplicate.Succeeded.Should().BeFalse();

        var login = await service.LoginAsync(new LoginRequest("ivan@test.local", "Student123!"));
        login.Succeeded.Should().BeTrue(login.Message);

        var user = await db.Users.FirstAsync(x => x.Email == "ivan@test.local");
        user.IsBlocked = true;
        await db.SaveChangesAsync();

        var blocked = await service.LoginAsync(new LoginRequest("ivan@test.local", "Student123!"));
        blocked.Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task TaskService_auto_checks_numeric_answers()
    {
        await using var fixture = await SqliteFixture.CreateAsync();
        var db = fixture.Context;
        var hasher = new Pbkdf2PasswordHasher();
        await new SeedData(db, hasher).SeedAsync();

        var user = await db.Users.FirstAsync(x => x.Role == UserRole.Student);
        var task = await db.MathTasks.FirstAsync(x => x.CorrectAnswer == "-3");
        var service = new TaskService(db, CreateEmailService(db));

        var result = await service.SubmitAnswerAsync(new SubmitAnswerRequest(user.Id, task.Id, "-3.000"));

        result.Succeeded.Should().BeTrue();
        result.Value!.Score.Should().Be(task.MaxScore);
    }

    [Fact]
    public async Task TaskService_auto_checks_choice_answers()
    {
        await using var fixture = await SqliteFixture.CreateAsync();
        var db = fixture.Context;
        var hasher = new Pbkdf2PasswordHasher();
        await new SeedData(db, hasher).SeedAsync();

        var user = await db.Users.FirstAsync(x => x.Role == UserRole.Student);
        var task = await db.MathTasks.FirstAsync(x => x.Title == "Верные равенства");
        var service = new TaskService(db, CreateEmailService(db));

        var result = await service.SubmitAnswerAsync(new SubmitAnswerRequest(user.Id, task.Id, "1/2;0.5"));

        result.Succeeded.Should().BeTrue();
        result.Value!.Score.Should().Be(task.MaxScore);
    }

    [Fact]
    public async Task TestService_saves_attempt_with_total_score()
    {
        await using var fixture = await SqliteFixture.CreateAsync();
        var db = fixture.Context;
        var hasher = new Pbkdf2PasswordHasher();
        await new SeedData(db, hasher).SeedAsync();

        var user = await db.Users.FirstAsync(x => x.Role == UserRole.Student);
        var material = await db.EducationalMaterials.FirstAsync(x => x.Title == "Тест: проценты и доли");
        var tasks = await db.MathTasks.Where(x => x.EducationalMaterialId == material.Id).OrderBy(x => x.Id).ToListAsync();
        var service = new TestService(db, CreateEmailService(db));

        var result = await service.SubmitTestAsync(new SubmitTestRequest(user.Id, material.Id, tasks.Select(x => new TestAnswerDto(x.Id, x.CorrectAnswer!)).ToList()));

        result.Succeeded.Should().BeTrue(result.Message);
        result.Value!.Submissions.Should().HaveCount(tasks.Count);
        result.Value.Score.Should().Be(result.Value.MaxScore);
        result.Value.Percent.Should().Be(100);
        result.Value.Grade.Should().Be(10);
        (await db.TestAttempts.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task ProgressService_includes_recent_test_attempts()
    {
        await using var fixture = await SqliteFixture.CreateAsync();
        var db = fixture.Context;
        var hasher = new Pbkdf2PasswordHasher();
        await new SeedData(db, hasher).SeedAsync();

        var user = await db.Users.FirstAsync(x => x.Role == UserRole.Student);
        var material = await db.EducationalMaterials.FirstAsync(x => x.Title == "Тест: линейные уравнения");
        var tasks = await db.MathTasks.Where(x => x.EducationalMaterialId == material.Id).OrderBy(x => x.Id).ToListAsync();
        await new TestService(db, CreateEmailService(db)).SubmitTestAsync(new SubmitTestRequest(user.Id, material.Id, tasks.Select(x => new TestAnswerDto(x.Id, x.CorrectAnswer!)).ToList()));

        var progress = await new ProgressService(db).GetStudentProgressAsync(user.Id);

        progress.RecentTestAttempts.Should().ContainSingle(x => x.MaterialTitle == material.Title);
        progress.BestTestPercent.Should().Be(100);
        progress.AverageTestPercent.Should().Be(100);
    }

    [Fact]
    public async Task ProgressService_calculates_topic_progress_and_recommendations()
    {
        await using var fixture = await SqliteFixture.CreateAsync();
        var db = fixture.Context;
        await new SeedData(db, new Pbkdf2PasswordHasher()).SeedAsync();

        var user = await db.Users.FirstAsync(x => x.Role == UserRole.Student);
        var weakMaterial = await db.EducationalMaterials.FirstAsync(x => x.Title == "Тест: линейные уравнения");
        var strongMaterial = await db.EducationalMaterials.FirstAsync(x => x.Title == "Тест: проценты и доли");
        var weakTasks = await db.MathTasks.Where(x => x.EducationalMaterialId == weakMaterial.Id).OrderBy(x => x.Id).ToListAsync();
        var strongTasks = await db.MathTasks.Where(x => x.EducationalMaterialId == strongMaterial.Id).OrderBy(x => x.Id).ToListAsync();
        var testService = new TestService(db, CreateEmailService(db));

        await testService.SubmitTestAsync(new SubmitTestRequest(user.Id, weakMaterial.Id, weakTasks.Select(x => new TestAnswerDto(x.Id, "wrong")).ToList()));
        await testService.SubmitTestAsync(new SubmitTestRequest(user.Id, strongMaterial.Id, strongTasks.Select(x => new TestAnswerDto(x.Id, x.CorrectAnswer!)).ToList()));

        var progress = await new ProgressService(db).GetStudentProgressAsync(user.Id);

        progress.TopicProgress.Should().Contain(x => string.Equals(x.Topic, weakMaterial.Topic, StringComparison.OrdinalIgnoreCase) && x.AveragePercent < 70);
        progress.TopicProgress.Should().Contain(x => string.Equals(x.Topic, strongMaterial.Topic, StringComparison.OrdinalIgnoreCase) && x.BestPercent == 100);
        progress.Recommendations.Should().Contain(x => string.Equals(x.Topic, weakMaterial.Topic, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Material_and_task_filters_return_expected_items()
    {
        await using var fixture = await SqliteFixture.CreateAsync();
        var db = fixture.Context;
        await new SeedData(db, new Pbkdf2PasswordHasher()).SeedAsync();

        var materials = await new MaterialService(db).GetMaterialsAsync(new MaterialQuery(Topic: "линей", Type: MaterialType.Test, Difficulty: DifficultyLevel.Beginner, PublishedOnly: true));
        materials.Should().OnlyContain(x => x.Topic.Contains("линей", StringComparison.OrdinalIgnoreCase) && x.Type == MaterialType.Test && x.IsPublished);

        var taskService = new TaskService(db, CreateEmailService(db));
        var tasks = await taskService.GetTasksAsync(new TaskQuery(Search: "уравнение", Difficulty: DifficultyLevel.Beginner));
        tasks.Should().NotBeEmpty();
        tasks.Should().OnlyContain(x => x.Difficulty == DifficultyLevel.Beginner);
    }

    [Fact]
    public async Task Admin_can_create_test_toggle_publication_and_find_it_by_partial_filters()
    {
        await using var fixture = await SqliteFixture.CreateAsync();
        var db = fixture.Context;
        await new SeedData(db, new Pbkdf2PasswordHasher()).SeedAsync();

        var admin = await db.Users.FirstAsync(x => x.Role == UserRole.Admin);
        var materialService = new MaterialService(db);

        var created = await materialService.SaveMaterialAsync(
            new MaterialEditDto(0, "Админский тест по дробям", "дроби", "контроль", MaterialType.Test, "Проверка ручного создания теста.", DifficultyLevel.Beginner, "Инструкция для теста.", true),
            admin.Id);

        created.Succeeded.Should().BeTrue(created.Message);
        created.Value.Should().NotBeNull();

        var unpublished = await materialService.SaveMaterialAsync(
            new MaterialEditDto(created.Value!.Id, created.Value.Title, created.Value.Topic, created.Value.Section, created.Value.Type, created.Value.Description, created.Value.Difficulty, created.Value.TheoryContent, false),
            admin.Id);

        unpublished.Succeeded.Should().BeTrue(unpublished.Message);

        var allMatches = await materialService.GetMaterialsAsync(new MaterialQuery(Search: "дроб", Topic: "дро", Section: "конт", Type: MaterialType.Test));
        var publishedMatches = await materialService.GetMaterialsAsync(new MaterialQuery(Search: "дроб", Type: MaterialType.Test, PublishedOnly: true));

        allMatches.Should().ContainSingle(x => x.Id == created.Value!.Id && !x.IsPublished);
        publishedMatches.Should().NotContain(x => x.Id == created.Value!.Id);
    }

    [Fact]
    public async Task TestService_filters_attempt_history()
    {
        await using var fixture = await SqliteFixture.CreateAsync();
        var db = fixture.Context;
        await new SeedData(db, new Pbkdf2PasswordHasher()).SeedAsync();

        var user = await db.Users.FirstAsync(x => x.Role == UserRole.Student);
        var firstMaterial = await db.EducationalMaterials.FirstAsync(x => x.Title == "Тест: линейные уравнения");
        var secondMaterial = await db.EducationalMaterials.FirstAsync(x => x.Title == "Тест: проценты и доли");
        var service = new TestService(db, CreateEmailService(db));
        foreach (var material in new[] { firstMaterial, secondMaterial })
        {
            var tasks = await db.MathTasks.Where(x => x.EducationalMaterialId == material.Id).OrderBy(x => x.Id).ToListAsync();
            await service.SubmitTestAsync(new SubmitTestRequest(user.Id, material.Id, tasks.Select(x => new TestAnswerDto(x.Id, x.CorrectAnswer!)).ToList()));
        }

        var attempts = await service.GetAttemptsAsync(new TestAttemptQuery(user.Id, Topic: firstMaterial.Topic));

        attempts.Should().ContainSingle();
        attempts[0].MaterialTopic.Should().Be(firstMaterial.Topic);
    }

    [Fact]
    public async Task ReportExportService_writes_student_progress_workbook()
    {
        await using var fixture = await SqliteFixture.CreateAsync();
        var db = fixture.Context;
        await new SeedData(db, new Pbkdf2PasswordHasher()).SeedAsync();

        var user = await db.Users.FirstAsync(x => x.Role == UserRole.Student);
        var progress = await new ProgressService(db).GetStudentProgressAsync(user.Id);
        await using var stream = new MemoryStream();

        await new ReportExportService().ExportStudentProgressAsync(progress, stream);

        stream.Position = 0;
        using var workbook = new XLWorkbook(stream);
        workbook.Worksheets.Select(x => x.Name).Should().Contain(["Сводка", "Темы", "Тесты"]);
        workbook.Worksheet("Сводка").Cell(1, 1).GetString().Should().Be("Показатель");
    }

    [Fact]
    public async Task EmailService_logs_skipped_messages_when_smtp_is_disabled()
    {
        await using var fixture = await SqliteFixture.CreateAsync();
        var db = fixture.Context;
        var service = CreateEmailService(db);

        await service.SendAsync("student@test.local", "Subject", "Body");

        var log = await db.EmailLogs.SingleAsync();
        log.Status.Should().Be("Skipped");
        log.ToEmail.Should().Be("student@test.local");
    }

    [Fact]
    public async Task Admin_services_see_seeded_users_statistics_reviews_theory_and_tests()
    {
        await using var fixture = await SqliteFixture.CreateAsync();
        var db = fixture.Context;
        var hasher = new Pbkdf2PasswordHasher();
        await new SeedData(db, hasher).SeedAsync();

        var users = await new UserManagementService(db).SearchUsersAsync(null);
        var statistics = await new ProgressService(db).GetAdminStatisticsAsync();
        var reviews = await new ReviewService(db).GetReviewsAsync();
        var materialService = new MaterialService(db);
        var theories = await materialService.GetMaterialsAsync(new MaterialQuery(Type: MaterialType.Theory));
        var tests = await materialService.GetMaterialsAsync(new MaterialQuery(Type: MaterialType.Test));
        var admin = await db.Users.FirstAsync(x => x.Role == UserRole.Admin);

        var created = await materialService.SaveMaterialAsync(
            new MaterialEditDto(0, "Админский тест создания теории", "админ", "проверка", MaterialType.Theory, "Материал создан администратором.", DifficultyLevel.Beginner, "Полный текст теории, созданной администратором.", true),
            admin.Id);

        users.Should().Contain(x => x.Role == UserRole.Student);
        statistics.StudentsCount.Should().BeGreaterThanOrEqualTo(2);
        statistics.Students.Should().NotBeEmpty();
        reviews.Should().NotBeEmpty();
        theories.Should().NotBeEmpty();
        tests.Should().NotBeEmpty();
        created.Succeeded.Should().BeTrue(created.Message);
    }

    private static EmailLogService CreateEmailService(MathTutorDbContext db)
        => new(db, Options.Create(new EmailOptions { Enabled = false, UseSmtp = false }));

    private sealed class SqliteFixture : IAsyncDisposable
    {
        private readonly SqliteConnection connection;
        public MathTutorDbContext Context { get; }

        private SqliteFixture(SqliteConnection connection, MathTutorDbContext context)
        {
            this.connection = connection;
            Context = context;
        }

        public static async Task<SqliteFixture> CreateAsync()
        {
            var connection = new SqliteConnection("DataSource=:memory:");
            await connection.OpenAsync();
            var options = new DbContextOptionsBuilder<MathTutorDbContext>().UseSqlite(connection).Options;
            var context = new MathTutorDbContext(options);
            await context.Database.EnsureCreatedAsync();
            return new SqliteFixture(connection, context);
        }

        public async ValueTask DisposeAsync()
        {
            await Context.DisposeAsync();
            await connection.DisposeAsync();
        }
    }
}
