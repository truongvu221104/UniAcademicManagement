using Microsoft.EntityFrameworkCore;
using UniAcademic.Application.Abstractions.Common;
using UniAcademic.Application.Common;
using UniAcademic.Application.Features.Attendance;
using UniAcademic.Application.Models.Attendance;
using UniAcademic.Application.Security;
using UniAcademic.Domain.Entities.Academic;
using UniAcademic.Domain.Enums;
using UniAcademic.Infrastructure.Persistence;
using UniAcademic.Infrastructure.Services.Auth;
using Xunit;

namespace UniAcademic.Tests.Integration.Attendance;

public sealed class AttendanceServiceTests
{
    [Fact]
    public async Task CreateSessionAsync_ShouldCreateAttendanceSession_AndWriteAudit()
    {
        await using var dbContext = CreateDbContext();
        var courseOffering = await SeedCourseOfferingAsync(dbContext, "CO-01");
        var rosterSnapshot = await SeedFinalizedRosterAsync(dbContext, courseOffering, ["SV001", "SV002"]);
        var service = CreateAttendanceService(dbContext);

        var result = await service.CreateSessionAsync(new CreateAttendanceSessionCommand
        {
            CourseOfferingId = courseOffering.Id,
            SessionDate = new DateTime(2026, 3, 25),
            SessionNo = 1,
            Title = "  Buoi 1  ",
            Note = "  Diem danh buoi dau  "
        });

        Assert.Equal(courseOffering.Id, result.CourseOfferingId);
        Assert.Equal(rosterSnapshot.Id, result.CourseOfferingRosterSnapshotId);
        Assert.Equal(2, result.RecordCount);
        Assert.All(result.Records, x => Assert.Equal(AttendanceStatus.Unmarked, x.Status));
        Assert.Equal("Buoi 1", result.Title);
        Assert.Equal("Diem danh buoi dau", result.Note);

        var session = await dbContext.AttendanceSessionsSet.Include(x => x.Records).SingleAsync();
        Assert.Equal(2, session.Records.Count);

        var audit = await dbContext.AuditLogs.SingleAsync(x => x.Action == "attendance.session.create");
        Assert.Equal(nameof(AttendanceSession), audit.EntityType);
        Assert.Equal(session.Id.ToString(), audit.EntityId);
    }

    [Fact]
    public async Task CreateSessionAsync_ShouldFail_WhenCourseOfferingDoesNotExist()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateAttendanceService(dbContext);

        var exception = await Assert.ThrowsAsync<AuthException>(() => service.CreateSessionAsync(new CreateAttendanceSessionCommand
        {
            CourseOfferingId = Guid.NewGuid(),
            SessionDate = new DateTime(2026, 3, 25),
            SessionNo = 1
        }));

        Assert.Equal("Course offering was not found.", exception.Message);
    }

    [Fact]
    public async Task CreateSessionAsync_ShouldFail_WhenCourseOfferingIsSoftDeleted()
    {
        await using var dbContext = CreateDbContext();
        var courseOffering = await SeedCourseOfferingAsync(dbContext, "CO-01");
        courseOffering.IsDeleted = true;
        courseOffering.DeletedAtUtc = new DateTime(2026, 3, 24, 0, 0, 0, DateTimeKind.Utc);
        courseOffering.DeletedBy = "seed";
        await dbContext.SaveChangesAsync();
        var service = CreateAttendanceService(dbContext);

        var exception = await Assert.ThrowsAsync<AuthException>(() => service.CreateSessionAsync(new CreateAttendanceSessionCommand
        {
            CourseOfferingId = courseOffering.Id,
            SessionDate = new DateTime(2026, 3, 25),
            SessionNo = 1
        }));

        Assert.Equal("Course offering was not found.", exception.Message);
    }

    [Fact]
    public async Task CreateSessionAsync_ShouldFail_WhenRosterIsNotFinalized()
    {
        await using var dbContext = CreateDbContext();
        var courseOffering = await SeedCourseOfferingAsync(dbContext, "CO-01");
        var service = CreateAttendanceService(dbContext);

        var exception = await Assert.ThrowsAsync<AuthException>(() => service.CreateSessionAsync(new CreateAttendanceSessionCommand
        {
            CourseOfferingId = courseOffering.Id,
            SessionDate = new DateTime(2026, 3, 25),
            SessionNo = 1
        }));

        Assert.Equal("Course offering roster was not finalized.", exception.Message);
    }

    [Fact]
    public async Task CreateSessionAsync_ShouldFail_WhenRosterSnapshotIsMissing()
    {
        await using var dbContext = CreateDbContext();
        var courseOffering = await SeedCourseOfferingAsync(dbContext, "CO-01");
        courseOffering.IsRosterFinalized = true;
        courseOffering.RosterFinalizedAtUtc = new DateTime(2026, 3, 24, 0, 0, 0, DateTimeKind.Utc);
        await dbContext.SaveChangesAsync();
        var service = CreateAttendanceService(dbContext);

        var exception = await Assert.ThrowsAsync<AuthException>(() => service.CreateSessionAsync(new CreateAttendanceSessionCommand
        {
            CourseOfferingId = courseOffering.Id,
            SessionDate = new DateTime(2026, 3, 25),
            SessionNo = 1
        }));

        Assert.Equal("Course offering roster snapshot was not found.", exception.Message);
    }

    [Fact]
    public async Task CreateSessionAsync_ShouldGenerateRecordsForAllRosterItems()
    {
        await using var dbContext = CreateDbContext();
        var courseOffering = await SeedCourseOfferingAsync(dbContext, "CO-01");
        await SeedFinalizedRosterAsync(dbContext, courseOffering, ["SV001", "SV002", "SV003"]);
        var service = CreateAttendanceService(dbContext);

        var result = await service.CreateSessionAsync(new CreateAttendanceSessionCommand
        {
            CourseOfferingId = courseOffering.Id,
            SessionDate = new DateTime(2026, 3, 25),
            SessionNo = 1
        });

        Assert.Equal(3, result.RecordCount);
        Assert.Equal(3, await dbContext.AttendanceRecordsSet.CountAsync());
    }

    [Fact]
    public async Task CreateSessionAsync_ShouldFail_WhenSessionAlreadyExists()
    {
        await using var dbContext = CreateDbContext();
        var courseOffering = await SeedCourseOfferingAsync(dbContext, "CO-01");
        var rosterSnapshot = await SeedFinalizedRosterAsync(dbContext, courseOffering, ["SV001"]);
        dbContext.AttendanceSessionsSet.Add(new AttendanceSession
        {
            CourseOfferingId = courseOffering.Id,
            CourseOfferingRosterSnapshotId = rosterSnapshot.Id,
            SessionDate = new DateTime(2026, 3, 25),
            SessionNo = 1,
            CreatedBy = "seed"
        });
        await dbContext.SaveChangesAsync();
        var service = CreateAttendanceService(dbContext);

        var exception = await Assert.ThrowsAsync<AuthException>(() => service.CreateSessionAsync(new CreateAttendanceSessionCommand
        {
            CourseOfferingId = courseOffering.Id,
            SessionDate = new DateTime(2026, 3, 25),
            SessionNo = 1
        }));

        Assert.Equal("Attendance session already exists.", exception.Message);
    }

    [Fact]
    public async Task UpdateRecordsAsync_ShouldUpdateAttendanceRecords_AndWriteAudit()
    {
        await using var dbContext = CreateDbContext();
        var session = await SeedAttendanceSessionAsync(dbContext, "CO-01", ["SV001", "SV002"]);
        var service = CreateAttendanceService(dbContext);

        var result = await service.UpdateRecordsAsync(new UpdateAttendanceRecordsCommand
        {
            Id = session.Id,
            Records = session.Records.Select(x => new UpdateAttendanceRecordItemCommand
            {
                RosterItemId = x.RosterItemId,
                Status = x.RosterItem!.StudentCode == "SV001" ? AttendanceStatus.Present : AttendanceStatus.Late,
                Note = "  Da cap nhat  "
            }).ToList()
        });

        Assert.Equal(2, result.RecordCount);
        Assert.Contains(result.Records, x => x.StudentCode == "SV001" && x.Status == AttendanceStatus.Present);
        Assert.Contains(result.Records, x => x.StudentCode == "SV002" && x.Status == AttendanceStatus.Late);
        Assert.All(result.Records, x => Assert.Equal("Da cap nhat", x.Note));

        var audit = await dbContext.AuditLogs.SingleAsync(x => x.Action == "attendance.records.update");
        Assert.Equal(session.Id.ToString(), audit.EntityId);
    }

    [Fact]
    public async Task UpdateRecordsAsync_ShouldFail_WhenRosterItemDoesNotBelongToSession()
    {
        await using var dbContext = CreateDbContext();
        var sessionA = await SeedAttendanceSessionAsync(dbContext, "CO-01", ["SV001"]);
        var sessionB = await SeedAttendanceSessionAsync(dbContext, "CO-02", ["SV002"]);
        var foreignRosterItemId = sessionB.Records.Single().RosterItemId;
        var service = CreateAttendanceService(dbContext);

        var exception = await Assert.ThrowsAsync<AuthException>(() => service.UpdateRecordsAsync(new UpdateAttendanceRecordsCommand
        {
            Id = sessionA.Id,
            Records =
            [
                new UpdateAttendanceRecordItemCommand
                {
                    RosterItemId = foreignRosterItemId,
                    Status = AttendanceStatus.Absent
                }
            ]
        }));

        Assert.Equal("Attendance record does not belong to the session.", exception.Message);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnSessionWithRecords()
    {
        await using var dbContext = CreateDbContext();
        var session = await SeedAttendanceSessionAsync(dbContext, "CO-01", ["SV001", "SV002"]);
        var service = CreateAttendanceService(dbContext);

        var result = await service.GetByIdAsync(new GetAttendanceSessionByIdQuery
        {
            Id = session.Id
        });

        Assert.Equal(session.Id, result.Id);
        Assert.Equal(2, result.RecordCount);
        Assert.Contains(result.Records, x => x.StudentCode == "SV001");
        Assert.Contains(result.Records, x => x.StudentCode == "SV002");
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
            Code = $"CNTT{dbContext.FacultiesSet.Count() + 1}",
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

    private static async Task<CourseOffering> SeedCourseOfferingAsync(AppDbContext dbContext, string code)
    {
        var course = await SeedCourseAsync(dbContext);
        var semester = await SeedSemesterAsync(dbContext);
        var offering = new CourseOffering
        {
            Code = code,
            CourseId = course.Id,
            SemesterId = semester.Id,
            DisplayName = $"Offering {code}",
            Capacity = 50,
            Status = CourseOfferingStatus.Active,
            CreatedBy = "seed"
        };

        dbContext.CourseOfferingsSet.Add(offering);
        await dbContext.SaveChangesAsync();
        return offering;
    }

    private static async Task<CourseOfferingRosterSnapshot> SeedFinalizedRosterAsync(AppDbContext dbContext, CourseOffering courseOffering, IReadOnlyCollection<string> studentCodes)
    {
        var snapshot = new CourseOfferingRosterSnapshot
        {
            CourseOfferingId = courseOffering.Id,
            FinalizedAtUtc = new DateTime(2026, 3, 24, 0, 0, 0, DateTimeKind.Utc),
            FinalizedBy = "seed",
            ItemCount = studentCodes.Count,
            CreatedBy = "seed"
        };

        foreach (var studentCode in studentCodes)
        {
            var studentProfile = await SeedStudentProfileAsync(dbContext, studentCode);
            var enrollment = new Enrollment
            {
                StudentProfileId = studentProfile.Id,
                CourseOfferingId = courseOffering.Id,
                Status = EnrollmentStatus.Enrolled,
                EnrolledAtUtc = new DateTime(2026, 3, 20, 0, 0, 0, DateTimeKind.Utc),
                CreatedBy = "seed"
            };

            dbContext.EnrollmentsSet.Add(enrollment);
            await dbContext.SaveChangesAsync();

            snapshot.Items.Add(new CourseOfferingRosterItem
            {
                EnrollmentId = enrollment.Id,
                StudentProfileId = studentProfile.Id,
                StudentCode = studentProfile.StudentCode,
                StudentFullName = studentProfile.FullName,
                StudentClassName = studentProfile.StudentClass?.Name ?? string.Empty,
                CourseOfferingCode = courseOffering.Code,
                CourseCode = courseOffering.Course?.Code ?? string.Empty,
                CourseName = courseOffering.Course?.Name ?? string.Empty,
                SemesterName = courseOffering.Semester?.Name ?? string.Empty,
                CreatedBy = "seed"
            });
        }

        courseOffering.IsRosterFinalized = true;
        courseOffering.RosterFinalizedAtUtc = new DateTime(2026, 3, 24, 0, 0, 0, DateTimeKind.Utc);
        dbContext.CourseOfferingRosterSnapshotsSet.Add(snapshot);
        await dbContext.SaveChangesAsync();

        return snapshot;
    }

    private static async Task<AttendanceSession> SeedAttendanceSessionAsync(AppDbContext dbContext, string courseOfferingCode, IReadOnlyCollection<string> studentCodes)
    {
        var courseOffering = await SeedCourseOfferingAsync(dbContext, courseOfferingCode);
        var snapshot = await SeedFinalizedRosterAsync(dbContext, courseOffering, studentCodes);
        var session = new AttendanceSession
        {
            CourseOfferingId = courseOffering.Id,
            CourseOfferingRosterSnapshotId = snapshot.Id,
            SessionDate = new DateTime(2026, 3, 25),
            SessionNo = 1,
            Title = "Buoi 1",
            CreatedBy = "seed"
        };

        foreach (var item in snapshot.Items)
        {
            session.Records.Add(new AttendanceRecord
            {
                RosterItemId = item.Id,
                Status = AttendanceStatus.Unmarked,
                CreatedBy = "seed"
            });
        }

        dbContext.AttendanceSessionsSet.Add(session);
        await dbContext.SaveChangesAsync();
        return await dbContext.AttendanceSessionsSet
            .Include(x => x.Records)
                .ThenInclude(x => x.RosterItem)
            .SingleAsync(x => x.Id == session.Id);
    }

    private static AttendanceService CreateAttendanceService(AppDbContext dbContext)
    {
        return new AttendanceService(
            dbContext,
            new AuditService(dbContext, new FakeClientContextAccessor()),
            new FakeCurrentUser(),
            new FakeDateTimeProvider());
    }

    private sealed class FakeCurrentUser : ICurrentUser
    {
        public Guid? UserId => Guid.Parse("99999999-9999-9999-9999-999999999999");
        public string? Username => "attendance-admin";
        public Guid? SessionId => null;
        public bool IsAuthenticated => true;
        public IReadOnlyCollection<string> Permissions => [PermissionConstants.Attendance.View, PermissionConstants.Attendance.Create, PermissionConstants.Attendance.Edit];
    }

    private sealed class FakeDateTimeProvider : IDateTimeProvider
    {
        public DateTime UtcNow => new(2026, 3, 24, 0, 0, 0, DateTimeKind.Utc);
    }

    private sealed class FakeClientContextAccessor : IClientContextAccessor
    {
        public string? IpAddress => "127.0.0.1";
        public string? UserAgent => "AttendanceTests";
        public string ClientType => "Tests";
    }
}
