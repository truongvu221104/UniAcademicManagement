using Microsoft.EntityFrameworkCore;
using UniAcademic.Application.Abstractions.Common;
using UniAcademic.Application.Common;
using UniAcademic.Application.Features.Courses;
using UniAcademic.Application.Models.Courses;
using UniAcademic.Application.Security;
using UniAcademic.Domain.Entities.Academic;
using UniAcademic.Domain.Enums;
using UniAcademic.Infrastructure.Persistence;
using UniAcademic.Infrastructure.Services.Auth;
using Xunit;

namespace UniAcademic.Tests.Integration.Courses;

public sealed class CourseServiceTests
{
    [Fact]
    public async Task CreateAsync_ShouldCreateCourse_AndWriteAudit()
    {
        await using var dbContext = CreateDbContext();
        var faculty = await SeedFacultyAsync(dbContext);
        var service = CreateCourseService(dbContext);

        var result = await service.CreateAsync(new CreateCourseCommand
        {
            Code = "  CS101  ",
            Name = "  Nhap mon lap trinh  ",
            Credits = 3,
            FacultyId = faculty.Id,
            Status = CourseStatus.Active,
            Description = "Core course"
        });

        Assert.Equal("CS101", result.Code);
        Assert.Equal("Nhap mon lap trinh", result.Name);
        Assert.Equal(3, result.Credits);
        Assert.Equal(faculty.Id, result.FacultyId);

        var course = await dbContext.CoursesSet.SingleAsync();
        Assert.Equal("CS101", course.Code);

        var audit = await dbContext.AuditLogs.SingleAsync(x => x.Action == "course.create");
        Assert.Equal(nameof(Course), audit.EntityType);
        Assert.Equal(course.Id.ToString(), audit.EntityId);
    }

    [Fact]
    public async Task CreateAsync_ShouldFail_WhenCodeAlreadyExists()
    {
        await using var dbContext = CreateDbContext();
        dbContext.CoursesSet.Add(new Course
        {
            Code = "CS101",
            Name = "Existing",
            Credits = 3,
            Status = CourseStatus.Active,
            CreatedBy = "seed"
        });
        await dbContext.SaveChangesAsync();

        var service = CreateCourseService(dbContext);

        await Assert.ThrowsAsync<AuthException>(() => service.CreateAsync(new CreateCourseCommand
        {
            Code = " CS101 ",
            Name = "Another",
            Credits = 4,
            Status = CourseStatus.Active
        }));
    }

    [Fact]
    public async Task CreateAsync_ShouldFail_WhenCodeMatchesSoftDeletedCourse()
    {
        await using var dbContext = CreateDbContext();
        dbContext.CoursesSet.Add(new Course
        {
            Code = "CS101",
            Name = "Deleted course",
            Credits = 3,
            Status = CourseStatus.Inactive,
            IsDeleted = true,
            DeletedAtUtc = new DateTime(2026, 3, 19, 0, 0, 0, DateTimeKind.Utc),
            DeletedBy = "seed",
            CreatedBy = "seed"
        });
        await dbContext.SaveChangesAsync();

        var service = CreateCourseService(dbContext);

        await Assert.ThrowsAsync<AuthException>(() => service.CreateAsync(new CreateCourseCommand
        {
            Code = "CS101",
            Name = "New course",
            Credits = 3,
            Status = CourseStatus.Active
        }));
    }

    [Fact]
    public async Task CreateAsync_ShouldFail_WhenCreditsAreInvalid()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateCourseService(dbContext);

        await Assert.ThrowsAsync<AuthException>(() => service.CreateAsync(new CreateCourseCommand
        {
            Code = "CS101",
            Name = "Course",
            Credits = 0,
            Status = CourseStatus.Active
        }));
    }

    [Fact]
    public async Task CreateAsync_ShouldFail_WhenFacultyDoesNotExist()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateCourseService(dbContext);

        await Assert.ThrowsAsync<AuthException>(() => service.CreateAsync(new CreateCourseCommand
        {
            Code = "CS101",
            Name = "Course",
            Credits = 3,
            FacultyId = Guid.NewGuid(),
            Status = CourseStatus.Active
        }));
    }

    [Fact]
    public async Task CreateAsync_ShouldFail_WhenFacultyIsSoftDeleted()
    {
        await using var dbContext = CreateDbContext();
        var faculty = await SeedFacultyAsync(dbContext);
        faculty.IsDeleted = true;
        faculty.DeletedAtUtc = new DateTime(2026, 3, 19, 0, 0, 0, DateTimeKind.Utc);
        faculty.DeletedBy = "seed";
        await dbContext.SaveChangesAsync();

        var service = CreateCourseService(dbContext);

        var exception = await Assert.ThrowsAsync<AuthException>(() => service.CreateAsync(new CreateCourseCommand
        {
            Code = "CS101",
            Name = "Course",
            Credits = 3,
            FacultyId = faculty.Id,
            Status = CourseStatus.Active
        }));

        Assert.Equal("Faculty was not found.", exception.Message);
        Assert.False(await dbContext.CoursesSet.IgnoreQueryFilters().AnyAsync());
    }

    [Fact]
    public async Task DeleteAsync_ShouldSoftDeleteCourse()
    {
        await using var dbContext = CreateDbContext();
        var course = new Course
        {
            Code = "CS101",
            Name = "Course",
            Credits = 3,
            Status = CourseStatus.Active,
            CreatedBy = "seed"
        };
        dbContext.CoursesSet.Add(course);
        await dbContext.SaveChangesAsync();

        var service = CreateCourseService(dbContext);
        await service.DeleteAsync(new DeleteCourseCommand { Id = course.Id });

        var deleted = await dbContext.CoursesSet.IgnoreQueryFilters().SingleAsync(x => x.Id == course.Id);
        Assert.True(deleted.IsDeleted);
        Assert.Equal(CourseStatus.Inactive, deleted.Status);
        Assert.NotNull(deleted.DeletedAtUtc);
        Assert.False(await dbContext.CoursesSet.AnyAsync(x => x.Id == course.Id));
    }

    [Fact]
    public async Task GetListAsync_ShouldExcludeDeletedRows()
    {
        await using var dbContext = CreateDbContext();
        dbContext.CoursesSet.AddRange(
            new Course
            {
                Code = "CS101",
                Name = "Course 1",
                Credits = 3,
                Status = CourseStatus.Active,
                CreatedBy = "seed"
            },
            new Course
            {
                Code = "CS102",
                Name = "Course 2",
                Credits = 4,
                Status = CourseStatus.Inactive,
                IsDeleted = true,
                DeletedAtUtc = new DateTime(2026, 3, 19, 0, 0, 0, DateTimeKind.Utc),
                DeletedBy = "seed",
                CreatedBy = "seed"
            });
        await dbContext.SaveChangesAsync();

        var service = CreateCourseService(dbContext);
        var result = await service.GetListAsync(new GetCoursesQuery());

        Assert.Single(result);
        Assert.Equal("CS101", result.Single().Code);
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

    private static CourseService CreateCourseService(AppDbContext dbContext)
    {
        return new CourseService(
            dbContext,
            new AuditService(dbContext, new FakeClientContextAccessor()),
            new FakeCurrentUser(),
            new FakeDateTimeProvider());
    }

    private sealed class FakeCurrentUser : ICurrentUser
    {
        public Guid? UserId => Guid.Parse("33333333-3333-3333-3333-333333333333");
        public string? Username => "course-admin";
        public Guid? SessionId => null;
        public bool IsAuthenticated => true;
        public IReadOnlyCollection<string> Permissions => [PermissionConstants.Courses.View, PermissionConstants.Courses.Create, PermissionConstants.Courses.Edit, PermissionConstants.Courses.Delete];
    }

    private sealed class FakeDateTimeProvider : IDateTimeProvider
    {
        public DateTime UtcNow => new(2026, 3, 19, 0, 0, 0, DateTimeKind.Utc);
    }

    private sealed class FakeClientContextAccessor : IClientContextAccessor
    {
        public string? IpAddress => "127.0.0.1";
        public string? UserAgent => "CourseTests";
        public string ClientType => "Tests";
    }
}
