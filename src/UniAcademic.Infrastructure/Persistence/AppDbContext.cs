using Microsoft.EntityFrameworkCore;
using UniAcademic.Application.Abstractions.Persistence;
using UniAcademic.Domain.Entities.Academic;
using UniAcademic.Domain.Entities.Identity;
using UniAcademic.Infrastructure.Persistence.SeedData;
using UniAcademic.SharedKernel;

namespace UniAcademic.Infrastructure.Persistence;

public sealed class AppDbContext : DbContext, IAppDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();

    public DbSet<Faculty> FacultiesSet => Set<Faculty>();

    public DbSet<StudentClass> StudentClassesSet => Set<StudentClass>();

    public DbSet<StudentProfile> StudentProfilesSet => Set<StudentProfile>();

    public DbSet<Enrollment> EnrollmentsSet => Set<Enrollment>();

    public DbSet<Course> CoursesSet => Set<Course>();

    public DbSet<Semester> SemestersSet => Set<Semester>();

    public DbSet<CourseOffering> CourseOfferingsSet => Set<CourseOffering>();

    public DbSet<CourseOfferingRosterSnapshot> CourseOfferingRosterSnapshotsSet => Set<CourseOfferingRosterSnapshot>();

    public DbSet<CourseOfferingRosterItem> CourseOfferingRosterItemsSet => Set<CourseOfferingRosterItem>();

    public DbSet<AttendanceSession> AttendanceSessionsSet => Set<AttendanceSession>();

    public DbSet<AttendanceRecord> AttendanceRecordsSet => Set<AttendanceRecord>();

    public DbSet<GradeCategory> GradeCategoriesSet => Set<GradeCategory>();

    public DbSet<GradeEntry> GradeEntriesSet => Set<GradeEntry>();

    public DbSet<SeedDatasetState> SeedDatasetStates => Set<SeedDatasetState>();

    public DbSet<Role> Roles => Set<Role>();

    public DbSet<Permission> Permissions => Set<Permission>();

    public DbSet<UserRole> UserRoles => Set<UserRole>();

    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<UserSession> UserSessions => Set<UserSession>();

    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    IQueryable<User> IAppDbContext.Users => Users;

    IQueryable<Faculty> IAppDbContext.Faculties => FacultiesSet;

    IQueryable<StudentClass> IAppDbContext.StudentClasses => StudentClassesSet;

    IQueryable<StudentProfile> IAppDbContext.StudentProfiles => StudentProfilesSet;

    IQueryable<Enrollment> IAppDbContext.Enrollments => EnrollmentsSet;

    IQueryable<Course> IAppDbContext.Courses => CoursesSet;

    IQueryable<Semester> IAppDbContext.Semesters => SemestersSet;

    IQueryable<CourseOffering> IAppDbContext.CourseOfferings => CourseOfferingsSet;

    IQueryable<CourseOfferingRosterSnapshot> IAppDbContext.CourseOfferingRosterSnapshots => CourseOfferingRosterSnapshotsSet;

    IQueryable<CourseOfferingRosterItem> IAppDbContext.CourseOfferingRosterItems => CourseOfferingRosterItemsSet;

    IQueryable<AttendanceSession> IAppDbContext.AttendanceSessions => AttendanceSessionsSet;

    IQueryable<AttendanceRecord> IAppDbContext.AttendanceRecords => AttendanceRecordsSet;

    IQueryable<GradeCategory> IAppDbContext.GradeCategories => GradeCategoriesSet;

    IQueryable<GradeEntry> IAppDbContext.GradeEntries => GradeEntriesSet;

    IQueryable<Role> IAppDbContext.Roles => Roles;

    IQueryable<Permission> IAppDbContext.Permissions => Permissions;

    IQueryable<RefreshToken> IAppDbContext.RefreshTokens => RefreshTokens;

    IQueryable<UserSession> IAppDbContext.UserSessions => UserSessions;

    IQueryable<AuditLog> IAppDbContext.AuditLogs => AuditLogs;

    public new Task AddAsync<TEntity>(TEntity entity, CancellationToken cancellationToken = default)
        where TEntity : class
    {
        return Set<TEntity>().AddAsync(entity, cancellationToken).AsTask();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var utcNow = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries<IAuditableEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAtUtc = utcNow;
            }

            if (entry.State == EntityState.Modified)
            {
                entry.Entity.ModifiedAtUtc = utcNow;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
