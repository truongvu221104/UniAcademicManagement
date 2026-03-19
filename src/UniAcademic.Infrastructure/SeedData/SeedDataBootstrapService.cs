using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using UniAcademic.Infrastructure.Options;
using UniAcademic.Infrastructure.Persistence;
using UniAcademic.Infrastructure.Seed;
using UniAcademic.Infrastructure.SeedData.Models;
using UniAcademic.Infrastructure.SeedData.Services;

namespace UniAcademic.Infrastructure.SeedData;

public sealed class SeedDataBootstrapService
{
    private readonly AppDbContext _dbContext;
    private readonly AuthSeedData _authSeedData;
    private readonly SeedDataOptions _options;
    private readonly IHostEnvironment _hostEnvironment;
    private readonly JsonSeedDataFileReader _fileReader;
    private readonly DatasetHashService _hashService;
    private readonly FacultyDatasetSynchronizer _facultyDatasetSynchronizer;

    public SeedDataBootstrapService(
        AppDbContext dbContext,
        AuthSeedData authSeedData,
        IOptions<SeedDataOptions> options,
        IHostEnvironment hostEnvironment,
        JsonSeedDataFileReader fileReader,
        DatasetHashService hashService,
        FacultyDatasetSynchronizer facultyDatasetSynchronizer)
    {
        _dbContext = dbContext;
        _authSeedData = authSeedData;
        _options = options.Value;
        _hostEnvironment = hostEnvironment;
        _fileReader = fileReader;
        _hashService = hashService;
        _facultyDatasetSynchronizer = facultyDatasetSynchronizer;
    }

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        if (_options.ApplyMigrationsEnabled)
        {
            await _dbContext.Database.MigrateAsync(cancellationToken);
        }
        else if (!await _dbContext.Database.CanConnectAsync(cancellationToken))
        {
            return;
        }

        await _authSeedData.SeedAsync(cancellationToken);

        if (!_options.AutoSyncEnabled)
        {
            return;
        }

        var rootPath = ResolveRootPath();
        var facultyFilePath = Path.Combine(rootPath, "academic", "faculties.json");
        if (!File.Exists(facultyFilePath))
        {
            return;
        }

        var fileHash = await _hashService.ComputeFileHashAsync(facultyFilePath, cancellationToken);
        var items = await _fileReader.ReadListAsync<FacultySeedItem>(facultyFilePath, cancellationToken);
        await _facultyDatasetSynchronizer.SynchronizeAsync(facultyFilePath, fileHash, items, cancellationToken);
    }

    private string ResolveRootPath()
    {
        if (Path.IsPathRooted(_options.RootPath))
        {
            return _options.RootPath;
        }

        return Path.GetFullPath(Path.Combine(_hostEnvironment.ContentRootPath, _options.RootPath));
    }
}
