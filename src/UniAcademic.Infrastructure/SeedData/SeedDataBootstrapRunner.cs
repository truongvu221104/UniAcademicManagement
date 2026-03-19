using Microsoft.Extensions.DependencyInjection;

namespace UniAcademic.Infrastructure.SeedData;

public static class SeedDataBootstrapRunner
{
    public static async Task RunInfrastructureBootstrapAsync(this IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
    {
        using var scope = serviceProvider.CreateScope();
        var bootstrapService = scope.ServiceProvider.GetRequiredService<SeedDataBootstrapService>();
        await bootstrapService.RunAsync(cancellationToken);
    }
}
