using Microsoft.EntityFrameworkCore;
using UniAcademic.Application.Abstractions.Common;
using UniAcademic.Application.Abstractions.ExamHandoff;
using UniAcademic.Application.Common;
using UniAcademic.Application.Features.Rosters;
using UniAcademic.Application.Models.ExamHandoff;
using UniAcademic.Application.Models.Rosters;
using UniAcademic.Application.Security;
using UniAcademic.Domain.Entities.Academic;
using UniAcademic.Domain.Enums;
using UniAcademic.Infrastructure.Persistence;
using UniAcademic.Infrastructure.Services.Auth;
using Xunit;

namespace UniAcademic.Tests.Integration.Rosters;

public sealed class CourseOfferingRosterServiceTests
{
    [Fact]
    public async Task FinalizeAsync_ShouldCreateSnapshot_AndWriteAudit()
    {
        await using var dbContext = CreateDbContext();
        var studentProfile = await SeedStudentProfileAsync(dbContext, "SV001");
        var courseOffering = await SeedCourseOfferingAsync(dbContext, "CS101-HK1-01", 2);
        dbContext.EnrollmentsSet.Add(new Enrollment
        {
            StudentProfileId = studentProfile.Id,
            CourseOfferingId = courseOffering.Id,
            Status = EnrollmentStatus.Enrolled,
            EnrolledAtUtc = new DateTime(2026, 3, 24, 0, 0, 0, DateTimeKind.Utc),
            CreatedBy = "seed"
        });
        await dbContext.SaveChangesAsync();

        var service = CreateRosterService(dbContext);
        var result = await service.FinalizeAsync(new FinalizeCourseOfferingRosterCommand
        {
            CourseOfferingId = courseOffering.Id,
            Note = "  Chot danh sach  "
        });

        Assert.True(result.IsFinalized);
        Assert.Equal(1, result.ItemCount);
        Assert.Equal("Chot danh sach", result.Note);

        var updatedOffering = await dbContext.CourseOfferingsSet.SingleAsync(x => x.Id == courseOffering.Id);
        Assert.True(updatedOffering.IsRosterFinalized);
        Assert.NotNull(updatedOffering.RosterFinalizedAtUtc);

        var snapshot = await dbContext.CourseOfferingRosterSnapshotsSet.Include(x => x.Items).SingleAsync();
        Assert.Equal(1, snapshot.ItemCount);
        Assert.Single(snapshot.Items);

        var audit = await dbContext.AuditLogs.SingleAsync(x => x.Action == "courseofferingroster.finalize");
        Assert.Equal(nameof(CourseOfferingRosterSnapshot), audit.EntityType);
        Assert.Equal(snapshot.Id.ToString(), audit.EntityId);
    }

    [Fact]
    public async Task FinalizeAsync_ShouldIncludeOnlyEnrolledEnrollments()
    {
        await using var dbContext = CreateDbContext();
        var studentProfileA = await SeedStudentProfileAsync(dbContext, "SV001");
        var studentProfileB = await SeedStudentProfileAsync(dbContext, "SV002");
        var courseOffering = await SeedCourseOfferingAsync(dbContext, "CS101-HK1-01", 3);
        dbContext.EnrollmentsSet.AddRange(
            new Enrollment
            {
                StudentProfileId = studentProfileA.Id,
                CourseOfferingId = courseOffering.Id,
                Status = EnrollmentStatus.Enrolled,
                EnrolledAtUtc = new DateTime(2026, 3, 24, 0, 0, 0, DateTimeKind.Utc),
                CreatedBy = "seed"
            },
            new Enrollment
            {
                StudentProfileId = studentProfileB.Id,
                CourseOfferingId = courseOffering.Id,
                Status = EnrollmentStatus.Dropped,
                EnrolledAtUtc = new DateTime(2026, 3, 22, 0, 0, 0, DateTimeKind.Utc),
                DroppedAtUtc = new DateTime(2026, 3, 23, 0, 0, 0, DateTimeKind.Utc),
                CreatedBy = "seed"
            });
        await dbContext.SaveChangesAsync();

        var service = CreateRosterService(dbContext);
        var result = await service.FinalizeAsync(new FinalizeCourseOfferingRosterCommand
        {
            CourseOfferingId = courseOffering.Id
        });

        Assert.Single(result.Items);
        Assert.Equal("SV001", result.Items.Single().StudentCode);
    }

    [Fact]
    public async Task FinalizeAsync_ShouldFail_WhenCourseOfferingDoesNotExist()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateRosterService(dbContext);

        await Assert.ThrowsAsync<AuthException>(() => service.FinalizeAsync(new FinalizeCourseOfferingRosterCommand
        {
            CourseOfferingId = Guid.NewGuid()
        }));
    }

    [Fact]
    public async Task FinalizeAsync_ShouldFail_WhenCourseOfferingIsSoftDeleted()
    {
        await using var dbContext = CreateDbContext();
        var courseOffering = await SeedCourseOfferingAsync(dbContext, "CS101-HK1-01", 2);
        courseOffering.IsDeleted = true;
        courseOffering.DeletedAtUtc = new DateTime(2026, 3, 24, 0, 0, 0, DateTimeKind.Utc);
        courseOffering.DeletedBy = "seed";
        await dbContext.SaveChangesAsync();

        var service = CreateRosterService(dbContext);
        var exception = await Assert.ThrowsAsync<AuthException>(() => service.FinalizeAsync(new FinalizeCourseOfferingRosterCommand
        {
            CourseOfferingId = courseOffering.Id
        }));

        Assert.Equal("Course offering was not found.", exception.Message);
    }

    [Fact]
    public async Task FinalizeAsync_ShouldFail_WhenAlreadyFinalized()
    {
        await using var dbContext = CreateDbContext();
        var courseOffering = await SeedCourseOfferingAsync(dbContext, "CS101-HK1-01", 2);
        courseOffering.IsRosterFinalized = true;
        courseOffering.RosterFinalizedAtUtc = new DateTime(2026, 3, 24, 0, 0, 0, DateTimeKind.Utc);
        await dbContext.SaveChangesAsync();

        var service = CreateRosterService(dbContext);
        var exception = await Assert.ThrowsAsync<AuthException>(() => service.FinalizeAsync(new FinalizeCourseOfferingRosterCommand
        {
            CourseOfferingId = courseOffering.Id
        }));

        Assert.Equal("Course offering roster was already finalized.", exception.Message);
    }

    [Fact]
    public async Task ReopenAsync_ShouldReopenRoster_WhenNoDownstreamDataExistsAndHandoffDidNotSucceed()
    {
        await using var dbContext = CreateDbContext();
        var studentProfile = await SeedStudentProfileAsync(dbContext, "SV001");
        var courseOffering = await SeedCourseOfferingAsync(dbContext, "CS101-HK1-01", 2);
        dbContext.EnrollmentsSet.Add(new Enrollment
        {
            StudentProfileId = studentProfile.Id,
            CourseOfferingId = courseOffering.Id,
            Status = EnrollmentStatus.Enrolled,
            EnrolledAtUtc = new DateTime(2026, 3, 24, 0, 0, 0, DateTimeKind.Utc),
            CreatedBy = "seed"
        });
        await dbContext.SaveChangesAsync();

        var service = CreateRosterService(dbContext, username: "admin");
        await service.FinalizeAsync(new FinalizeCourseOfferingRosterCommand
        {
            CourseOfferingId = courseOffering.Id
        });
        var snapshot = await dbContext.CourseOfferingRosterSnapshotsSet.SingleAsync();

        dbContext.ExamHandoffLogsSet.Add(new ExamHandoffLog
        {
            CourseOfferingId = courseOffering.Id,
            RosterSnapshotId = snapshot.Id,
            Status = ExamHandoffStatus.Failed,
            SentAtUtc = new DateTime(2026, 3, 24, 1, 0, 0, DateTimeKind.Utc),
            ErrorMessage = "Timeout",
            CreatedBy = "seed"
        });
        await dbContext.SaveChangesAsync();

        var reopened = await service.ReopenAsync(new ReopenCourseOfferingRosterCommand
        {
            CourseOfferingId = courseOffering.Id,
            Reason = "Fix mistaken finalize"
        });

        Assert.False(reopened.IsFinalized);
        Assert.Equal(0, reopened.ItemCount);
        Assert.Empty(reopened.Items);

        var refreshedOffering = await dbContext.CourseOfferingsSet.SingleAsync(x => x.Id == courseOffering.Id);
        Assert.False(refreshedOffering.IsRosterFinalized);
        Assert.Null(refreshedOffering.RosterFinalizedAtUtc);
        Assert.Empty(await dbContext.CourseOfferingRosterSnapshotsSet.ToListAsync());
        Assert.Empty(await dbContext.ExamHandoffLogsSet.ToListAsync());

        var audit = await dbContext.AuditLogs.SingleAsync(x => x.Action == "courseofferingroster.reopen");
        Assert.Equal(nameof(CourseOfferingRosterSnapshot), audit.EntityType);
    }

    [Fact]
    public async Task ReopenAsync_ShouldFail_WhenCurrentUserIsNotAdmin()
    {
        await using var dbContext = CreateDbContext();
        var courseOffering = await SeedFinalizedRosterAsync(dbContext);
        var service = CreateRosterService(dbContext, username: "staff.ops");

        var exception = await Assert.ThrowsAsync<AuthException>(() => service.ReopenAsync(new ReopenCourseOfferingRosterCommand
        {
            CourseOfferingId = courseOffering.Id
        }));

        Assert.Equal("Only admin can reopen a finalized roster.", exception.Message);
    }

    [Fact]
    public async Task ReopenAsync_ShouldFail_WhenAttendanceExists()
    {
        await using var dbContext = CreateDbContext();
        var courseOffering = await SeedFinalizedRosterAsync(dbContext);
        var snapshot = await dbContext.CourseOfferingRosterSnapshotsSet.SingleAsync();
        dbContext.AttendanceSessionsSet.Add(new AttendanceSession
        {
            CourseOfferingId = courseOffering.Id,
            CourseOfferingRosterSnapshotId = snapshot.Id,
            SessionDate = new DateTime(2026, 3, 25, 0, 0, 0, DateTimeKind.Utc),
            SessionNo = 1,
            CreatedBy = "seed"
        });
        await dbContext.SaveChangesAsync();

        var service = CreateRosterService(dbContext, username: "admin");
        var exception = await Assert.ThrowsAsync<AuthException>(() => service.ReopenAsync(new ReopenCourseOfferingRosterCommand
        {
            CourseOfferingId = courseOffering.Id
        }));

        Assert.Equal("Roster cannot be reopened because attendance sessions already exist.", exception.Message);
    }

    [Fact]
    public async Task ReopenAsync_ShouldFail_WhenExamHandoffAlreadySucceeded()
    {
        await using var dbContext = CreateDbContext();
        var courseOffering = await SeedFinalizedRosterAsync(dbContext);
        var snapshot = await dbContext.CourseOfferingRosterSnapshotsSet.SingleAsync();
        dbContext.ExamHandoffLogsSet.Add(new ExamHandoffLog
        {
            CourseOfferingId = courseOffering.Id,
            RosterSnapshotId = snapshot.Id,
            Status = ExamHandoffStatus.Success,
            SentAtUtc = new DateTime(2026, 3, 25, 0, 0, 0, DateTimeKind.Utc),
            ResponseCode = 200,
            CreatedBy = "seed"
        });
        await dbContext.SaveChangesAsync();

        var service = CreateRosterService(dbContext, username: "admin");
        var exception = await Assert.ThrowsAsync<AuthException>(() => service.ReopenAsync(new ReopenCourseOfferingRosterCommand
        {
            CourseOfferingId = courseOffering.Id
        }));

        Assert.Equal("Roster cannot be reopened because exam handoff already succeeded.", exception.Message);
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private static async Task<Faculty> SeedFacultyAsync(AppDbContext dbContext)
    {
        var faculty = new Faculty
        {
            Code = "CNTT",
            Name = "Cong nghe thong tin",
            Status = FacultyStatus.Active,
            CreatedBy = "seed"
        };

        dbContext.FacultiesSet.Add(faculty);
        await dbContext.SaveChangesAsync();
        return faculty;
    }

    private static async Task<StudentClass> SeedStudentClassAsync(AppDbContext dbContext)
    {
        var faculty = await SeedFacultyAsync(dbContext);
        var studentClass = new StudentClass
        {
            Code = $"DHKTPM{dbContext.StudentClassesSet.Count() + 1}",
            Name = "KTPM",
            FacultyId = faculty.Id,
            IntakeYear = 2024,
            Status = StudentClassStatus.Active,
            CreatedBy = "seed"
        };

        dbContext.StudentClassesSet.Add(studentClass);
        await dbContext.SaveChangesAsync();
        return studentClass;
    }

    private static async Task<StudentProfile> SeedStudentProfileAsync(AppDbContext dbContext, string studentCode)
    {
        var studentClass = await SeedStudentClassAsync(dbContext);
        var studentProfile = new StudentProfile
        {
            StudentCode = studentCode,
            FullName = $"Sinh vien {studentCode}",
            StudentClassId = studentClass.Id,
            Status = StudentProfileStatus.Active,
            CreatedBy = "seed"
        };

        dbContext.StudentProfilesSet.Add(studentProfile);
        await dbContext.SaveChangesAsync();
        return studentProfile;
    }

    private static async Task<Course> SeedCourseAsync(AppDbContext dbContext)
    {
        var course = new Course
        {
            Code = $"CS{100 + dbContext.CoursesSet.Count()}",
            Name = "Nhap mon lap trinh",
            Credits = 3,
            Status = CourseStatus.Active,
            CreatedBy = "seed"
        };

        dbContext.CoursesSet.Add(course);
        await dbContext.SaveChangesAsync();
        return course;
    }

    private static async Task<Semester> SeedSemesterAsync(AppDbContext dbContext)
    {
        var semester = new Semester
        {
            Code = $"HK1-2526-{dbContext.SemestersSet.Count() + 1}",
            Name = "Hoc ky 1",
            AcademicYear = "2025-2026",
            TermNo = 1,
            StartDate = new DateTime(2025, 9, 1),
            EndDate = new DateTime(2026, 1, 15),
            Status = SemesterStatus.Active,
            CreatedBy = "seed"
        };

        dbContext.SemestersSet.Add(semester);
        await dbContext.SaveChangesAsync();
        return semester;
    }

    private static async Task<CourseOffering> SeedCourseOfferingAsync(AppDbContext dbContext, string code, int capacity)
    {
        var course = await SeedCourseAsync(dbContext);
        var semester = await SeedSemesterAsync(dbContext);
        var offering = new CourseOffering
        {
            Code = code,
            CourseId = course.Id,
            SemesterId = semester.Id,
            DisplayName = $"Offering {code}",
            Capacity = capacity,
            Status = CourseOfferingStatus.Active,
            CreatedBy = "seed"
        };

        dbContext.CourseOfferingsSet.Add(offering);
        await dbContext.SaveChangesAsync();
        return offering;
    }

    private static async Task<CourseOffering> SeedFinalizedRosterAsync(AppDbContext dbContext)
    {
        var studentProfile = await SeedStudentProfileAsync(dbContext, "SV900");
        var courseOffering = await SeedCourseOfferingAsync(dbContext, "CS999-HK1-01", 2);
        dbContext.EnrollmentsSet.Add(new Enrollment
        {
            StudentProfileId = studentProfile.Id,
            CourseOfferingId = courseOffering.Id,
            Status = EnrollmentStatus.Enrolled,
            EnrolledAtUtc = new DateTime(2026, 3, 24, 0, 0, 0, DateTimeKind.Utc),
            CreatedBy = "seed"
        });
        await dbContext.SaveChangesAsync();

        var service = CreateRosterService(dbContext, username: "admin");
        await service.FinalizeAsync(new FinalizeCourseOfferingRosterCommand
        {
            CourseOfferingId = courseOffering.Id
        });

        return courseOffering;
    }

    private static CourseOfferingRosterService CreateRosterService(AppDbContext dbContext, string username = "roster-admin")
    {
        return new CourseOfferingRosterService(
            dbContext,
            new AuditService(dbContext, new FakeClientContextAccessor()),
            new FakeCurrentUser(username),
            new FakeDateTimeProvider(),
            new FakeExamHandoffService());
    }

    private sealed class FakeCurrentUser : ICurrentUser
    {
        public FakeCurrentUser(string username)
        {
            UsernameValue = username;
        }

        public Guid? UserId => Guid.Parse("99999999-9999-9999-9999-999999999999");
        public string? Username => UsernameValue;
        public Guid? SessionId => null;
        public bool IsAuthenticated => true;
        public IReadOnlyCollection<string> Permissions => [PermissionConstants.CourseOfferingRosters.View, PermissionConstants.CourseOfferingRosters.Finalize, PermissionConstants.CourseOfferingRosters.Reopen];
        private string UsernameValue { get; }
    }

    private sealed class FakeDateTimeProvider : IDateTimeProvider
    {
        public DateTime UtcNow => new(2026, 3, 24, 0, 0, 0, DateTimeKind.Utc);
    }

    private sealed class FakeClientContextAccessor : IClientContextAccessor
    {
        public string? IpAddress => "127.0.0.1";
        public string? UserAgent => "RosterTests";
        public string ClientType => "Tests";
    }

    private sealed class FakeExamHandoffService : IExamHandoffService
    {
        public Task<ExamHandoffLogModel?> GetLatestStatusAsync(Guid courseOfferingId, CancellationToken cancellationToken = default)
            => Task.FromResult<ExamHandoffLogModel?>(null);

        public Task HandoffAsync(CourseOfferingRosterSnapshot snapshot, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task RetryHandoffAsync(Guid courseOfferingId, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }
}
