using MathTutor.Application.Abstractions;
using MathTutor.Application.Services;
using MathTutor.Infrastructure.Persistence;
using MathTutor.Infrastructure.Seed;
using MathTutor.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MathTutor.Infrastructure;

public static class DependencyInjection
{
    public const string DefaultConnectionString = "Server=localhost\\SQLEXPRESS;Database=MathTutor;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("MathTutor") ?? DefaultConnectionString;
        services.Configure<EmailOptions>(options =>
        {
            var section = configuration.GetSection("Email");
            options.Enabled = bool.TryParse(section["Enabled"], out var enabled) && enabled;
            options.UseSmtp = bool.TryParse(section["UseSmtp"], out var useSmtp) && useSmtp;
            options.Host = section["Host"] ?? options.Host;
            options.Port = int.TryParse(section["Port"], out var port) ? port : options.Port;
            options.UseSsl = !bool.TryParse(section["UseSsl"], out var useSsl) || useSsl;
            options.User = section["User"] ?? options.User;
            options.Password = section["Password"] ?? options.Password;
            options.FromAddress = section["FromAddress"] ?? options.FromAddress;
            options.FromName = section["FromName"] ?? options.FromName;
        });
        services.AddDbContext<MathTutorDbContext>(options => options.UseSqlServer(connectionString));
        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddSingleton<IPasswordValidator, PasswordValidator>();
        services.AddScoped<SeedData>();
        services.AddScoped<IDatabaseInitializer, DatabaseInitializer>();
        services.AddSingleton<ISessionService, SessionService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IMaterialService, MaterialService>();
        services.AddScoped<ITaskService, TaskService>();
        services.AddScoped<ITestService, TestService>();
        services.AddScoped<IProgressService, ProgressService>();
        services.AddScoped<IReviewService, ReviewService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IUserManagementService, UserManagementService>();
        services.AddScoped<IReportExportService, ReportExportService>();
        services.AddScoped<IEmailService, EmailLogService>();
        return services;
    }
}
