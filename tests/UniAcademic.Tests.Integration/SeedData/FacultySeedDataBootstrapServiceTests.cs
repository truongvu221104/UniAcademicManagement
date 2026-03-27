using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using UniAcademic.Domain.Entities.Academic;
using UniAcademic.Domain.Entities.Identity;
using UniAcademic.Domain.Enums;
using UniAcademic.Infrastructure.Options;
using UniAcademic.Infrastructure.Persistence;
using UniAcademic.Infrastructure.Persistence.SeedData;
using UniAcademic.Infrastructure.Seed;
using UniAcademic.Infrastructure.SeedData;
using UniAcademic.Infrastructure.SeedData.Services;
using UniAcademic.Infrastructure.Services.Auth;
using Xunit;

namespace UniAcademic.Tests.Integration.SeedData;

public sealed class FacultySeedDataBootstrapServiceTests
{
    [Fact]
    public async Task RunAsync_ShouldInsertFaculties_AndTrackDatasetState_OnFirstRun()
    {
        await using var dbContext = CreateDbContext();
        var rootPath = CreateSeedRoot("""
[
  {
    "code": "CNTT",
    "name": "Cong nghe thong tin",
    "description": "Khoa CNTT",
    "status": "Active"
  },
  {
    "code": "QTKD",
    "name": "Quan tri kinh doanh",
    "description": "Khoa QTKD",
    "status": "Active"
  }
]
""");

        var service = CreateBootstrapService(dbContext, rootPath);

        await service.RunAsync();

        Assert.Equal(2, await dbContext.FacultiesSet.CountAsync());
        var datasetState = await dbContext.Set<SeedDatasetState>().SingleAsync();
        Assert.Equal(FacultyDatasetSynchronizer.DatasetName, datasetState.DatasetName);
        Assert.Equal("Applied", datasetState.Status);
    }

    [Fact]
    public async Task RunAsync_ShouldSkip_WhenDatasetHashIsUnchanged()
    {
        await using var dbContext = CreateDbContext();
        var rootPath = CreateSeedRoot("""
[
  {
    "code": "CNTT",
    "name": "Cong nghe thong tin",
    "description": "Khoa CNTT",
    "status": "Active"
  }
]
""");

        var service = CreateBootstrapService(dbContext, rootPath);

        await service.RunAsync();
        var firstState = await dbContext.Set<SeedDatasetState>().SingleAsync();
        var firstAppliedAt = firstState.AppliedAtUtc;

        await service.RunAsync();

        var secondState = await dbContext.Set<SeedDatasetState>().SingleAsync();
        Assert.Equal(1, await dbContext.FacultiesSet.CountAsync());
        Assert.Equal(firstAppliedAt, secondState.AppliedAtUtc);
    }

    [Fact]
    public async Task RunAsync_ShouldUpdateExistingFacultyByCode_AndRestoreSoftDeletedRow()
    {
        await using var dbContext = CreateDbContext();
        dbContext.FacultiesSet.Add(new Faculty
        {
            Code = "CNTT",
            Name = "Old Name",
            Description = "Old Description",
            Status = FacultyStatus.Inactive,
            IsDeleted = true,
            DeletedAtUtc = new DateTime(2026, 3, 19, 0, 0, 0, DateTimeKind.Utc),
            DeletedBy = "seed",
            CreatedBy = "seed"
        });
        await dbContext.SaveChangesAsync();

        var rootPath = CreateSeedRoot("""
[
  {
    "code": "CNTT",
    "name": "Cong nghe thong tin moi",
    "description": "Khoa CNTT moi",
    "status": "Active"
  }
]
""");

        var service = CreateBootstrapService(dbContext, rootPath);

        await service.RunAsync();

        var faculty = await dbContext.FacultiesSet.IgnoreQueryFilters().SingleAsync(x => x.Code == "CNTT");
        Assert.Equal("Cong nghe thong tin moi", faculty.Name);
        Assert.Equal("Khoa CNTT moi", faculty.Description);
        Assert.Equal(FacultyStatus.Active, faculty.Status);
        Assert.False(faculty.IsDeleted);
        Assert.Null(faculty.DeletedAtUtc);
        Assert.Equal(1, await dbContext.FacultiesSet.IgnoreQueryFilters().CountAsync(x => x.Code == "CNTT"));
    }

    [Fact]
    public async Task RunAsync_ShouldKeepExistingFaculty_WhenRecordIsRemovedFromFile()
    {
        await using var dbContext = CreateDbContext();
        var initialRootPath = CreateSeedRoot("""
[
  {
    "code": "CNTT",
    "name": "Cong nghe thong tin",
    "description": "Khoa CNTT",
    "status": "Active"
  },
  {
    "code": "QTKD",
    "name": "Quan tri kinh doanh",
    "description": "Khoa QTKD",
    "status": "Active"
  }
]
""");

        var initialService = CreateBootstrapService(dbContext, initialRootPath);
        await initialService.RunAsync();

        var updatedRootPath = CreateSeedRoot("""
[
  {
    "code": "CNTT",
    "name": "Cong nghe thong tin",
    "description": "Khoa CNTT",
    "status": "Active"
  }
]
""");

        var updatedService = CreateBootstrapService(dbContext, updatedRootPath);
        await updatedService.RunAsync();

        var cntt = await dbContext.FacultiesSet.IgnoreQueryFilters().SingleAsync(x => x.Code == "CNTT");
        var qtkt = await dbContext.FacultiesSet.IgnoreQueryFilters().SingleAsync(x => x.Code == "QTKD");

        Assert.Equal(FacultyStatus.Active, cntt.Status);
        Assert.Equal("Cong nghe thong tin", cntt.Name);

        Assert.False(qtkt.IsDeleted);
        Assert.Null(qtkt.DeletedAtUtc);
        Assert.Equal(FacultyStatus.Active, qtkt.Status);
        Assert.Equal("Quan tri kinh doanh", qtkt.Name);
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private static SeedDataBootstrapService CreateBootstrapService(AppDbContext dbContext, string rootPath)
    {
        var passwordHasher = new PasswordHasher();
        var bootstrapAdminOptions = Options.Create(new BootstrapAdminOptions
        {
            Username = "admin",
            Email = "admin@uniacademic.local",
            DisplayName = "System Administrator",
            Password = "Admin@123456"
        });

        return new SeedDataBootstrapService(
            dbContext,
            new AuthSeedData(dbContext, passwordHasher, bootstrapAdminOptions),
            Options.Create(new SeedDataOptions
            {
                ApplyMigrationsEnabled = false,
                AutoSyncEnabled = true,
                RootPath = rootPath
            }),
            new FakeHostEnvironment(),
            new JsonSeedDataFileReader(),
            new DatasetHashService(),
            new FacultyDatasetSynchronizer(dbContext),
            new DemoFoundationDatasetSynchronizer(dbContext, passwordHasher),
            new DemoLiveDatasetSynchronizer(dbContext));
    }

    private static string CreateSeedRoot(string facultiesJson)
    {
        var rootPath = Path.Combine(Path.GetTempPath(), "UniAcademicSeedTests", Guid.NewGuid().ToString("N"));
        var academicPath = Path.Combine(rootPath, "academic");
        Directory.CreateDirectory(academicPath);
        File.WriteAllText(Path.Combine(academicPath, "faculties.json"), facultiesJson);
        return rootPath;
    }

    private sealed class FakeHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Testing";
        public string ApplicationName { get; set; } = "UniAcademic.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } = default!;
    }
}
