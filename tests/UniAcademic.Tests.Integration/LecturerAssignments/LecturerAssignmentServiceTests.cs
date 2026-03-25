using Microsoft.EntityFrameworkCore;
using UniAcademic.Application.Abstractions.Common;
using UniAcademic.Application.Common;
using UniAcademic.Application.Features.LecturerAssignments;
using UniAcademic.Application.Models.LecturerAssignments;
using UniAcademic.Application.Security;
using UniAcademic.Domain.Entities.Academic;
using UniAcademic.Domain.Enums;
using UniAcademic.Infrastructure.Persistence;
using UniAcademic.Infrastructure.Services.Auth;
using Xunit;

namespace UniAcademic.Tests.Integration.LecturerAssignments;

public sealed class LecturerAssignmentServiceTests
{
    [Fact]
    public async Task AssignAsync_ShouldCreateAssignment_AndWriteAudit()
    {
        await using var dbContext = CreateDbContext();
        var lecturer = await SeedLecturerProfileAsync(dbContext, "GV001");
        var courseOffering = await SeedCourseOfferingAsync(dbContext, "CS101-01");
        var service = CreateLecturerAssignmentService(dbContext);

        var result = await service.AssignAsync(new AssignLecturerCommand
        {
            CourseOfferingId = courseOffering.Id,
            LecturerProfileId = lecturer.Id,
            IsPrimary = true
        });

        Assert.Equal(courseOffering.Id, result.CourseOfferingId);
        Assert.Equal(lecturer.Id, result.LecturerProfileId);
        Assert.True(result.IsPrimary);

        var assignment = await dbContext.LecturerAssignmentsSet.SingleAsync();
        Assert.True(assignment.IsPrimary);
        Assert.NotEqual(default, assignment.AssignedAtUtc);

        var audit = await dbContext.AuditLogs.SingleAsync(x => x.Action == "lecturerassignment.assign");
        Assert.Equal(nameof(LecturerAssignment), audit.EntityType);
        Assert.Equal(assignment.Id.ToString(), audit.EntityId);
    }

    [Fact]
    public async Task AssignAsync_ShouldFail_WhenCourseOfferingDoesNotExist()
    {
        await using var dbContext = CreateDbContext();
        var lecturer = await SeedLecturerProfileAsync(dbContext, "GV001");
        var service = CreateLecturerAssignmentService(dbContext);

        var exception = await Assert.ThrowsAsync<AuthException>(() => service.AssignAsync(new AssignLecturerCommand
        {
            CourseOfferingId = Guid.NewGuid(),
            LecturerProfileId = lecturer.Id
        }));

        Assert.Equal("Course offering was not found.", exception.Message);
    }

    [Fact]
    public async Task AssignAsync_ShouldFail_WhenCourseOfferingIsSoftDeleted()
    {
        await using var dbContext = CreateDbContext();
        var lecturer = await SeedLecturerProfileAsync(dbContext, "GV001");
        var courseOffering = await SeedCourseOfferingAsync(dbContext, "CS101-01");
        courseOffering.IsDeleted = true;
        courseOffering.DeletedAtUtc = new DateTime(2026, 3, 25, 0, 0, 0, DateTimeKind.Utc);
        courseOffering.DeletedBy = "seed";
        await dbContext.SaveChangesAsync();

        var service = CreateLecturerAssignmentService(dbContext);
        var exception = await Assert.ThrowsAsync<AuthException>(() => service.AssignAsync(new AssignLecturerCommand
        {
            CourseOfferingId = courseOffering.Id,
            LecturerProfileId = lecturer.Id
        }));

        Assert.Equal("Course offering was not found.", exception.Message);
    }

    [Fact]
    public async Task AssignAsync_ShouldFail_WhenLecturerDoesNotExist()
    {
        await using var dbContext = CreateDbContext();
        var courseOffering = await SeedCourseOfferingAsync(dbContext, "CS101-01");
        var service = CreateLecturerAssignmentService(dbContext);

        var exception = await Assert.ThrowsAsync<AuthException>(() => service.AssignAsync(new AssignLecturerCommand
        {
            CourseOfferingId = courseOffering.Id,
            LecturerProfileId = Guid.NewGuid()
        }));

        Assert.Equal("Lecturer profile was not found.", exception.Message);
    }

    [Fact]
    public async Task AssignAsync_ShouldFail_WhenLecturerIsSoftDeleted()
    {
        await using var dbContext = CreateDbContext();
        var lecturer = await SeedLecturerProfileAsync(dbContext, "GV001");
        var courseOffering = await SeedCourseOfferingAsync(dbContext, "CS101-01");
        lecturer.IsDeleted = true;
        lecturer.DeletedAtUtc = new DateTime(2026, 3, 25, 0, 0, 0, DateTimeKind.Utc);
        lecturer.DeletedBy = "seed";
        await dbContext.SaveChangesAsync();

        var service = CreateLecturerAssignmentService(dbContext);
        var exception = await Assert.ThrowsAsync<AuthException>(() => service.AssignAsync(new AssignLecturerCommand
        {
            CourseOfferingId = courseOffering.Id,
            LecturerProfileId = lecturer.Id
        }));

        Assert.Equal("Lecturer profile was not found.", exception.Message);
    }

    [Fact]
    public async Task AssignAsync_ShouldFail_WhenLecturerIsInactive()
    {
        await using var dbContext = CreateDbContext();
        var lecturer = await SeedLecturerProfileAsync(dbContext, "GV001");
        var courseOffering = await SeedCourseOfferingAsync(dbContext, "CS101-01");
        lecturer.IsActive = false;
        await dbContext.SaveChangesAsync();

        var service = CreateLecturerAssignmentService(dbContext);
        var exception = await Assert.ThrowsAsync<AuthException>(() => service.AssignAsync(new AssignLecturerCommand
        {
            CourseOfferingId = courseOffering.Id,
            LecturerProfileId = lecturer.Id
        }));

        Assert.Equal("Lecturer profile is inactive.", exception.Message);
    }

    [Fact]
    public async Task AssignAsync_ShouldFail_WhenAssignmentAlreadyExists()
    {
        await using var dbContext = CreateDbContext();
        var lecturer = await SeedLecturerProfileAsync(dbContext, "GV001");
        var courseOffering = await SeedCourseOfferingAsync(dbContext, "CS101-01");
        dbContext.LecturerAssignmentsSet.Add(new LecturerAssignment
        {
            CourseOfferingId = courseOffering.Id,
            LecturerProfileId = lecturer.Id,
            IsPrimary = false,
            AssignedAtUtc = new DateTime(2026, 3, 25, 0, 0, 0, DateTimeKind.Utc),
            CreatedBy = "seed"
        });
        await dbContext.SaveChangesAsync();

        var service = CreateLecturerAssignmentService(dbContext);
        var exception = await Assert.ThrowsAsync<AuthException>(() => service.AssignAsync(new AssignLecturerCommand
        {
            CourseOfferingId = courseOffering.Id,
            LecturerProfileId = lecturer.Id
        }));

        Assert.Equal("Lecturer assignment already exists.", exception.Message);
    }

    [Fact]
    public async Task AssignAsync_ShouldAllowPrimaryAssignment()
    {
        await using var dbContext = CreateDbContext();
        var lecturer = await SeedLecturerProfileAsync(dbContext, "GV001");
        var courseOffering = await SeedCourseOfferingAsync(dbContext, "CS101-01");
        var service = CreateLecturerAssignmentService(dbContext);

        var result = await service.AssignAsync(new AssignLecturerCommand
        {
            CourseOfferingId = courseOffering.Id,
            LecturerProfileId = lecturer.Id,
            IsPrimary = true
        });

        Assert.True(result.IsPrimary);
    }

    [Fact]
    public async Task AssignAsync_ShouldFail_WhenPrimaryLecturerAlreadyExists()
    {
        await using var dbContext = CreateDbContext();
        var lecturer1 = await SeedLecturerProfileAsync(dbContext, "GV001");
        var lecturer2 = await SeedLecturerProfileAsync(dbContext, "GV002");
        var courseOffering = await SeedCourseOfferingAsync(dbContext, "CS101-01");
        dbContext.LecturerAssignmentsSet.Add(new LecturerAssignment
        {
            CourseOfferingId = courseOffering.Id,
            LecturerProfileId = lecturer1.Id,
            IsPrimary = true,
            AssignedAtUtc = new DateTime(2026, 3, 25, 0, 0, 0, DateTimeKind.Utc),
            CreatedBy = "seed"
        });
        await dbContext.SaveChangesAsync();

        var service = CreateLecturerAssignmentService(dbContext);
        var exception = await Assert.ThrowsAsync<AuthException>(() => service.AssignAsync(new AssignLecturerCommand
        {
            CourseOfferingId = courseOffering.Id,
            LecturerProfileId = lecturer2.Id,
            IsPrimary = true
        }));

        Assert.Equal("Course offering already has a primary lecturer.", exception.Message);
    }

    [Fact]
    public async Task UnassignAsync_ShouldRemoveAssignment_AndWriteAudit()
    {
        await using var dbContext = CreateDbContext();
        var lecturer = await SeedLecturerProfileAsync(dbContext, "GV001");
        var courseOffering = await SeedCourseOfferingAsync(dbContext, "CS101-01");
        var assignment = new LecturerAssignment
        {
            CourseOfferingId = courseOffering.Id,
            LecturerProfileId = lecturer.Id,
            IsPrimary = true,
            AssignedAtUtc = new DateTime(2026, 3, 25, 0, 0, 0, DateTimeKind.Utc),
            CreatedBy = "seed"
        };
        dbContext.LecturerAssignmentsSet.Add(assignment);
        await dbContext.SaveChangesAsync();

        var service = CreateLecturerAssignmentService(dbContext);
        await service.UnassignAsync(new UnassignLecturerCommand { Id = assignment.Id });

        Assert.False(await dbContext.LecturerAssignmentsSet.AnyAsync());
        var audit = await dbContext.AuditLogs.SingleAsync(x => x.Action == "lecturerassignment.unassign");
        Assert.Equal(assignment.Id.ToString(), audit.EntityId);
    }

    [Fact]
    public async Task GetListAsync_ShouldReturnAssignmentsForCourseOffering()
    {
        await using var dbContext = CreateDbContext();
        var lecturer1 = await SeedLecturerProfileAsync(dbContext, "GV001");
        var lecturer2 = await SeedLecturerProfileAsync(dbContext, "GV002");
        var courseOffering1 = await SeedCourseOfferingAsync(dbContext, "CS101-01");
        var courseOffering2 = await SeedCourseOfferingAsync(dbContext, "CS102-01");
        dbContext.LecturerAssignmentsSet.AddRange(
            new LecturerAssignment
            {
                CourseOfferingId = courseOffering1.Id,
                LecturerProfileId = lecturer1.Id,
                IsPrimary = true,
                AssignedAtUtc = new DateTime(2026, 3, 25, 0, 0, 0, DateTimeKind.Utc),
                CreatedBy = "seed"
            },
            new LecturerAssignment
            {
                CourseOfferingId = courseOffering2.Id,
                LecturerProfileId = lecturer2.Id,
                IsPrimary = false,
                AssignedAtUtc = new DateTime(2026, 3, 25, 0, 0, 0, DateTimeKind.Utc),
                CreatedBy = "seed"
            });
        await dbContext.SaveChangesAsync();

        var service = CreateLecturerAssignmentService(dbContext);
        var result = await service.GetListAsync(new GetLecturerAssignmentsQuery
        {
            CourseOfferingId = courseOffering1.Id
        });

        Assert.Single(result);
        Assert.Equal("GV001", result.Single().LecturerCode);
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

    private static async Task<LecturerProfile> SeedLecturerProfileAsync(AppDbContext dbContext, string code)
    {
        var faculty = await SeedFacultyAsync(dbContext);
        var lecturer = new LecturerProfile
        {
            Code = code,
            FullName = $"Lecturer {code}",
            FacultyId = faculty.Id,
            IsActive = true,
            CreatedBy = "seed"
        };
        dbContext.LecturerProfilesSet.Add(lecturer);
        await dbContext.SaveChangesAsync();
        return lecturer;
    }

    private static async Task<CourseOffering> SeedCourseOfferingAsync(AppDbContext dbContext, string code)
    {
        var course = new Course
        {
            Code = $"CS{100 + dbContext.CoursesSet.Count()}",
            Name = "Lap trinh",
            Credits = 3,
            Status = CourseStatus.Active,
            CreatedBy = "seed"
        };
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
        dbContext.CoursesSet.Add(course);
        dbContext.SemestersSet.Add(semester);
        await dbContext.SaveChangesAsync();

        var courseOffering = new CourseOffering
        {
            Code = code,
            CourseId = course.Id,
            SemesterId = semester.Id,
            DisplayName = $"Offering {code}",
            Capacity = 50,
            Status = CourseOfferingStatus.Active,
            CreatedBy = "seed"
        };
        dbContext.CourseOfferingsSet.Add(courseOffering);
        await dbContext.SaveChangesAsync();
        return courseOffering;
    }

    private static LecturerAssignmentService CreateLecturerAssignmentService(AppDbContext dbContext)
    {
        return new LecturerAssignmentService(
            dbContext,
            new AuditService(dbContext, new FakeClientContextAccessor()),
            new FakeCurrentUser(),
            new FakeDateTimeProvider());
    }

    private sealed class FakeCurrentUser : ICurrentUser
    {
        public Guid? UserId => Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        public string? Username => "lecturerassignment-admin";
        public Guid? SessionId => null;
        public bool IsAuthenticated => true;
        public IReadOnlyCollection<string> Permissions => [PermissionConstants.LecturerAssignments.View, PermissionConstants.LecturerAssignments.Assign, PermissionConstants.LecturerAssignments.Unassign];
    }

    private sealed class FakeDateTimeProvider : IDateTimeProvider
    {
        public DateTime UtcNow => new(2026, 3, 25, 0, 0, 0, DateTimeKind.Utc);
    }

    private sealed class FakeClientContextAccessor : IClientContextAccessor
    {
        public string? IpAddress => "127.0.0.1";
        public string? UserAgent => "LecturerAssignmentTests";
        public string ClientType => "Tests";
    }
}
