using Microsoft.Extensions.DependencyInjection;

namespace UniAcademic.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // TODO: Register MediatR, AutoMapper, Validators, etc.
        return services;
    }
}
