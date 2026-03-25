using Microsoft.EntityFrameworkCore;
using UniAcademic.Application.Abstractions.Common;
using UniAcademic.Application.Common;
using UniAcademic.Domain.Entities.Academic;
using UniAcademic.Domain.Entities.Identity;
using UniAcademic.Domain.Enums;
using UniAcademic.Infrastructure.Persistence;
using UniAcademic.Infrastructure.Security;
using Xunit;

namespace UniAcademic.Tests.Unit.Auth;

public sealed class CurrentAcademicContextTests
{
    [Fact]
    public async Task GetRequiredStudentProfileIdAsync_ShouldReturnMappedStudentProfileId()
    {
        await using var dbContext = CreateDbContext();
        var studentProfile = await SeedStudentProfileAsync(dbContext, "SV001");
        var user = await SeedUserAsync(dbContext, studentProfileId: studentProfile.Id);
        var context = new CurrentStudentContext(dbContext, new FakeCurrentUser(user.Id));

        var result = await context.GetRequiredStudentProfileIdAsync();

        Assert.Equal(studentProfile.Id, result);
    }

    [Fact]
    public async Task GetRequiredStudentProfileIdAsync_ShouldFail_WhenMappedStudentProfileIsSoftDeleted()
    {
        await using var dbContext = CreateDbContext();
        var studentProfile = await SeedStudentProfileAsync(dbContext, "SV001");
        studentProfile.IsDeleted = true;
        studentProfile.DeletedAtUtc = new DateTime(2026, 3, 25, 0, 0, 0, DateTimeKind.Utc);
        studentProfile.DeletedBy = "seed";
        await dbContext.SaveChangesAsync();

        var user = await SeedUserAsync(dbContext, studentProfileId: studentProfile.Id);
        var context = new CurrentStudentContext(dbContext, new FakeCurrentUser(user.Id));

        var exception = await Assert.ThrowsAsync<AuthException>(() => context.GetRequiredStudentProfileIdAsync());

        Assert.Equal("Current student profile was not found.", exception.Message);
    }

    [Fact]
    public async Task GetRequiredLecturerProfileIdAsync_ShouldReturnMappedLecturerProfileId()
    {
        await using var dbContext = CreateDbContext();
        var lecturerProfile = await SeedLecturerProfileAsync(dbContext, "GV001");
        var user = await SeedUserAsync(dbContext, lecturerProfileId: lecturerProfile.Id);
        var context = new CurrentLecturerContext(dbContext, new FakeCurrentUser(user.Id));

        var result = await context.GetRequiredLecturerProfileIdAsync();

        Assert.Equal(lecturerProfile.Id, result);
    }

    [Fact]
    public async Task GetRequiredLecturerProfileIdAsync_ShouldFail_WhenMappedLecturerProfileIsInactive()
    {
        await using var dbContext = CreateDbContext();
        var lecturerProfile = await SeedLecturerProfileAsync(dbContext, "GV001");
        lecturerProfile.IsActive = false;
        await dbContext.SaveChangesAsync();

        var user = await SeedUserAsync(dbContext, lecturerProfileId: lecturerProfile.Id);
        var context = new CurrentLecturerContext(dbContext, new FakeCurrentUser(user.Id));

        var exception = await Assert.ThrowsAsync<AuthException>(() => context.GetRequiredLecturerProfileIdAsync());

        Assert.Equal("Current lecturer profile is inactive.", exception.Message);
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

    private static async Task<LecturerProfile> SeedLecturerProfileAsync(AppDbContext dbContext, string lecturerCode)
    {
        var faculty = await SeedFacultyAsync(dbContext);
        var lecturerProfile = new LecturerProfile
        {
            Code = lecturerCode,
            FullName = $"Lecturer {lecturerCode}",
            FacultyId = faculty.Id,
            IsActive = true,
            CreatedBy = "seed"
        };

        dbContext.LecturerProfilesSet.Add(lecturerProfile);
        await dbContext.SaveChangesAsync();
        return lecturerProfile;
    }

    private static async Task<User> SeedUserAsync(AppDbContext dbContext, Guid? studentProfileId = null, Guid? lecturerProfileId = null)
    {
        var sequence = dbContext.Users.Count() + 1;
        var user = new User
        {
            Username = $"user{sequence}",
            NormalizedUsername = $"USER{sequence}",
            Email = $"user{sequence}@example.com",
            NormalizedEmail = $"USER{sequence}@EXAMPLE.COM",
            DisplayName = "Mapped User",
            PasswordHash = "hash",
            StudentProfileId = studentProfileId,
            LecturerProfileId = lecturerProfileId,
            CreatedBy = "seed"
        };

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();
        return user;
    }

    private sealed class FakeCurrentUser : ICurrentUser
    {
        public FakeCurrentUser(Guid userId)
        {
            UserId = userId;
        }

        public Guid? UserId { get; }

        public string? Username => "mapped-user";

        public Guid? SessionId => null;

        public bool IsAuthenticated => true;

        public IReadOnlyCollection<string> Permissions => [];
    }
}
