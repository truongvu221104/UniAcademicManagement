using Microsoft.EntityFrameworkCore;
using UniAcademic.Application.Abstractions.Common;
using UniAcademic.Application.Common;
using UniAcademic.Application.Features.Faculties;
using UniAcademic.Application.Models.Faculties;
using UniAcademic.Application.Security;
using UniAcademic.Domain.Entities.Academic;
using UniAcademic.Domain.Entities.Identity;
using UniAcademic.Domain.Enums;
using UniAcademic.Infrastructure.Persistence;
using UniAcademic.Infrastructure.Services.Auth;
using Xunit;

namespace UniAcademic.Tests.Integration.Faculties;

public sealed class FacultyServiceTests
{
    [Fact]
    public async Task CreateAsync_ShouldCreateFaculty_AndWriteAudit()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateFacultyService(dbContext);

        var result = await service.CreateAsync(new CreateFacultyCommand
        {
            Code = "  FIT  ",
            Name = "  Faculty of Information Technology  ",
            Description = "Core technology faculty",
            Status = FacultyStatus.Active
        });

        Assert.Equal("FIT", result.Code);
        Assert.Equal("Faculty of Information Technology", result.Name);

        var faculty = await dbContext.FacultiesSet.SingleAsync();
        Assert.Equal("FIT", faculty.Code);

        var audit = await dbContext.AuditLogs.SingleAsync(x => x.Action == "faculty.create");
        Assert.Equal(nameof(Faculty), audit.EntityType);
        Assert.Equal(faculty.Id.ToString(), audit.EntityId);
    }

    [Fact]
    public async Task CreateAsync_ShouldFail_WhenCodeAlreadyExists()
    {
        await using var dbContext = CreateDbContext();
        dbContext.FacultiesSet.Add(new Faculty
        {
            Code = "FIT",
            Name = "Faculty of IT",
            Status = FacultyStatus.Active,
            CreatedBy = "seed"
        });
        await dbContext.SaveChangesAsync();

        var service = CreateFacultyService(dbContext);

        await Assert.ThrowsAsync<AuthException>(() => service.CreateAsync(new CreateFacultyCommand
        {
            Code = " FIT ",
            Name = "Another Faculty",
            Status = FacultyStatus.Active
        }));
    }

    [Fact]
    public async Task CreateAsync_ShouldFail_WhenCodeMatchesSoftDeletedFaculty()
    {
        await using var dbContext = CreateDbContext();
        dbContext.FacultiesSet.Add(new Faculty
        {
            Code = "FIT",
            Name = "Faculty of IT",
            Status = FacultyStatus.Inactive,
            IsDeleted = true,
            DeletedAtUtc = new DateTime(2026, 3, 18, 0, 0, 0, DateTimeKind.Utc),
            DeletedBy = "seed",
            CreatedBy = "seed"
        });
        await dbContext.SaveChangesAsync();

        var service = CreateFacultyService(dbContext);

        await Assert.ThrowsAsync<AuthException>(() => service.CreateAsync(new CreateFacultyCommand
        {
            Code = " FIT ",
            Name = "New Faculty of IT",
            Status = FacultyStatus.Active
        }));
    }

    [Fact]
    public async Task DeleteAsync_ShouldSoftDeleteFaculty()
    {
        await using var dbContext = CreateDbContext();
        var faculty = new Faculty
        {
            Code = "BUS",
            Name = "Business",
            Status = FacultyStatus.Active,
            CreatedBy = "seed"
        };
        dbContext.FacultiesSet.Add(faculty);
        await dbContext.SaveChangesAsync();

        var service = CreateFacultyService(dbContext);
        await service.DeleteAsync(new DeleteFacultyCommand { Id = faculty.Id });

        var deleted = await dbContext.FacultiesSet.IgnoreQueryFilters().SingleAsync(x => x.Id == faculty.Id);
        Assert.True(deleted.IsDeleted);
        Assert.Equal(FacultyStatus.Inactive, deleted.Status);
        Assert.NotNull(deleted.DeletedAtUtc);
        Assert.False(await dbContext.FacultiesSet.AnyAsync(x => x.Id == faculty.Id));
    }

    [Fact]
    public async Task GetListAsync_ShouldExcludeDeletedRows()
    {
        await using var dbContext = CreateDbContext();
        dbContext.FacultiesSet.AddRange(
            new Faculty
            {
                Code = "LAW",
                Name = "Law",
                Status = FacultyStatus.Active,
                CreatedBy = "seed"
            },
            new Faculty
            {
                Code = "MED",
                Name = "Medicine",
                Status = FacultyStatus.Inactive,
                IsDeleted = true,
                DeletedAtUtc = new DateTime(2026, 3, 18, 0, 0, 0, DateTimeKind.Utc),
                DeletedBy = "seed",
                CreatedBy = "seed"
            });
        await dbContext.SaveChangesAsync();

        var service = CreateFacultyService(dbContext);
        var result = await service.GetListAsync(new GetFacultiesQuery());

        Assert.Single(result);
        Assert.Equal("LAW", result.Single().Code);
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private static FacultyService CreateFacultyService(AppDbContext dbContext)
    {
        return new FacultyService(
            dbContext,
            new AuditService(dbContext, new FakeClientContextAccessor()),
            new FakeCurrentUser(),
            new FakeDateTimeProvider());
    }

    private sealed class FakeCurrentUser : ICurrentUser
    {
        public Guid? UserId => Guid.Parse("11111111-1111-1111-1111-111111111111");
        public string? Username => "faculty-admin";
        public Guid? SessionId => null;
        public bool IsAuthenticated => true;
        public IReadOnlyCollection<string> Permissions => [PermissionConstants.Faculties.View, PermissionConstants.Faculties.Create, PermissionConstants.Faculties.Edit, PermissionConstants.Faculties.Delete];
    }

    private sealed class FakeDateTimeProvider : IDateTimeProvider
    {
        public DateTime UtcNow => new(2026, 3, 18, 0, 0, 0, DateTimeKind.Utc);
    }

    private sealed class FakeClientContextAccessor : IClientContextAccessor
    {
        public string? IpAddress => "127.0.0.1";
        public string? UserAgent => "FacultyTests";
        public string ClientType => "Tests";
    }
}
