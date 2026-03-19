using Microsoft.EntityFrameworkCore;
using UniAcademic.Domain.Entities.Academic;
using UniAcademic.Domain.Enums;
using UniAcademic.Infrastructure.Persistence;
using UniAcademic.Infrastructure.Persistence.SeedData;
using UniAcademic.Infrastructure.SeedData.Models;

namespace UniAcademic.Infrastructure.SeedData.Services;

public sealed class FacultyDatasetSynchronizer
{
    public const string DatasetName = "academic.faculties";

    private readonly AppDbContext _dbContext;

    public FacultyDatasetSynchronizer(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> SynchronizeAsync(
        string filePath,
        string fileHash,
        IReadOnlyCollection<FacultySeedItem> items,
        CancellationToken cancellationToken = default)
    {
        var datasetState = await _dbContext.Set<SeedDatasetState>()
            .FirstOrDefaultAsync(x => x.DatasetName == DatasetName, cancellationToken);

        if (datasetState is not null && string.Equals(datasetState.FileHash, fileHash, StringComparison.Ordinal))
        {
            return false;
        }

        var normalizedCodes = items
            .Select(x => NormalizeCode(x.Code))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var facultiesByCode = await _dbContext.FacultiesSet
            .IgnoreQueryFilters()
            .Where(x => normalizedCodes.Contains(x.Code))
            .ToDictionaryAsync(x => x.Code, StringComparer.OrdinalIgnoreCase, cancellationToken);

        foreach (var item in items)
        {
            var code = NormalizeCode(item.Code);
            var name = NormalizeName(item.Name);
            var description = NormalizeDescription(item.Description);
            var status = ParseStatus(item.Status);

            if (facultiesByCode.TryGetValue(code, out var existing))
            {
                existing.Name = name;
                existing.Description = description;
                existing.Status = status;
                existing.IsDeleted = false;
                existing.DeletedAtUtc = null;
                existing.DeletedBy = null;
                existing.ModifiedBy = "seed-data";
                continue;
            }

            var faculty = new Faculty
            {
                Code = code,
                Name = name,
                Description = description,
                Status = status,
                CreatedBy = "seed-data"
            };

            await _dbContext.FacultiesSet.AddAsync(faculty, cancellationToken);
            facultiesByCode[code] = faculty;
        }

        if (datasetState is null)
        {
            datasetState = new SeedDatasetState
            {
                DatasetName = DatasetName
            };

            await _dbContext.Set<SeedDatasetState>().AddAsync(datasetState, cancellationToken);
        }

        datasetState.FilePath = filePath;
        datasetState.FileHash = fileHash;
        datasetState.AppliedAtUtc = DateTime.UtcNow;
        datasetState.Status = "Applied";

        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static string NormalizeCode(string code)
    {
        var normalized = code.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new InvalidOperationException("Faculty seed code is required.");
        }

        return normalized;
    }

    private static string NormalizeName(string name)
    {
        var normalized = name.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new InvalidOperationException("Faculty seed name is required.");
        }

        return normalized;
    }

    private static string? NormalizeDescription(string? description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return null;
        }

        return description.Trim();
    }

    private static FacultyStatus ParseStatus(string status)
    {
        if (Enum.TryParse<FacultyStatus>(status, true, out var parsed) && Enum.IsDefined(parsed))
        {
            return parsed;
        }

        throw new InvalidOperationException($"Faculty seed status '{status}' is invalid.");
    }
}
