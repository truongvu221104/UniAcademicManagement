using Microsoft.Extensions.DependencyInjection;
using UniAcademic.Application.Abstractions.Attendance;
using UniAcademic.Application.Abstractions.CourseOfferings;
using UniAcademic.Application.Abstractions.Courses;
using UniAcademic.Application.Abstractions.Enrollments;
using UniAcademic.Application.Abstractions.Faculties;
using UniAcademic.Application.Abstractions.Grades;
using UniAcademic.Application.Abstractions.Semesters;
using UniAcademic.Application.Abstractions.StudentClasses;
using UniAcademic.Application.Abstractions.StudentProfiles;
using UniAcademic.Application.Abstractions.Rosters;
using UniAcademic.Application.Features.Attendance;
using UniAcademic.Application.Features.CourseOfferings;
using UniAcademic.Application.Features.Courses;
using UniAcademic.Application.Features.Enrollments;
using UniAcademic.Application.Features.Faculties;
using UniAcademic.Application.Features.Grades;
using UniAcademic.Application.Features.Rosters;
using UniAcademic.Application.Features.Semesters;
using UniAcademic.Application.Features.StudentClasses;
using UniAcademic.Application.Features.StudentProfiles;

namespace UniAcademic.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAttendanceService, AttendanceService>();
        services.AddScoped<ICourseOfferingService, CourseOfferingService>();
        services.AddScoped<ICourseService, CourseService>();
        services.AddScoped<IEnrollmentService, EnrollmentService>();
        services.AddScoped<IFacultyService, FacultyService>();
        services.AddScoped<IGradeService, GradeService>();
        services.AddScoped<ICourseOfferingRosterService, CourseOfferingRosterService>();
        services.AddScoped<ISemesterService, SemesterService>();
        services.AddScoped<IStudentClassService, StudentClassService>();
        services.AddScoped<IStudentProfileService, StudentProfileService>();
        return services;
    }
}
