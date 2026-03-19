using Microsoft.EntityFrameworkCore;
using UniAcademic.Application.Abstractions.Common;
using UniAcademic.Application.Common;
using UniAcademic.Application.Features.CourseOfferings;
using UniAcademic.Application.Models.CourseOfferings;
using UniAcademic.Application.Security;
using UniAcademic.Domain.Entities.Academic;
using UniAcademic.Domain.Enums;
using UniAcademic.Infrastructure.Persistence;
using UniAcademic.Infrastructure.Services.Auth;
using Xunit;

namespace UniAcademic.Tests.Integration.CourseOfferings;

public sealed class CourseOfferingServiceTests
{
    [Fact]
    public async Task CreateAsync_ShouldCreateCourseOffering_AndWriteAudit()
    {
        await using var dbContext = CreateDbContext();
        var course = await SeedCourseAsync(dbContext);
        var semester = await SeedSemesterAsync(dbContext);
        var service = CreateCourseOfferingService(dbContext);

        var result = await service.CreateAsync(new CreateCourseOfferingCommand
        {
            Code = "  CS101-HK1-01  ",
            CourseId = course.Id,
            SemesterId = semester.Id,
            DisplayName = "  Lap trinh can ban - Nhom 01  ",
            Capacity = 60,
            Status = CourseOfferingStatus.Active,
            Description = "Offering"
        });

        Assert.Equal("CS101-HK1-01", result.Code);
        Assert.Equal("Lap trinh can ban - Nhom 01", result.DisplayName);
        Assert.Equal(course.Id, result.CourseId);
        Assert.Equal(semester.Id, result.SemesterId);

        var offering = await dbContext.CourseOfferingsSet.SingleAsync();
        Assert.Equal("CS101-HK1-01", offering.Code);

        var audit = await dbContext.AuditLogs.SingleAsync(x => x.Action == "courseoffering.create");
        Assert.Equal(nameof(CourseOffering), audit.EntityType);
        Assert.Equal(offering.Id.ToString(), audit.EntityId);
    }

    [Fact]
    public async Task CreateAsync_ShouldFail_WhenCodeAlreadyExists()
    {
        await using var dbContext = CreateDbContext();
        var course = await SeedCourseAsync(dbContext);
        var semester = await SeedSemesterAsync(dbContext);
        dbContext.CourseOfferingsSet.Add(new CourseOffering
        {
            Code = "CS101-HK1-01",
            CourseId = course.Id,
            SemesterId = semester.Id,
            DisplayName = "Existing",
            Capacity = 50,
            Status = CourseOfferingStatus.Active,
            CreatedBy = "seed"
        });
        await dbContext.SaveChangesAsync();

        var service = CreateCourseOfferingService(dbContext);

        await Assert.ThrowsAsync<AuthException>(() => service.CreateAsync(new CreateCourseOfferingCommand
        {
            Code = " CS101-HK1-01 ",
            CourseId = course.Id,
            SemesterId = semester.Id,
            DisplayName = "New",
            Capacity = 60,
            Status = CourseOfferingStatus.Active
        }));
    }

    [Fact]
    public async Task CreateAsync_ShouldFail_WhenCodeMatchesSoftDeletedCourseOffering()
    {
        await using var dbContext = CreateDbContext();
        var course = await SeedCourseAsync(dbContext);
        var semester = await SeedSemesterAsync(dbContext);
        dbContext.CourseOfferingsSet.Add(new CourseOffering
        {
            Code = "CS101-HK1-01",
            CourseId = course.Id,
            SemesterId = semester.Id,
            DisplayName = "Deleted",
            Capacity = 50,
            Status = CourseOfferingStatus.Inactive,
            IsDeleted = true,
            DeletedAtUtc = new DateTime(2026, 3, 19, 0, 0, 0, DateTimeKind.Utc),
            DeletedBy = "seed",
            CreatedBy = "seed"
        });
        await dbContext.SaveChangesAsync();

        var service = CreateCourseOfferingService(dbContext);

        await Assert.ThrowsAsync<AuthException>(() => service.CreateAsync(new CreateCourseOfferingCommand
        {
            Code = "CS101-HK1-01",
            CourseId = course.Id,
            SemesterId = semester.Id,
            DisplayName = "New",
            Capacity = 60,
            Status = CourseOfferingStatus.Active
        }));
    }

    [Fact]
    public async Task CreateAsync_ShouldFail_WhenCapacityIsInvalid()
    {
        await using var dbContext = CreateDbContext();
        var course = await SeedCourseAsync(dbContext);
        var semester = await SeedSemesterAsync(dbContext);
        var service = CreateCourseOfferingService(dbContext);

        await Assert.ThrowsAsync<AuthException>(() => service.CreateAsync(new CreateCourseOfferingCommand
        {
            Code = "CS101-HK1-01",
            CourseId = course.Id,
            SemesterId = semester.Id,
            DisplayName = "Offering",
            Capacity = 0,
            Status = CourseOfferingStatus.Active
        }));
    }

    [Fact]
    public async Task CreateAsync_ShouldFail_WhenCourseDoesNotExist()
    {
        await using var dbContext = CreateDbContext();
        var semester = await SeedSemesterAsync(dbContext);
        var service = CreateCourseOfferingService(dbContext);

        await Assert.ThrowsAsync<AuthException>(() => service.CreateAsync(new CreateCourseOfferingCommand
        {
            Code = "CS101-HK1-01",
            CourseId = Guid.NewGuid(),
            SemesterId = semester.Id,
            DisplayName = "Offering",
            Capacity = 60,
            Status = CourseOfferingStatus.Active
        }));
    }

    [Fact]
    public async Task CreateAsync_ShouldFail_WhenCourseIsSoftDeleted()
    {
        await using var dbContext = CreateDbContext();
        var course = await SeedCourseAsync(dbContext);
        var semester = await SeedSemesterAsync(dbContext);
        course.IsDeleted = true;
        course.DeletedAtUtc = new DateTime(2026, 3, 19, 0, 0, 0, DateTimeKind.Utc);
        course.DeletedBy = "seed";
        await dbContext.SaveChangesAsync();

        var service = CreateCourseOfferingService(dbContext);
        var exception = await Assert.ThrowsAsync<AuthException>(() => service.CreateAsync(new CreateCourseOfferingCommand
        {
            Code = "CS101-HK1-01",
            CourseId = course.Id,
            SemesterId = semester.Id,
            DisplayName = "Offering",
            Capacity = 60,
            Status = CourseOfferingStatus.Active
        }));

        Assert.Equal("Course was not found.", exception.Message);
        Assert.False(await dbContext.CourseOfferingsSet.IgnoreQueryFilters().AnyAsync());
    }

    [Fact]
    public async Task CreateAsync_ShouldFail_WhenSemesterDoesNotExist()
    {
        await using var dbContext = CreateDbContext();
        var course = await SeedCourseAsync(dbContext);
        var service = CreateCourseOfferingService(dbContext);

        await Assert.ThrowsAsync<AuthException>(() => service.CreateAsync(new CreateCourseOfferingCommand
        {
            Code = "CS101-HK1-01",
            CourseId = course.Id,
            SemesterId = Guid.NewGuid(),
            DisplayName = "Offering",
            Capacity = 60,
            Status = CourseOfferingStatus.Active
        }));
    }

    [Fact]
    public async Task CreateAsync_ShouldFail_WhenSemesterIsSoftDeleted()
    {
        await using var dbContext = CreateDbContext();
        var course = await SeedCourseAsync(dbContext);
        var semester = await SeedSemesterAsync(dbContext);
        semester.IsDeleted = true;
        semester.DeletedAtUtc = new DateTime(2026, 3, 19, 0, 0, 0, DateTimeKind.Utc);
        semester.DeletedBy = "seed";
        await dbContext.SaveChangesAsync();

        var service = CreateCourseOfferingService(dbContext);
        var exception = await Assert.ThrowsAsync<AuthException>(() => service.CreateAsync(new CreateCourseOfferingCommand
        {
            Code = "CS101-HK1-01",
            CourseId = course.Id,
            SemesterId = semester.Id,
            DisplayName = "Offering",
            Capacity = 60,
            Status = CourseOfferingStatus.Active
        }));

        Assert.Equal("Semester was not found.", exception.Message);
        Assert.False(await dbContext.CourseOfferingsSet.IgnoreQueryFilters().AnyAsync());
    }

    [Fact]
    public async Task DeleteAsync_ShouldSoftDeleteCourseOffering()
    {
        await using var dbContext = CreateDbContext();
        var course = await SeedCourseAsync(dbContext);
        var semester = await SeedSemesterAsync(dbContext);
        var offering = new CourseOffering
        {
            Code = "CS101-HK1-01",
            CourseId = course.Id,
            SemesterId = semester.Id,
            DisplayName = "Offering",
            Capacity = 60,
            Status = CourseOfferingStatus.Active,
            CreatedBy = "seed"
        };
        dbContext.CourseOfferingsSet.Add(offering);
        await dbContext.SaveChangesAsync();

        var service = CreateCourseOfferingService(dbContext);
        await service.DeleteAsync(new DeleteCourseOfferingCommand { Id = offering.Id });

        var deleted = await dbContext.CourseOfferingsSet.IgnoreQueryFilters().SingleAsync(x => x.Id == offering.Id);
        Assert.True(deleted.IsDeleted);
        Assert.Equal(CourseOfferingStatus.Inactive, deleted.Status);
        Assert.NotNull(deleted.DeletedAtUtc);
        Assert.False(await dbContext.CourseOfferingsSet.AnyAsync(x => x.Id == offering.Id));
    }

    [Fact]
    public async Task GetListAsync_ShouldExcludeDeletedRows()
    {
        await using var dbContext = CreateDbContext();
        var course = await SeedCourseAsync(dbContext);
        var semester = await SeedSemesterAsync(dbContext);
        dbContext.CourseOfferingsSet.AddRange(
            new CourseOffering
            {
                Code = "CS101-HK1-01",
                CourseId = course.Id,
                SemesterId = semester.Id,
                DisplayName = "Offering 1",
                Capacity = 60,
                Status = CourseOfferingStatus.Active,
                CreatedBy = "seed"
            },
            new CourseOffering
            {
                Code = "CS101-HK1-02",
                CourseId = course.Id,
                SemesterId = semester.Id,
                DisplayName = "Offering 2",
                Capacity = 60,
                Status = CourseOfferingStatus.Inactive,
                IsDeleted = true,
                DeletedAtUtc = new DateTime(2026, 3, 19, 0, 0, 0, DateTimeKind.Utc),
                DeletedBy = "seed",
                CreatedBy = "seed"
            });
        await dbContext.SaveChangesAsync();

        var service = CreateCourseOfferingService(dbContext);
        var result = await service.GetListAsync(new GetCourseOfferingsQuery());

        Assert.Single(result);
        Assert.Equal("CS101-HK1-01", result.Single().Code);
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private static async Task<Course> SeedCourseAsync(AppDbContext dbContext)
    {
        var course = new Course
        {
            Code = "CS101",
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
            Code = "HK1-2526",
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

    private static CourseOfferingService CreateCourseOfferingService(AppDbContext dbContext)
    {
        return new CourseOfferingService(
            dbContext,
            new AuditService(dbContext, new FakeClientContextAccessor()),
            new FakeCurrentUser(),
            new FakeDateTimeProvider());
    }

    private sealed class FakeCurrentUser : ICurrentUser
    {
        public Guid? UserId => Guid.Parse("55555555-5555-5555-5555-555555555555");
        public string? Username => "courseoffering-admin";
        public Guid? SessionId => null;
        public bool IsAuthenticated => true;
        public IReadOnlyCollection<string> Permissions => [PermissionConstants.CourseOfferings.View, PermissionConstants.CourseOfferings.Create, PermissionConstants.CourseOfferings.Edit, PermissionConstants.CourseOfferings.Delete];
    }

    private sealed class FakeDateTimeProvider : IDateTimeProvider
    {
        public DateTime UtcNow => new(2026, 3, 19, 0, 0, 0, DateTimeKind.Utc);
    }

    private sealed class FakeClientContextAccessor : IClientContextAccessor
    {
        public string? IpAddress => "127.0.0.1";
        public string? UserAgent => "CourseOfferingTests";
        public string ClientType => "Tests";
    }
}
