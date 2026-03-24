using Microsoft.EntityFrameworkCore;
using UniAcademic.Application.Abstractions.Auth;
using UniAcademic.Application.Abstractions.Common;
using UniAcademic.Application.Abstractions.Grades;
using UniAcademic.Application.Abstractions.Persistence;
using UniAcademic.Application.Common;
using UniAcademic.Application.Models.Grades;
using UniAcademic.Domain.Entities.Academic;

namespace UniAcademic.Application.Features.Grades;

public sealed class GradeService : IGradeService
{
    private readonly IAppDbContext _dbContext;
    private readonly IAuditService _auditService;
    private readonly ICurrentUser _currentUser;

    public GradeService(
        IAppDbContext dbContext,
        IAuditService auditService,
        ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _auditService = auditService;
        _currentUser = currentUser;
    }

    public async Task<GradeCategoryModel> CreateCategoryAsync(CreateGradeCategoryCommand command, CancellationToken cancellationToken = default)
    {
        var courseOffering = await RequireCourseOfferingAsync(command.CourseOfferingId, cancellationToken);
        EnsureRosterFinalized(courseOffering);
        var rosterSnapshot = await RequireRosterSnapshotAsync(courseOffering.Id, cancellationToken);
        var name = NormalizeName(command.Name);
        var weight = NormalizeWeight(command.Weight);
        var maxScore = NormalizeMaxScore(command.MaxScore);
        var orderIndex = NormalizeOrderIndex(command.OrderIndex);

        await EnsureCategoryNameUniqueAsync(courseOffering.Id, null, name, cancellationToken);
        await EnsureTotalWeightWithinLimitAsync(courseOffering.Id, null, weight, command.IsActive, cancellationToken);

        var createdBy = _currentUser.Username ?? "system";
        var category = new GradeCategory
        {
            CourseOfferingId = courseOffering.Id,
            CourseOfferingRosterSnapshotId = rosterSnapshot.Id,
            Name = name,
            Weight = weight,
            MaxScore = maxScore,
            OrderIndex = orderIndex,
            IsActive = command.IsActive,
            CreatedBy = createdBy
        };

        foreach (var rosterItem in rosterSnapshot.Items.OrderBy(x => x.StudentCode))
        {
            category.Entries.Add(new GradeEntry
            {
                RosterItemId = rosterItem.Id,
                Score = null,
                Note = null,
                CreatedBy = createdBy
            });
        }

        await _dbContext.AddAsync(category, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await _auditService.WriteAsync("gradecategory.create", nameof(GradeCategory), category.Id.ToString(), new
        {
            category.CourseOfferingId,
            category.Name,
            category.Weight,
            category.MaxScore
        }, _currentUser.UserId, cancellationToken);

        return await GetByIdAsync(new GetGradeCategoryByIdQuery { Id = category.Id }, cancellationToken);
    }

    public async Task<GradeCategoryModel> UpdateCategoryAsync(UpdateGradeCategoryCommand command, CancellationToken cancellationToken = default)
    {
        var category = await BuildCategoryQuery()
            .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken)
            ?? throw new AuthException("Grade category was not found.");

        var courseOffering = await RequireCourseOfferingAsync(category.CourseOfferingId, cancellationToken);
        EnsureRosterFinalized(courseOffering);
        await RequireRosterSnapshotAsync(courseOffering.Id, cancellationToken);

        var name = NormalizeName(command.Name);
        var weight = NormalizeWeight(command.Weight);
        var maxScore = NormalizeMaxScore(command.MaxScore);
        var orderIndex = NormalizeOrderIndex(command.OrderIndex);

        await EnsureCategoryNameUniqueAsync(category.CourseOfferingId, category.Id, name, cancellationToken);
        await EnsureTotalWeightWithinLimitAsync(category.CourseOfferingId, category.Id, weight, command.IsActive, cancellationToken);

        if (category.Entries.Any(x => x.Score.HasValue && x.Score.Value > maxScore))
        {
            throw new AuthException("Existing grade score exceeds max score.");
        }

        category.Name = name;
        category.Weight = weight;
        category.MaxScore = maxScore;
        category.OrderIndex = orderIndex;
        category.IsActive = command.IsActive;
        category.ModifiedBy = _currentUser.Username ?? "system";

        await _dbContext.SaveChangesAsync(cancellationToken);
        await _auditService.WriteAsync("gradecategory.update", nameof(GradeCategory), category.Id.ToString(), new
        {
            category.CourseOfferingId,
            category.Name,
            category.Weight,
            category.MaxScore,
            category.IsActive
        }, _currentUser.UserId, cancellationToken);

        return Map(category);
    }

    public async Task<GradeCategoryModel> UpdateEntriesAsync(UpdateGradeEntriesCommand command, CancellationToken cancellationToken = default)
    {
        var category = await BuildCategoryQuery()
            .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken)
            ?? throw new AuthException("Grade category was not found.");

        var courseOffering = await RequireCourseOfferingAsync(category.CourseOfferingId, cancellationToken);
        EnsureRosterFinalized(courseOffering);

        if (command.Entries.Count == 0)
        {
            throw new AuthException("Grade entries are required.");
        }

        var duplicateRosterItem = command.Entries
            .GroupBy(x => x.RosterItemId)
            .FirstOrDefault(x => x.Count() > 1);

        if (duplicateRosterItem is not null)
        {
            throw new AuthException("Grade entries contain duplicates.");
        }

        var entriesByRosterItemId = category.Entries.ToDictionary(x => x.RosterItemId);
        var modifiedBy = _currentUser.Username ?? "system";

        foreach (var item in command.Entries)
        {
            if (item.RosterItemId == Guid.Empty)
            {
                throw new AuthException("Roster item is required.");
            }

            if (!entriesByRosterItemId.TryGetValue(item.RosterItemId, out var entry))
            {
                throw new AuthException("Grade entry does not belong to the category snapshot.");
            }

            entry.Score = NormalizeScore(item.Score, category.MaxScore);
            entry.Note = NormalizeNote(item.Note);
            entry.ModifiedBy = modifiedBy;
        }

        category.ModifiedBy = modifiedBy;

        await _dbContext.SaveChangesAsync(cancellationToken);
        await _auditService.WriteAsync("gradeentries.update", nameof(GradeCategory), category.Id.ToString(), new
        {
            category.CourseOfferingId,
            category.Name,
            UpdatedCount = command.Entries.Count
        }, _currentUser.UserId, cancellationToken);

        return Map(category);
    }

    public async Task<GradeCategoryModel> GetByIdAsync(GetGradeCategoryByIdQuery query, CancellationToken cancellationToken = default)
    {
        var category = await BuildCategoryQuery()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == query.Id, cancellationToken)
            ?? throw new AuthException("Grade category was not found.");

        return Map(category);
    }

    public async Task<IReadOnlyCollection<GradeCategoryListItemModel>> GetListAsync(GetGradeCategoriesQuery query, CancellationToken cancellationToken = default)
    {
        var categories = _dbContext.GradeCategories
            .AsNoTracking()
            .Include(x => x.CourseOffering)
                .ThenInclude(x => x!.Course)
            .Include(x => x.CourseOffering)
                .ThenInclude(x => x!.Semester)
            .Include(x => x.Entries)
            .AsQueryable();

        if (query.CourseOfferingId.HasValue)
        {
            categories = categories.Where(x => x.CourseOfferingId == query.CourseOfferingId.Value);
        }

        return await categories
            .OrderBy(x => x.OrderIndex)
            .ThenBy(x => x.Name)
            .Select(x => new GradeCategoryListItemModel
            {
                Id = x.Id,
                CourseOfferingId = x.CourseOfferingId,
                CourseOfferingCode = x.CourseOffering != null ? x.CourseOffering.Code : string.Empty,
                CourseName = x.CourseOffering != null && x.CourseOffering.Course != null ? x.CourseOffering.Course.Name : string.Empty,
                SemesterName = x.CourseOffering != null && x.CourseOffering.Semester != null ? x.CourseOffering.Semester.Name : string.Empty,
                Name = x.Name,
                Weight = x.Weight,
                MaxScore = x.MaxScore,
                OrderIndex = x.OrderIndex,
                IsActive = x.IsActive,
                EntryCount = x.Entries.Count
            })
            .ToListAsync(cancellationToken);
    }

    private IQueryable<GradeCategory> BuildCategoryQuery()
    {
        return _dbContext.GradeCategories
            .Include(x => x.CourseOffering)
                .ThenInclude(x => x!.Course)
            .Include(x => x.CourseOffering)
                .ThenInclude(x => x!.Semester)
            .Include(x => x.Entries)
                .ThenInclude(x => x.RosterItem);
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

    private async Task<CourseOfferingRosterSnapshot> RequireRosterSnapshotAsync(Guid courseOfferingId, CancellationToken cancellationToken)
    {
        var snapshot = await _dbContext.CourseOfferingRosterSnapshots
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.CourseOfferingId == courseOfferingId, cancellationToken);

        if (snapshot is null)
        {
            throw new AuthException("Course offering roster snapshot was not found.");
        }

        return snapshot;
    }

    private async Task EnsureCategoryNameUniqueAsync(Guid courseOfferingId, Guid? categoryId, string name, CancellationToken cancellationToken)
    {
        var exists = await _dbContext.GradeCategories
            .AnyAsync(
                x => x.CourseOfferingId == courseOfferingId
                    && x.Id != categoryId
                    && x.Name.ToLower() == name.ToLower(),
                cancellationToken);

        if (exists)
        {
            throw new AuthException("Grade category name already exists.");
        }
    }

    private async Task EnsureTotalWeightWithinLimitAsync(Guid courseOfferingId, Guid? categoryId, decimal weight, bool isActive, CancellationToken cancellationToken)
    {
        if (!isActive)
        {
            return;
        }

        var existingActiveWeight = await _dbContext.GradeCategories
            .Where(x => x.CourseOfferingId == courseOfferingId && x.Id != categoryId && x.IsActive)
            .SumAsync(x => x.Weight, cancellationToken);

        if (existingActiveWeight + weight > 100m)
        {
            throw new AuthException("Total active grade weight exceeds 100.");
        }
    }

    private static void EnsureRosterFinalized(CourseOffering courseOffering)
    {
        if (!courseOffering.IsRosterFinalized)
        {
            throw new AuthException("Course offering roster was not finalized.");
        }
    }

    private static string NormalizeName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new AuthException("Grade category name is required.");
        }

        var normalized = name.Trim();
        if (normalized.Length > 200)
        {
            throw new AuthException("Grade category name is invalid.");
        }

        return normalized;
    }

    private static decimal NormalizeWeight(decimal weight)
    {
        if (weight <= 0)
        {
            throw new AuthException("Grade weight is invalid.");
        }

        return decimal.Round(weight, 2, MidpointRounding.AwayFromZero);
    }

    private static decimal NormalizeMaxScore(decimal maxScore)
    {
        if (maxScore <= 0)
        {
            throw new AuthException("Grade max score is invalid.");
        }

        return decimal.Round(maxScore, 2, MidpointRounding.AwayFromZero);
    }

    private static int NormalizeOrderIndex(int orderIndex)
    {
        if (orderIndex < 0)
        {
            throw new AuthException("Grade order index is invalid.");
        }

        return orderIndex;
    }

    private static decimal? NormalizeScore(decimal? score, decimal maxScore)
    {
        if (!score.HasValue)
        {
            return null;
        }

        var normalized = decimal.Round(score.Value, 2, MidpointRounding.AwayFromZero);
        if (normalized < 0 || normalized > maxScore)
        {
            throw new AuthException("Grade score is invalid.");
        }

        return normalized;
    }

    private static string? NormalizeNote(string? note)
    {
        if (string.IsNullOrWhiteSpace(note))
        {
            return null;
        }

        var normalized = note.Trim();
        if (normalized.Length > 1000)
        {
            throw new AuthException("Grade note is invalid.");
        }

        return normalized;
    }

    private static GradeCategoryModel Map(GradeCategory category)
    {
        return new GradeCategoryModel
        {
            Id = category.Id,
            CourseOfferingId = category.CourseOfferingId,
            CourseOfferingCode = category.CourseOffering?.Code ?? string.Empty,
            CourseName = category.CourseOffering?.Course?.Name ?? string.Empty,
            SemesterName = category.CourseOffering?.Semester?.Name ?? string.Empty,
            CourseOfferingRosterSnapshotId = category.CourseOfferingRosterSnapshotId,
            Name = category.Name,
            Weight = category.Weight,
            MaxScore = category.MaxScore,
            OrderIndex = category.OrderIndex,
            IsActive = category.IsActive,
            EntryCount = category.Entries.Count,
            Entries = category.Entries
                .OrderBy(x => x.RosterItem != null ? x.RosterItem.StudentCode : string.Empty)
                .Select(x => new GradeEntryModel
                {
                    Id = x.Id,
                    RosterItemId = x.RosterItemId,
                    StudentProfileId = x.RosterItem?.StudentProfileId ?? Guid.Empty,
                    StudentCode = x.RosterItem?.StudentCode ?? string.Empty,
                    StudentFullName = x.RosterItem?.StudentFullName ?? string.Empty,
                    StudentClassName = x.RosterItem?.StudentClassName ?? string.Empty,
                    Score = x.Score,
                    Note = x.Note
                })
                .ToList()
        };
    }
}
