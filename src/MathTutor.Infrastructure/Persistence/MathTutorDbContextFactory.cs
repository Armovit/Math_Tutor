using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace MathTutor.Infrastructure.Persistence;

public sealed class MathTutorDbContextFactory : IDesignTimeDbContextFactory<MathTutorDbContext>
{
    public MathTutorDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<MathTutorDbContext>()
            .UseSqlServer(DependencyInjection.DefaultConnectionString)
            .Options;
        return new MathTutorDbContext(options);
    }
}
