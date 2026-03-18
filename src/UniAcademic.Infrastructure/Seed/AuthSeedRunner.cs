using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using UniAcademic.Infrastructure.Persistence;

namespace UniAcademic.Infrastructure.Seed;

public static class AuthSeedRunner
{
    public static async Task SeedAuthAsync(this IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await dbContext.Database.MigrateAsync(cancellationToken);

        var seedData = scope.ServiceProvider.GetRequiredService<AuthSeedData>();
        await seedData.SeedAsync(cancellationToken);
    }
}
