using MathTutor.Application.Abstractions;
using MathTutor.Infrastructure.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace MathTutor.Infrastructure.Persistence;

public sealed class DatabaseInitializer(MathTutorDbContext dbContext, SeedData seedData, IConfiguration configuration) : IDatabaseInitializer
{
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await dbContext.Database.MigrateAsync(cancellationToken);
        var seedSetting = configuration["Database:SeedOnStartup"];
        var seedOnStartup = !bool.TryParse(seedSetting, out var parsed) || parsed;
        if (seedOnStartup)
        {
            await seedData.SeedAsync(cancellationToken);
        }
    }
}
