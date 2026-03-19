using Microsoft.EntityFrameworkCore;
using UniAcademic.Application.Abstractions.Common;
using UniAcademic.Application.Common;
using UniAcademic.Application.Features.Semesters;
using UniAcademic.Application.Models.Semesters;
using UniAcademic.Application.Security;
using UniAcademic.Domain.Entities.Academic;
using UniAcademic.Domain.Enums;
using UniAcademic.Infrastructure.Persistence;
using UniAcademic.Infrastructure.Services.Auth;
using Xunit;

namespace UniAcademic.Tests.Integration.Semesters;

public sealed class SemesterServiceTests
{
    [Fact]
    public async Task CreateAsync_ShouldCreateSemester_AndWriteAudit()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateSemesterService(dbContext);

        var result = await service.CreateAsync(new CreateSemesterCommand
        {
            Code = " HK1-2526 ",
            Name = " Hoc ky 1 ",
            AcademicYear = "2025-2026",
            TermNo = 1,
            StartDate = new DateTime(2025, 9, 1),
            EndDate = new DateTime(2026, 1, 15),
            Status = SemesterStatus.Active,
            Description = "Semester 1"
        });

        Assert.Equal("HK1-2526", result.Code);
        Assert.Equal("Hoc ky 1", result.Name);
        Assert.Equal("2025-2026", result.AcademicYear);

        var semester = await dbContext.SemestersSet.SingleAsync();
        Assert.Equal("HK1-2526", semester.Code);

        var audit = await dbContext.AuditLogs.SingleAsync(x => x.Action == "semester.create");
        Assert.Equal(nameof(Semester), audit.EntityType);
        Assert.Equal(semester.Id.ToString(), audit.EntityId);
    }

    [Fact]
    public async Task CreateAsync_ShouldFail_WhenCodeAlreadyExists()
    {
        await using var dbContext = CreateDbContext();
        dbContext.SemestersSet.Add(new Semester
        {
            Code = "HK1-2526",
            Name = "Existing",
            AcademicYear = "2025-2026",
            TermNo = 1,
            StartDate = new DateTime(2025, 9, 1),
            EndDate = new DateTime(2026, 1, 15),
            Status = SemesterStatus.Active,
            CreatedBy = "seed"
        });
        await dbContext.SaveChangesAsync();

        var service = CreateSemesterService(dbContext);

        await Assert.ThrowsAsync<AuthException>(() => service.CreateAsync(new CreateSemesterCommand
        {
            Code = " HK1-2526 ",
            Name = "Another",
            AcademicYear = "2025-2026",
            TermNo = 2,
            StartDate = new DateTime(2026, 1, 16),
            EndDate = new DateTime(2026, 5, 15),
            Status = SemesterStatus.Active
        }));
    }

    [Fact]
    public async Task CreateAsync_ShouldFail_WhenCodeMatchesSoftDeletedSemester()
    {
        await using var dbContext = CreateDbContext();
        dbContext.SemestersSet.Add(new Semester
        {
            Code = "HK1-2526",
            Name = "Deleted",
            AcademicYear = "2025-2026",
            TermNo = 1,
            StartDate = new DateTime(2025, 9, 1),
            EndDate = new DateTime(2026, 1, 15),
            Status = SemesterStatus.Inactive,
            IsDeleted = true,
            DeletedAtUtc = new DateTime(2026, 3, 19, 0, 0, 0, DateTimeKind.Utc),
            DeletedBy = "seed",
            CreatedBy = "seed"
        });
        await dbContext.SaveChangesAsync();

        var service = CreateSemesterService(dbContext);

        await Assert.ThrowsAsync<AuthException>(() => service.CreateAsync(new CreateSemesterCommand
        {
            Code = "HK1-2526",
            Name = "New",
            AcademicYear = "2025-2026",
            TermNo = 1,
            StartDate = new DateTime(2025, 9, 1),
            EndDate = new DateTime(2026, 1, 15),
            Status = SemesterStatus.Active
        }));
    }

    [Fact]
    public async Task CreateAsync_ShouldFail_WhenAcademicYearIsInvalid()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateSemesterService(dbContext);

        await Assert.ThrowsAsync<AuthException>(() => service.CreateAsync(new CreateSemesterCommand
        {
            Code = "HK1-2526",
            Name = "Semester",
            AcademicYear = "2025-2027",
            TermNo = 1,
            StartDate = new DateTime(2025, 9, 1),
            EndDate = new DateTime(2026, 1, 15),
            Status = SemesterStatus.Active
        }));
    }

    [Fact]
    public async Task CreateAsync_ShouldFail_WhenTermNoIsInvalid()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateSemesterService(dbContext);

        await Assert.ThrowsAsync<AuthException>(() => service.CreateAsync(new CreateSemesterCommand
        {
            Code = "HK1-2526",
            Name = "Semester",
            AcademicYear = "2025-2026",
            TermNo = 4,
            StartDate = new DateTime(2025, 9, 1),
            EndDate = new DateTime(2026, 1, 15),
            Status = SemesterStatus.Active
        }));
    }

    [Fact]
    public async Task CreateAsync_ShouldFail_WhenDateRangeIsInvalid()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateSemesterService(dbContext);

        await Assert.ThrowsAsync<AuthException>(() => service.CreateAsync(new CreateSemesterCommand
        {
            Code = "HK1-2526",
            Name = "Semester",
            AcademicYear = "2025-2026",
            TermNo = 1,
            StartDate = new DateTime(2026, 1, 15),
            EndDate = new DateTime(2025, 9, 1),
            Status = SemesterStatus.Active
        }));
    }

    [Fact]
    public async Task DeleteAsync_ShouldSoftDeleteSemester()
    {
        await using var dbContext = CreateDbContext();
        var semester = new Semester
        {
            Code = "HK1-2526",
            Name = "Semester",
            AcademicYear = "2025-2026",
            TermNo = 1,
            StartDate = new DateTime(2025, 9, 1),
            EndDate = new DateTime(2026, 1, 15),
            Status = SemesterStatus.Active,
            CreatedBy = "seed"
        };
        dbContext.SemestersSet.Add(semester);
        await dbContext.SaveChangesAsync();

        var service = CreateSemesterService(dbContext);
        await service.DeleteAsync(new DeleteSemesterCommand { Id = semester.Id });

        var deleted = await dbContext.SemestersSet.IgnoreQueryFilters().SingleAsync(x => x.Id == semester.Id);
        Assert.True(deleted.IsDeleted);
        Assert.Equal(SemesterStatus.Inactive, deleted.Status);
        Assert.NotNull(deleted.DeletedAtUtc);
        Assert.False(await dbContext.SemestersSet.AnyAsync(x => x.Id == semester.Id));
    }

    [Fact]
    public async Task GetListAsync_ShouldExcludeDeletedRows()
    {
        await using var dbContext = CreateDbContext();
        dbContext.SemestersSet.AddRange(
            new Semester
            {
                Code = "HK1-2526",
                Name = "Semester 1",
                AcademicYear = "2025-2026",
                TermNo = 1,
                StartDate = new DateTime(2025, 9, 1),
                EndDate = new DateTime(2026, 1, 15),
                Status = SemesterStatus.Active,
                CreatedBy = "seed"
            },
            new Semester
            {
                Code = "HK2-2526",
                Name = "Semester 2",
                AcademicYear = "2025-2026",
                TermNo = 2,
                StartDate = new DateTime(2026, 1, 16),
                EndDate = new DateTime(2026, 5, 15),
                Status = SemesterStatus.Inactive,
                IsDeleted = true,
                DeletedAtUtc = new DateTime(2026, 3, 19, 0, 0, 0, DateTimeKind.Utc),
                DeletedBy = "seed",
                CreatedBy = "seed"
            });
        await dbContext.SaveChangesAsync();

        var service = CreateSemesterService(dbContext);
        var result = await service.GetListAsync(new GetSemestersQuery());

        Assert.Single(result);
        Assert.Equal("HK1-2526", result.Single().Code);
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private static SemesterService CreateSemesterService(AppDbContext dbContext)
    {
        return new SemesterService(
            dbContext,
            new AuditService(dbContext, new FakeClientContextAccessor()),
            new FakeCurrentUser(),
            new FakeDateTimeProvider());
    }

    private sealed class FakeCurrentUser : ICurrentUser
    {
        public Guid? UserId => Guid.Parse("44444444-4444-4444-4444-444444444444");
        public string? Username => "semester-admin";
        public Guid? SessionId => null;
        public bool IsAuthenticated => true;
        public IReadOnlyCollection<string> Permissions => [PermissionConstants.Semesters.View, PermissionConstants.Semesters.Create, PermissionConstants.Semesters.Edit, PermissionConstants.Semesters.Delete];
    }

    private sealed class FakeDateTimeProvider : IDateTimeProvider
    {
        public DateTime UtcNow => new(2026, 3, 19, 0, 0, 0, DateTimeKind.Utc);
    }

    private sealed class FakeClientContextAccessor : IClientContextAccessor
    {
        public string? IpAddress => "127.0.0.1";
        public string? UserAgent => "SemesterTests";
        public string ClientType => "Tests";
    }
}
