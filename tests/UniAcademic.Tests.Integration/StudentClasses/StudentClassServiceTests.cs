using Microsoft.EntityFrameworkCore;
using UniAcademic.Application.Abstractions.Common;
using UniAcademic.Application.Common;
using UniAcademic.Application.Features.StudentClasses;
using UniAcademic.Application.Models.StudentClasses;
using UniAcademic.Application.Security;
using UniAcademic.Domain.Entities.Academic;
using UniAcademic.Domain.Enums;
using UniAcademic.Infrastructure.Persistence;
using UniAcademic.Infrastructure.Services.Auth;
using Xunit;

namespace UniAcademic.Tests.Integration.StudentClasses;

public sealed class StudentClassServiceTests
{
    [Fact]
    public async Task CreateAsync_ShouldCreateStudentClass_AndWriteAudit()
    {
        await using var dbContext = CreateDbContext();
        var faculty = await SeedFacultyAsync(dbContext);
        var service = CreateStudentClassService(dbContext);

        var result = await service.CreateAsync(new CreateStudentClassCommand
        {
            Code = "DHKTPM18A",
            Name = "  Lop KTPM 18A  ",
            FacultyId = faculty.Id,
            IntakeYear = 2024,
            Status = StudentClassStatus.Active,
            Description = "Administrative class"
        });

        Assert.Equal("DHKTPM18A", result.Code);
        Assert.Equal("Lop KTPM 18A", result.Name);
        Assert.Equal(faculty.Id, result.FacultyId);

        var studentClass = await dbContext.StudentClassesSet.SingleAsync();
        Assert.Equal("DHKTPM18A", studentClass.Code);

        var audit = await dbContext.AuditLogs.SingleAsync(x => x.Action == "studentclass.create");
        Assert.Equal(nameof(StudentClass), audit.EntityType);
        Assert.Equal(studentClass.Id.ToString(), audit.EntityId);
    }

    [Fact]
    public async Task CreateAsync_ShouldFail_WhenCodeAlreadyExists()
    {
        await using var dbContext = CreateDbContext();
        var faculty = await SeedFacultyAsync(dbContext);
        dbContext.StudentClassesSet.Add(new StudentClass
        {
            Code = "DHKTPM18A",
            Name = "Existing class",
            FacultyId = faculty.Id,
            IntakeYear = 2024,
            Status = StudentClassStatus.Active,
            CreatedBy = "seed"
        });
        await dbContext.SaveChangesAsync();

        var service = CreateStudentClassService(dbContext);

        await Assert.ThrowsAsync<AuthException>(() => service.CreateAsync(new CreateStudentClassCommand
        {
            Code = " DHKTPM18A ",
            Name = "Another class",
            FacultyId = faculty.Id,
            IntakeYear = 2024,
            Status = StudentClassStatus.Active
        }));
    }

    [Fact]
    public async Task CreateAsync_ShouldFail_WhenFacultyDoesNotExist()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateStudentClassService(dbContext);

        await Assert.ThrowsAsync<AuthException>(() => service.CreateAsync(new CreateStudentClassCommand
        {
            Code = "DHKTPM18A",
            Name = "Class",
            FacultyId = Guid.NewGuid(),
            IntakeYear = 2024,
            Status = StudentClassStatus.Active
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

        var service = CreateStudentClassService(dbContext);

        var exception = await Assert.ThrowsAsync<AuthException>(() => service.CreateAsync(new CreateStudentClassCommand
        {
            Code = "DHKTPM18A",
            Name = "Class",
            FacultyId = faculty.Id,
            IntakeYear = 2024,
            Status = StudentClassStatus.Active
        }));

        Assert.Equal("Faculty was not found.", exception.Message);
        Assert.False(await dbContext.StudentClassesSet.IgnoreQueryFilters().AnyAsync());
    }

    [Fact]
    public async Task DeleteAsync_ShouldSoftDeleteStudentClass()
    {
        await using var dbContext = CreateDbContext();
        var faculty = await SeedFacultyAsync(dbContext);
        var studentClass = new StudentClass
        {
            Code = "DHKTPM18A",
            Name = "Class",
            FacultyId = faculty.Id,
            IntakeYear = 2024,
            Status = StudentClassStatus.Active,
            CreatedBy = "seed"
        };
        dbContext.StudentClassesSet.Add(studentClass);
        await dbContext.SaveChangesAsync();

        var service = CreateStudentClassService(dbContext);
        await service.DeleteAsync(new DeleteStudentClassCommand { Id = studentClass.Id });

        var deleted = await dbContext.StudentClassesSet.IgnoreQueryFilters().SingleAsync(x => x.Id == studentClass.Id);
        Assert.True(deleted.IsDeleted);
        Assert.Equal(StudentClassStatus.Inactive, deleted.Status);
        Assert.NotNull(deleted.DeletedAtUtc);
        Assert.False(await dbContext.StudentClassesSet.AnyAsync(x => x.Id == studentClass.Id));
    }

    [Fact]
    public async Task GetListAsync_ShouldExcludeDeletedRows()
    {
        await using var dbContext = CreateDbContext();
        var faculty = await SeedFacultyAsync(dbContext);
        dbContext.StudentClassesSet.AddRange(
            new StudentClass
            {
                Code = "CNTT01",
                Name = "CNTT01",
                FacultyId = faculty.Id,
                IntakeYear = 2024,
                Status = StudentClassStatus.Active,
                CreatedBy = "seed"
            },
            new StudentClass
            {
                Code = "CNTT02",
                Name = "CNTT02",
                FacultyId = faculty.Id,
                IntakeYear = 2024,
                Status = StudentClassStatus.Inactive,
                IsDeleted = true,
                DeletedAtUtc = new DateTime(2026, 3, 19, 0, 0, 0, DateTimeKind.Utc),
                DeletedBy = "seed",
                CreatedBy = "seed"
            });
        await dbContext.SaveChangesAsync();

        var service = CreateStudentClassService(dbContext);
        var result = await service.GetListAsync(new GetStudentClassesQuery());

        Assert.Single(result);
        Assert.Equal("CNTT01", result.Single().Code);
    }

    [Fact]
    public async Task CreateAsync_ShouldFail_WhenCodeMatchesSoftDeletedStudentClass()
    {
        await using var dbContext = CreateDbContext();
        var faculty = await SeedFacultyAsync(dbContext);
        dbContext.StudentClassesSet.Add(new StudentClass
        {
            Code = "DHKTPM18A",
            Name = "Deleted class",
            FacultyId = faculty.Id,
            IntakeYear = 2024,
            Status = StudentClassStatus.Inactive,
            IsDeleted = true,
            DeletedAtUtc = new DateTime(2026, 3, 19, 0, 0, 0, DateTimeKind.Utc),
            DeletedBy = "seed",
            CreatedBy = "seed"
        });
        await dbContext.SaveChangesAsync();

        var service = CreateStudentClassService(dbContext);

        await Assert.ThrowsAsync<AuthException>(() => service.CreateAsync(new CreateStudentClassCommand
        {
            Code = " DHKTPM18A ",
            Name = "New class",
            FacultyId = faculty.Id,
            IntakeYear = 2024,
            Status = StudentClassStatus.Active
        }));
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

    private static StudentClassService CreateStudentClassService(AppDbContext dbContext)
    {
        return new StudentClassService(
            dbContext,
            new AuditService(dbContext, new FakeClientContextAccessor()),
            new FakeCurrentUser(),
            new FakeDateTimeProvider());
    }

    private sealed class FakeCurrentUser : ICurrentUser
    {
        public Guid? UserId => Guid.Parse("22222222-2222-2222-2222-222222222222");
        public string? Username => "studentclass-admin";
        public Guid? SessionId => null;
        public bool IsAuthenticated => true;
        public IReadOnlyCollection<string> Permissions => [PermissionConstants.StudentClasses.View, PermissionConstants.StudentClasses.Create, PermissionConstants.StudentClasses.Edit, PermissionConstants.StudentClasses.Delete];
    }

    private sealed class FakeDateTimeProvider : IDateTimeProvider
    {
        public DateTime UtcNow => new(2026, 3, 19, 0, 0, 0, DateTimeKind.Utc);
    }

    private sealed class FakeClientContextAccessor : IClientContextAccessor
    {
        public string? IpAddress => "127.0.0.1";
        public string? UserAgent => "StudentClassTests";
        public string ClientType => "Tests";
    }
}
