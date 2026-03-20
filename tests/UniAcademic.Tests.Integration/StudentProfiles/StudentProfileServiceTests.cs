using Microsoft.EntityFrameworkCore;
using UniAcademic.Application.Abstractions.Common;
using UniAcademic.Application.Common;
using UniAcademic.Application.Features.StudentProfiles;
using UniAcademic.Application.Models.StudentProfiles;
using UniAcademic.Application.Security;
using UniAcademic.Domain.Entities.Academic;
using UniAcademic.Domain.Enums;
using UniAcademic.Infrastructure.Persistence;
using UniAcademic.Infrastructure.Services.Auth;
using Xunit;

namespace UniAcademic.Tests.Integration.StudentProfiles;

public sealed class StudentProfileServiceTests
{
    [Fact]
    public async Task CreateAsync_ShouldCreateStudentProfile_AndWriteAudit()
    {
        await using var dbContext = CreateDbContext();
        var studentClass = await SeedStudentClassAsync(dbContext);
        var service = CreateStudentProfileService(dbContext);

        var result = await service.CreateAsync(new CreateStudentProfileCommand
        {
            Code = "  SV001  ",
            FullName = "  Nguyen Van A  ",
            StudentClassId = studentClass.Id,
            Email = " student@example.com ",
            Phone = " 0123456789 ",
            DateOfBirth = new DateTime(2005, 1, 15, 10, 0, 0, DateTimeKind.Utc),
            Gender = StudentGender.Male,
            Address = "  123 Le Loi  ",
            Status = StudentProfileStatus.Active,
            Note = "  Ghi chu  "
        });

        Assert.Equal("SV001", result.StudentCode);
        Assert.Equal("Nguyen Van A", result.FullName);
        Assert.Equal("student@example.com", result.Email);
        Assert.Equal("0123456789", result.Phone);
        Assert.Equal(new DateTime(2005, 1, 15), result.DateOfBirth);
        Assert.Equal("123 Le Loi", result.Address);
        Assert.Equal("Ghi chu", result.Note);

        var studentProfile = await dbContext.StudentProfilesSet.SingleAsync();
        Assert.Equal("SV001", studentProfile.StudentCode);

        var audit = await dbContext.AuditLogs.SingleAsync(x => x.Action == "studentprofile.create");
        Assert.Equal(nameof(StudentProfile), audit.EntityType);
        Assert.Equal(studentProfile.Id.ToString(), audit.EntityId);
    }

    [Fact]
    public async Task CreateAsync_ShouldFail_WhenCodeAlreadyExists()
    {
        await using var dbContext = CreateDbContext();
        var studentClass = await SeedStudentClassAsync(dbContext);
        dbContext.StudentProfilesSet.Add(new StudentProfile
        {
            StudentCode = "SV001",
            FullName = "Existing",
            StudentClassId = studentClass.Id,
            Status = StudentProfileStatus.Active,
            CreatedBy = "seed"
        });
        await dbContext.SaveChangesAsync();

        var service = CreateStudentProfileService(dbContext);

        await Assert.ThrowsAsync<AuthException>(() => service.CreateAsync(new CreateStudentProfileCommand
        {
            Code = " SV001 ",
            FullName = "Another",
            StudentClassId = studentClass.Id,
            Status = StudentProfileStatus.Active
        }));
    }

    [Fact]
    public async Task CreateAsync_ShouldFail_WhenCodeMatchesSoftDeletedStudentProfile()
    {
        await using var dbContext = CreateDbContext();
        var studentClass = await SeedStudentClassAsync(dbContext);
        dbContext.StudentProfilesSet.Add(new StudentProfile
        {
            StudentCode = "SV001",
            FullName = "Deleted",
            StudentClassId = studentClass.Id,
            Status = StudentProfileStatus.Inactive,
            IsDeleted = true,
            DeletedAtUtc = new DateTime(2026, 3, 20, 0, 0, 0, DateTimeKind.Utc),
            DeletedBy = "seed",
            CreatedBy = "seed"
        });
        await dbContext.SaveChangesAsync();

        var service = CreateStudentProfileService(dbContext);

        await Assert.ThrowsAsync<AuthException>(() => service.CreateAsync(new CreateStudentProfileCommand
        {
            Code = " SV001 ",
            FullName = "Another",
            StudentClassId = studentClass.Id,
            Status = StudentProfileStatus.Active
        }));
    }

    [Fact]
    public async Task CreateAsync_ShouldFail_WhenStudentClassDoesNotExist()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateStudentProfileService(dbContext);

        await Assert.ThrowsAsync<AuthException>(() => service.CreateAsync(new CreateStudentProfileCommand
        {
            Code = "SV001",
            FullName = "Student",
            StudentClassId = Guid.NewGuid(),
            Status = StudentProfileStatus.Active
        }));
    }

    [Fact]
    public async Task CreateAsync_ShouldFail_WhenStudentClassIsSoftDeleted()
    {
        await using var dbContext = CreateDbContext();
        var studentClass = await SeedStudentClassAsync(dbContext);
        studentClass.IsDeleted = true;
        studentClass.DeletedAtUtc = new DateTime(2026, 3, 20, 0, 0, 0, DateTimeKind.Utc);
        studentClass.DeletedBy = "seed";
        await dbContext.SaveChangesAsync();

        var service = CreateStudentProfileService(dbContext);
        var exception = await Assert.ThrowsAsync<AuthException>(() => service.CreateAsync(new CreateStudentProfileCommand
        {
            Code = "SV001",
            FullName = "Student",
            StudentClassId = studentClass.Id,
            Status = StudentProfileStatus.Active
        }));

        Assert.Equal("Student class was not found.", exception.Message);
        Assert.False(await dbContext.StudentProfilesSet.IgnoreQueryFilters().AnyAsync());
    }

    [Fact]
    public async Task CreateAsync_ShouldFail_WhenEmailIsInvalid()
    {
        await using var dbContext = CreateDbContext();
        var studentClass = await SeedStudentClassAsync(dbContext);
        var service = CreateStudentProfileService(dbContext);

        await Assert.ThrowsAsync<AuthException>(() => service.CreateAsync(new CreateStudentProfileCommand
        {
            Code = "SV001",
            FullName = "Student",
            StudentClassId = studentClass.Id,
            Email = "not-an-email",
            Status = StudentProfileStatus.Active
        }));
    }

    [Fact]
    public async Task CreateAsync_ShouldFail_WhenDateOfBirthIsInTheFuture()
    {
        await using var dbContext = CreateDbContext();
        var studentClass = await SeedStudentClassAsync(dbContext);
        var service = CreateStudentProfileService(dbContext);

        await Assert.ThrowsAsync<AuthException>(() => service.CreateAsync(new CreateStudentProfileCommand
        {
            Code = "SV001",
            FullName = "Student",
            StudentClassId = studentClass.Id,
            DateOfBirth = new DateTime(2026, 3, 21),
            Status = StudentProfileStatus.Active
        }));
    }

    [Fact]
    public async Task DeleteAsync_ShouldSoftDeleteStudentProfile()
    {
        await using var dbContext = CreateDbContext();
        var studentClass = await SeedStudentClassAsync(dbContext);
        var studentProfile = new StudentProfile
        {
            StudentCode = "SV001",
            FullName = "Student",
            StudentClassId = studentClass.Id,
            Status = StudentProfileStatus.Active,
            CreatedBy = "seed"
        };
        dbContext.StudentProfilesSet.Add(studentProfile);
        await dbContext.SaveChangesAsync();

        var service = CreateStudentProfileService(dbContext);
        await service.DeleteAsync(new DeleteStudentProfileCommand { Id = studentProfile.Id });

        var deleted = await dbContext.StudentProfilesSet.IgnoreQueryFilters().SingleAsync(x => x.Id == studentProfile.Id);
        Assert.True(deleted.IsDeleted);
        Assert.Equal(StudentProfileStatus.Inactive, deleted.Status);
        Assert.NotNull(deleted.DeletedAtUtc);
        Assert.False(await dbContext.StudentProfilesSet.AnyAsync(x => x.Id == studentProfile.Id));
    }

    [Fact]
    public async Task GetListAsync_ShouldExcludeDeletedRows()
    {
        await using var dbContext = CreateDbContext();
        var studentClass = await SeedStudentClassAsync(dbContext);
        dbContext.StudentProfilesSet.AddRange(
            new StudentProfile
            {
                StudentCode = "SV001",
                FullName = "Student 1",
                StudentClassId = studentClass.Id,
                Status = StudentProfileStatus.Active,
                CreatedBy = "seed"
            },
            new StudentProfile
            {
                StudentCode = "SV002",
                FullName = "Student 2",
                StudentClassId = studentClass.Id,
                Status = StudentProfileStatus.Inactive,
                IsDeleted = true,
                DeletedAtUtc = new DateTime(2026, 3, 20, 0, 0, 0, DateTimeKind.Utc),
                DeletedBy = "seed",
                CreatedBy = "seed"
            });
        await dbContext.SaveChangesAsync();

        var service = CreateStudentProfileService(dbContext);
        var result = await service.GetListAsync(new GetStudentProfilesQuery());

        Assert.Single(result);
        Assert.Equal("SV001", result.Single().StudentCode);
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private static async Task<StudentClass> SeedStudentClassAsync(AppDbContext dbContext)
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
        return studentClass;
    }

    private static StudentProfileService CreateStudentProfileService(AppDbContext dbContext)
    {
        return new StudentProfileService(
            dbContext,
            new AuditService(dbContext, new FakeClientContextAccessor()),
            new FakeCurrentUser(),
            new FakeDateTimeProvider());
    }

    private sealed class FakeCurrentUser : ICurrentUser
    {
        public Guid? UserId => Guid.Parse("66666666-6666-6666-6666-666666666666");
        public string? Username => "studentprofile-admin";
        public Guid? SessionId => null;
        public bool IsAuthenticated => true;
        public IReadOnlyCollection<string> Permissions => [PermissionConstants.StudentProfiles.View, PermissionConstants.StudentProfiles.Create, PermissionConstants.StudentProfiles.Edit, PermissionConstants.StudentProfiles.Delete];
    }

    private sealed class FakeDateTimeProvider : IDateTimeProvider
    {
        public DateTime UtcNow => new(2026, 3, 20, 0, 0, 0, DateTimeKind.Utc);
    }

    private sealed class FakeClientContextAccessor : IClientContextAccessor
    {
        public string? IpAddress => "127.0.0.1";
        public string? UserAgent => "StudentProfileTests";
        public string ClientType => "Tests";
    }
}
