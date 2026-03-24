using UniAcademic.Domain.Entities.Academic;
using UniAcademic.Domain.Entities.Identity;

namespace UniAcademic.Application.Abstractions.Persistence;

public interface IAppDbContext
{
    IQueryable<Faculty> Faculties { get; }

    IQueryable<StudentClass> StudentClasses { get; }

    IQueryable<StudentProfile> StudentProfiles { get; }

    IQueryable<Enrollment> Enrollments { get; }

    IQueryable<Course> Courses { get; }

    IQueryable<Semester> Semesters { get; }

    IQueryable<CourseOffering> CourseOfferings { get; }

    IQueryable<CourseOfferingRosterSnapshot> CourseOfferingRosterSnapshots { get; }

    IQueryable<CourseOfferingRosterItem> CourseOfferingRosterItems { get; }

    IQueryable<AttendanceSession> AttendanceSessions { get; }

    IQueryable<AttendanceRecord> AttendanceRecords { get; }

    IQueryable<GradeCategory> GradeCategories { get; }

    IQueryable<GradeEntry> GradeEntries { get; }

    IQueryable<FileMetadata> FileMetadatas { get; }

    IQueryable<CourseMaterial> CourseMaterials { get; }

    IQueryable<User> Users { get; }

    IQueryable<Role> Roles { get; }

    IQueryable<Permission> Permissions { get; }

    IQueryable<RefreshToken> RefreshTokens { get; }

    IQueryable<UserSession> UserSessions { get; }

    IQueryable<AuditLog> AuditLogs { get; }

    Task AddAsync<TEntity>(TEntity entity, CancellationToken cancellationToken = default)
        where TEntity : class;

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
