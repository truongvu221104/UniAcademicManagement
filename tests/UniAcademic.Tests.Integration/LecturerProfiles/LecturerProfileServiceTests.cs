using Microsoft.EntityFrameworkCore;
using UniAcademic.Application.Abstractions.Common;
using UniAcademic.Application.Common;
using UniAcademic.Application.Features.LecturerProfiles;
using UniAcademic.Application.Models.LecturerProfiles;
using UniAcademic.Application.Security;
using UniAcademic.Domain.Entities.Academic;
using UniAcademic.Domain.Enums;
using UniAcademic.Infrastructure.Persistence;
using UniAcademic.Infrastructure.Services.Auth;
using Xunit;

namespace UniAcademic.Tests.Integration.LecturerProfiles;

public sealed class LecturerProfileServiceTests
{
    [Fact]
    public async Task CreateAsync_ShouldCreateLecturerProfile_AndWriteAudit()
    {
        await using var dbContext = CreateDbContext();
        var faculty = await SeedFacultyAsync(dbContext);
        var service = CreateLecturerProfileService(dbContext);

        var result = await service.CreateAsync(new CreateLecturerProfileCommand
        {
            Code = "  GV001  ",
            FullName = "  Nguyen Van B  ",
            Email = " lecturer@example.com ",
            PhoneNumber = " 0123456789 ",
            FacultyId = faculty.Id,
            IsActive = true,
            Note = "  Chu nhiem bo mon  "
        });

        Assert.Equal("GV001", result.Code);
        Assert.Equal("Nguyen Van B", result.FullName);
        Assert.Equal("lecturer@example.com", result.Email);
        Assert.Equal("0123456789", result.PhoneNumber);
        Assert.Equal("Chu nhiem bo mon", result.Note);

        var lecturer = await dbContext.LecturerProfilesSet.SingleAsync();
        Assert.Equal("GV001", lecturer.Code);

        var audit = await dbContext.AuditLogs.SingleAsync(x => x.Action == "lecturerprofile.create");
        Assert.Equal(nameof(LecturerProfile), audit.EntityType);
        Assert.Equal(lecturer.Id.ToString(), audit.EntityId);
    }

    [Fact]
    public async Task CreateAsync_ShouldFail_WhenCodeAlreadyExists()
    {
        await using var dbContext = CreateDbContext();
        var faculty = await SeedFacultyAsync(dbContext);
        dbContext.LecturerProfilesSet.Add(new LecturerProfile
        {
            Code = "GV001",
            FullName = "Existing",
            FacultyId = faculty.Id,
            IsActive = true,
            CreatedBy = "seed"
        });
        await dbContext.SaveChangesAsync();

        var service = CreateLecturerProfileService(dbContext);
        var exception = await Assert.ThrowsAsync<AuthException>(() => service.CreateAsync(new CreateLecturerProfileCommand
        {
            Code = " GV001 ",
            FullName = "Another",
            FacultyId = faculty.Id,
            IsActive = true
        }));

        Assert.Equal("Lecturer profile code already exists.", exception.Message);
    }

    [Fact]
    public async Task CreateAsync_ShouldFail_WhenFacultyDoesNotExist()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateLecturerProfileService(dbContext);

        var exception = await Assert.ThrowsAsync<AuthException>(() => service.CreateAsync(new CreateLecturerProfileCommand
        {
            Code = "GV001",
            FullName = "Lecturer",
            FacultyId = Guid.NewGuid()
        }));

        Assert.Equal("Faculty was not found.", exception.Message);
    }

    [Fact]
    public async Task CreateAsync_ShouldFail_WhenFacultyIsSoftDeleted()
    {
        await using var dbContext = CreateDbContext();
        var faculty = await SeedFacultyAsync(dbContext);
        faculty.IsDeleted = true;
        faculty.DeletedAtUtc = new DateTime(2026, 3, 25, 0, 0, 0, DateTimeKind.Utc);
        faculty.DeletedBy = "seed";
        await dbContext.SaveChangesAsync();

        var service = CreateLecturerProfileService(dbContext);
        var exception = await Assert.ThrowsAsync<AuthException>(() => service.CreateAsync(new CreateLecturerProfileCommand
        {
            Code = "GV001",
            FullName = "Lecturer",
            FacultyId = faculty.Id
        }));

        Assert.Equal("Faculty was not found.", exception.Message);
    }

    [Fact]
    public async Task CreateAsync_ShouldFail_WhenEmailIsInvalid()
    {
        await using var dbContext = CreateDbContext();
        var faculty = await SeedFacultyAsync(dbContext);
        var service = CreateLecturerProfileService(dbContext);

        var exception = await Assert.ThrowsAsync<AuthException>(() => service.CreateAsync(new CreateLecturerProfileCommand
        {
            Code = "GV001",
            FullName = "Lecturer",
            FacultyId = faculty.Id,
            Email = "invalid"
        }));

        Assert.Equal("Lecturer profile email is invalid.", exception.Message);
    }

    [Fact]
    public async Task DeleteAsync_ShouldSoftDeleteLecturerProfile()
    {
        await using var dbContext = CreateDbContext();
        var lecturer = await SeedLecturerProfileAsync(dbContext, "GV001");
        var service = CreateLecturerProfileService(dbContext);

        await service.DeleteAsync(new DeleteLecturerProfileCommand { Id = lecturer.Id });

        var deleted = await dbContext.LecturerProfilesSet.IgnoreQueryFilters().SingleAsync(x => x.Id == lecturer.Id);
        Assert.True(deleted.IsDeleted);
        Assert.False(deleted.IsActive);
        Assert.NotNull(deleted.DeletedAtUtc);
        Assert.False(await dbContext.LecturerProfilesSet.AnyAsync(x => x.Id == lecturer.Id));

        var audit = await dbContext.AuditLogs.SingleAsync(x => x.Action == "lecturerprofile.delete");
        Assert.Equal(lecturer.Id.ToString(), audit.EntityId);
    }

    [Fact]
    public async Task GetListAsync_ShouldExcludeDeletedRows()
    {
        await using var dbContext = CreateDbContext();
        var faculty = await SeedFacultyAsync(dbContext);
        dbContext.LecturerProfilesSet.AddRange(
            new LecturerProfile
            {
                Code = "GV001",
                FullName = "Lecturer 1",
                FacultyId = faculty.Id,
                IsActive = true,
                CreatedBy = "seed"
            },
            new LecturerProfile
            {
                Code = "GV002",
                FullName = "Lecturer 2",
                FacultyId = faculty.Id,
                IsActive = false,
                IsDeleted = true,
                DeletedAtUtc = new DateTime(2026, 3, 25, 0, 0, 0, DateTimeKind.Utc),
                DeletedBy = "seed",
                CreatedBy = "seed"
            });
        await dbContext.SaveChangesAsync();

        var service = CreateLecturerProfileService(dbContext);
        var result = await service.GetListAsync(new GetLecturerProfilesQuery());

        Assert.Single(result);
        Assert.Equal("GV001", result.Single().Code);
    }

    [Fact]
    public async Task DeleteAsync_ShouldFail_WhenLecturerStillHasAssignment()
    {
        await using var dbContext = CreateDbContext();
        var lecturer = await SeedLecturerProfileAsync(dbContext, "GV001");
        var courseOffering = await SeedCourseOfferingAsync(dbContext);
        dbContext.LecturerAssignmentsSet.Add(new LecturerAssignment
        {
            LecturerProfileId = lecturer.Id,
            CourseOfferingId = courseOffering.Id,
            IsPrimary = true,
            AssignedAtUtc = new DateTime(2026, 3, 25, 0, 0, 0, DateTimeKind.Utc),
            CreatedBy = "seed"
        });
        await dbContext.SaveChangesAsync();

        var service = CreateLecturerProfileService(dbContext);
        var exception = await Assert.ThrowsAsync<AuthException>(() => service.DeleteAsync(new DeleteLecturerProfileCommand
        {
            Id = lecturer.Id
        }));

        Assert.Equal("Lecturer profile still has assignments.", exception.Message);
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

    private static async Task<CourseOffering> SeedCourseOfferingAsync(AppDbContext dbContext)
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
            Code = $"CS101-{dbContext.CourseOfferingsSet.Count() + 1}",
            CourseId = course.Id,
            SemesterId = semester.Id,
            DisplayName = "Offering",
            Capacity = 50,
            Status = CourseOfferingStatus.Active,
            CreatedBy = "seed"
        };
        dbContext.CourseOfferingsSet.Add(courseOffering);
        await dbContext.SaveChangesAsync();
        return courseOffering;
    }

    private static LecturerProfileService CreateLecturerProfileService(AppDbContext dbContext)
    {
        return new LecturerProfileService(
            dbContext,
            new AuditService(dbContext, new FakeClientContextAccessor()),
            new FakeCurrentUser(),
            new FakeDateTimeProvider());
    }

    private sealed class FakeCurrentUser : ICurrentUser
    {
        public Guid? UserId => Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        public string? Username => "lecturerprofile-admin";
        public Guid? SessionId => null;
        public bool IsAuthenticated => true;
        public IReadOnlyCollection<string> Permissions => [PermissionConstants.LecturerProfiles.View, PermissionConstants.LecturerProfiles.Create, PermissionConstants.LecturerProfiles.Edit, PermissionConstants.LecturerProfiles.Delete];
    }

    private sealed class FakeDateTimeProvider : IDateTimeProvider
    {
        public DateTime UtcNow => new(2026, 3, 25, 0, 0, 0, DateTimeKind.Utc);
    }

    private sealed class FakeClientContextAccessor : IClientContextAccessor
    {
        public string? IpAddress => "127.0.0.1";
        public string? UserAgent => "LecturerProfileTests";
        public string ClientType => "Tests";
    }
}
