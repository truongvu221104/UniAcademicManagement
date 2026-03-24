using Microsoft.EntityFrameworkCore;
using UniAcademic.Application.Abstractions.Auth;
using UniAcademic.Application.Abstractions.Common;
using UniAcademic.Application.Abstractions.Materials;
using UniAcademic.Application.Abstractions.Persistence;
using UniAcademic.Application.Abstractions.Storage;
using UniAcademic.Application.Common;
using UniAcademic.Application.Models.Materials;
using UniAcademic.Domain.Entities.Academic;
using UniAcademic.Domain.Enums;

namespace UniAcademic.Application.Features.Materials;

public sealed class CourseMaterialService : ICourseMaterialService
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".doc", ".docx", ".ppt", ".pptx", ".xls", ".xlsx", ".zip"
    };

    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "application/pdf",
        "application/msword",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "application/vnd.ms-powerpoint",
        "application/vnd.openxmlformats-officedocument.presentationml.presentation",
        "application/vnd.ms-excel",
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        "application/zip",
        "application/x-zip-compressed",
        "application/octet-stream"
    };

    private readonly IAppDbContext _dbContext;
    private readonly IAuditService _auditService;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILocalFileStorage _localFileStorage;

    public CourseMaterialService(
        IAppDbContext dbContext,
        IAuditService auditService,
        ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider,
        ILocalFileStorage localFileStorage)
    {
        _dbContext = dbContext;
        _auditService = auditService;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
        _localFileStorage = localFileStorage;
    }

    public async Task<CourseMaterialModel> UploadAsync(UploadCourseMaterialCommand command, CancellationToken cancellationToken = default)
    {
        var courseOffering = await RequireCourseOfferingAsync(command.CourseOfferingId, cancellationToken);
        var title = NormalizeTitle(command.Title);
        var description = NormalizeDescription(command.Description);
        var materialType = NormalizeMaterialType(command.MaterialType);
        var sortOrder = NormalizeSortOrder(command.SortOrder);
        var originalFileName = NormalizeOriginalFileName(command.OriginalFileName);
        var contentType = NormalizeContentType(command.ContentType);

        EnsureFileProvided(command.FileContent, command.SizeInBytes);
        EnsureFileTypeAllowed(originalFileName, contentType);
        EnsureFileSizeAllowed(command.SizeInBytes);

        var createdBy = _currentUser.Username ?? "system";
        var storedFile = await _localFileStorage.SaveAsync(new LocalFileSaveRequest
        {
            CourseOfferingId = courseOffering.Id,
            OriginalFileName = originalFileName,
            Content = command.FileContent
        }, cancellationToken);

        var uploadedAtUtc = _dateTimeProvider.UtcNow;
        var fileMetadata = new FileMetadata
        {
            OriginalFileName = originalFileName,
            RelativePath = storedFile.RelativePath,
            ContentType = contentType,
            SizeInBytes = command.SizeInBytes,
            UploadedAtUtc = uploadedAtUtc,
            UploadedBy = createdBy,
            CreatedBy = createdBy
        };

        var material = new CourseMaterial
        {
            CourseOfferingId = courseOffering.Id,
            FileMetadata = fileMetadata,
            Title = title,
            Description = description,
            MaterialType = materialType,
            SortOrder = sortOrder,
            IsPublished = command.IsPublished,
            CreatedBy = createdBy
        };

        await _dbContext.AddAsync(material, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await _auditService.WriteAsync("coursematerial.upload", nameof(CourseMaterial), material.Id.ToString(), new
        {
            material.CourseOfferingId,
            material.Title,
            material.MaterialType,
            fileMetadata.OriginalFileName,
            fileMetadata.SizeInBytes
        }, _currentUser.UserId, cancellationToken);

        return await GetByIdAsync(new GetCourseMaterialByIdQuery { Id = material.Id }, cancellationToken);
    }

    public async Task<CourseMaterialModel> UpdateAsync(UpdateCourseMaterialCommand command, CancellationToken cancellationToken = default)
    {
        var material = await BuildQuery()
            .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken)
            ?? throw new AuthException("Course material was not found.");

        await RequireCourseOfferingAsync(material.CourseOfferingId, cancellationToken);

        material.Title = NormalizeTitle(command.Title);
        material.Description = NormalizeDescription(command.Description);
        material.MaterialType = NormalizeMaterialType(command.MaterialType);
        material.SortOrder = NormalizeSortOrder(command.SortOrder);
        material.ModifiedBy = _currentUser.Username ?? "system";

        await _dbContext.SaveChangesAsync(cancellationToken);
        await _auditService.WriteAsync("coursematerial.update", nameof(CourseMaterial), material.Id.ToString(), new
        {
            material.CourseOfferingId,
            material.Title,
            material.MaterialType,
            material.SortOrder
        }, _currentUser.UserId, cancellationToken);

        return Map(material);
    }

    public async Task<CourseMaterialModel> SetPublishStateAsync(SetCourseMaterialPublishStateCommand command, CancellationToken cancellationToken = default)
    {
        var material = await BuildQuery()
            .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken)
            ?? throw new AuthException("Course material was not found.");

        await RequireCourseOfferingAsync(material.CourseOfferingId, cancellationToken);

        material.IsPublished = command.IsPublished;
        material.ModifiedBy = _currentUser.Username ?? "system";

        await _dbContext.SaveChangesAsync(cancellationToken);
        await _auditService.WriteAsync(command.IsPublished ? "coursematerial.publish" : "coursematerial.unpublish",
            nameof(CourseMaterial),
            material.Id.ToString(),
            new
            {
                material.CourseOfferingId,
                material.IsPublished
            },
            _currentUser.UserId,
            cancellationToken);

        return Map(material);
    }

    public async Task<CourseMaterialModel> GetByIdAsync(GetCourseMaterialByIdQuery query, CancellationToken cancellationToken = default)
    {
        var material = await BuildQuery()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == query.Id, cancellationToken)
            ?? throw new AuthException("Course material was not found.");

        return Map(material);
    }

    public async Task<IReadOnlyCollection<CourseMaterialListItemModel>> GetListAsync(GetCourseMaterialsQuery query, CancellationToken cancellationToken = default)
    {
        var materials = _dbContext.CourseMaterials
            .AsNoTracking()
            .Include(x => x.CourseOffering)
                .ThenInclude(x => x!.Course)
            .Include(x => x.CourseOffering)
                .ThenInclude(x => x!.Semester)
            .Include(x => x.FileMetadata)
            .AsQueryable();

        if (query.CourseOfferingId.HasValue)
        {
            materials = materials.Where(x => x.CourseOfferingId == query.CourseOfferingId.Value);
        }

        return await materials
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Title)
            .Select(x => new CourseMaterialListItemModel
            {
                Id = x.Id,
                CourseOfferingId = x.CourseOfferingId,
                CourseOfferingCode = x.CourseOffering != null ? x.CourseOffering.Code : string.Empty,
                CourseName = x.CourseOffering != null && x.CourseOffering.Course != null ? x.CourseOffering.Course.Name : string.Empty,
                SemesterName = x.CourseOffering != null && x.CourseOffering.Semester != null ? x.CourseOffering.Semester.Name : string.Empty,
                Title = x.Title,
                MaterialType = x.MaterialType,
                SortOrder = x.SortOrder,
                IsPublished = x.IsPublished,
                OriginalFileName = x.FileMetadata != null ? x.FileMetadata.OriginalFileName : string.Empty,
                ContentType = x.FileMetadata != null ? x.FileMetadata.ContentType : string.Empty,
                SizeInBytes = x.FileMetadata != null ? x.FileMetadata.SizeInBytes : 0,
                UploadedAtUtc = x.FileMetadata != null ? x.FileMetadata.UploadedAtUtc : DateTime.MinValue
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<FileDownloadModel> DownloadAsync(DownloadCourseMaterialQuery query, CancellationToken cancellationToken = default)
    {
        var material = await BuildQuery()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == query.Id, cancellationToken)
            ?? throw new AuthException("Course material was not found.");

        var relativePath = material.FileMetadata?.RelativePath;
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            throw new AuthException("Material file was not found on disk.");
        }

        if (!await _localFileStorage.ExistsAsync(relativePath, cancellationToken))
        {
            throw new AuthException("Material file was not found on disk.");
        }

        return new FileDownloadModel
        {
            FileName = material.FileMetadata!.OriginalFileName,
            ContentType = material.FileMetadata.ContentType,
            Content = await _localFileStorage.OpenReadAsync(relativePath, cancellationToken)
        };
    }

    private IQueryable<CourseMaterial> BuildQuery()
    {
        return _dbContext.CourseMaterials
            .Include(x => x.CourseOffering)
                .ThenInclude(x => x!.Course)
            .Include(x => x.CourseOffering)
                .ThenInclude(x => x!.Semester)
            .Include(x => x.FileMetadata);
    }

    private async Task<CourseOffering> RequireCourseOfferingAsync(Guid courseOfferingId, CancellationToken cancellationToken)
    {
        if (courseOfferingId == Guid.Empty)
        {
            throw new AuthException("Course offering is required.");
        }

        var courseOffering = await _dbContext.CourseOfferings
            .IgnoreQueryFilters()
            .Include(x => x.Course)
            .Include(x => x.Semester)
            .FirstOrDefaultAsync(x => x.Id == courseOfferingId, cancellationToken);

        if (courseOffering is null || courseOffering.IsDeleted)
        {
            throw new AuthException("Course offering was not found.");
        }

        return courseOffering;
    }

    private static void EnsureFileProvided(Stream? fileContent, long sizeInBytes)
    {
        if (fileContent is null || fileContent == Stream.Null || sizeInBytes <= 0)
        {
            throw new AuthException("Material file is required.");
        }
    }

    private void EnsureFileSizeAllowed(long sizeInBytes)
    {
        if (sizeInBytes > _localFileStorage.MaxFileSizeInBytes)
        {
            throw new AuthException("Material file exceeds the allowed size limit.");
        }
    }

    private static void EnsureFileTypeAllowed(string originalFileName, string contentType)
    {
        var extension = Path.GetExtension(originalFileName);
        if (string.IsNullOrWhiteSpace(extension) || !AllowedExtensions.Contains(extension))
        {
            throw new AuthException("Material file type is not allowed.");
        }

        if (!AllowedContentTypes.Contains(contentType))
        {
            throw new AuthException("Material file type is not allowed.");
        }
    }

    private static string NormalizeTitle(string? title)
    {
        var normalized = title?.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new AuthException("Title is required.");
        }

        if (normalized.Length > 200)
        {
            throw new AuthException("Title is too long.");
        }

        return normalized;
    }

    private static string? NormalizeDescription(string? description)
    {
        var normalized = description?.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return null;
        }

        if (normalized.Length > 1000)
        {
            throw new AuthException("Description is too long.");
        }

        return normalized;
    }

    private static CourseMaterialType NormalizeMaterialType(CourseMaterialType materialType)
    {
        if (!Enum.IsDefined(materialType))
        {
            throw new AuthException("Material type is invalid.");
        }

        return materialType;
    }

    private static int NormalizeSortOrder(int sortOrder)
    {
        if (sortOrder < 0)
        {
            throw new AuthException("Sort order is invalid.");
        }

        return sortOrder;
    }

    private static string NormalizeOriginalFileName(string? originalFileName)
    {
        var normalized = Path.GetFileName(originalFileName?.Trim());
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new AuthException("Material file is required.");
        }

        if (normalized.Length > 260)
        {
            throw new AuthException("Material file name is too long.");
        }

        return normalized;
    }

    private static string NormalizeContentType(string? contentType)
    {
        var normalized = contentType?.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new AuthException("Material content type is required.");
        }

        return normalized;
    }

    private static CourseMaterialModel Map(CourseMaterial material)
    {
        return new CourseMaterialModel
        {
            Id = material.Id,
            CourseOfferingId = material.CourseOfferingId,
            CourseOfferingCode = material.CourseOffering?.Code ?? string.Empty,
            CourseName = material.CourseOffering?.Course?.Name ?? string.Empty,
            SemesterName = material.CourseOffering?.Semester?.Name ?? string.Empty,
            FileMetadataId = material.FileMetadataId,
            Title = material.Title,
            Description = material.Description,
            MaterialType = material.MaterialType,
            SortOrder = material.SortOrder,
            IsPublished = material.IsPublished,
            OriginalFileName = material.FileMetadata?.OriginalFileName ?? string.Empty,
            ContentType = material.FileMetadata?.ContentType ?? string.Empty,
            SizeInBytes = material.FileMetadata?.SizeInBytes ?? 0,
            UploadedAtUtc = material.FileMetadata?.UploadedAtUtc ?? DateTime.MinValue,
            UploadedBy = material.FileMetadata?.UploadedBy ?? string.Empty
        };
    }
}
