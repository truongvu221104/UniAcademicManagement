using Microsoft.Extensions.DependencyInjection;
using UniAcademic.Application.Abstractions.Courses;
using UniAcademic.Application.Abstractions.Faculties;
using UniAcademic.Application.Abstractions.StudentClasses;
using UniAcademic.Application.Features.Courses;
using UniAcademic.Application.Features.Faculties;
using UniAcademic.Application.Features.StudentClasses;

namespace UniAcademic.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ICourseService, CourseService>();
        services.AddScoped<IFacultyService, FacultyService>();
        services.AddScoped<IStudentClassService, StudentClassService>();
        return services;
    }
}
