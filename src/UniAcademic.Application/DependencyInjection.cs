using Microsoft.Extensions.DependencyInjection;
using UniAcademic.Application.Abstractions.Attendance;
using UniAcademic.Application.Abstractions.CourseOfferings;
using UniAcademic.Application.Abstractions.Courses;
using UniAcademic.Application.Abstractions.Enrollments;
using UniAcademic.Application.Abstractions.Faculties;
using UniAcademic.Application.Abstractions.Grades;
using UniAcademic.Application.Abstractions.GradeResults;
using UniAcademic.Application.Abstractions.LecturerAssignments;
using UniAcademic.Application.Abstractions.LecturerProfiles;
using UniAcademic.Application.Abstractions.Materials;
using UniAcademic.Application.Abstractions.Semesters;
using UniAcademic.Application.Abstractions.StudentPortal;
using UniAcademic.Application.Abstractions.Transcripts;
using UniAcademic.Application.Abstractions.StudentClasses;
using UniAcademic.Application.Abstractions.StudentProfiles;
using UniAcademic.Application.Abstractions.Rosters;
using UniAcademic.Application.Abstractions.LecturerPortal;
using UniAcademic.Application.Features.Attendance;
using UniAcademic.Application.Features.CourseOfferings;
using UniAcademic.Application.Features.Courses;
using UniAcademic.Application.Features.Enrollments;
using UniAcademic.Application.Features.Faculties;
using UniAcademic.Application.Features.Grades;
using UniAcademic.Application.Features.GradeResults;
using UniAcademic.Application.Features.LecturerAssignments;
using UniAcademic.Application.Features.LecturerProfiles;
using UniAcademic.Application.Features.Materials;
using UniAcademic.Application.Features.LecturerPortal;
using UniAcademic.Application.Features.Rosters;
using UniAcademic.Application.Features.Semesters;
using UniAcademic.Application.Features.StudentPortal;
using UniAcademic.Application.Features.StudentClasses;
using UniAcademic.Application.Features.StudentProfiles;
using UniAcademic.Application.Features.Transcripts;

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
        services.AddScoped<IGradeResultService, GradeResultService>();
        services.AddScoped<ILecturerAssignmentService, LecturerAssignmentService>();
        services.AddScoped<ILecturerProfileService, LecturerProfileService>();
        services.AddScoped<ILecturerPortalService, LecturerPortalService>();
        services.AddScoped<ICourseMaterialService, CourseMaterialService>();
        services.AddScoped<ICourseOfferingRosterService, CourseOfferingRosterService>();
        services.AddScoped<ISemesterService, SemesterService>();
        services.AddScoped<IStudentPortalService, StudentPortalService>();
        services.AddScoped<IStudentClassService, StudentClassService>();
        services.AddScoped<IStudentProfileService, StudentProfileService>();
        services.AddScoped<ITranscriptService, TranscriptService>();
        return services;
    }
}
