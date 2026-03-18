using Microsoft.Extensions.DependencyInjection;
using UniAcademic.Application.Abstractions.Faculties;
using UniAcademic.Application.Features.Faculties;

namespace UniAcademic.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IFacultyService, FacultyService>();
        return services;
    }
}
