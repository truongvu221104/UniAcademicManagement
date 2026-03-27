using Microsoft.EntityFrameworkCore;
using UniAcademic.Application.Abstractions.Auth;
using UniAcademic.Application.Abstractions.Common;
using UniAcademic.Application.Abstractions.GradeResults;
using UniAcademic.Application.Abstractions.Persistence;
using UniAcademic.Application.Common;
using UniAcademic.Application.Models.GradeResults;
using UniAcademic.Domain.Entities.Academic;

namespace UniAcademic.Application.Features.GradeResults;

public sealed class GradeResultService : IGradeResultService
{
    private readonly IAppDbContext _dbContext;
    private readonly IAuditService _auditService;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public GradeResultService(
        IAppDbContext dbContext,
        IAuditService auditService,
        ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider)
    {
        _dbContext = dbContext;
        _auditService = auditService;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<IReadOnlyCollection<GradeResultModel>> CalculateAsync(CalculateGradeResultsCommand command, CancellationToken cancellationToken = default)
    {
        var courseOffering = await RequireCourseOfferingAsync(command.CourseOfferingId, cancellationToken);
        EnsureRosterFinalized(courseOffering);
        var rosterSnapshot = await RequireRosterSnapshotAsync(courseOffering.Id, cancellationToken);
        var passingScore = NormalizePassingScore(command.PassingScore);
        var activeCategories = await RequireActiveCategoriesAsync(courseOffering.Id, rosterSnapshot.Id, cancellationToken);

        EnsureActiveWeightEquals100(activeCategories);

        var rosterItemIds = rosterSnapshot.Items.Select(x => x.Id).OrderBy(x => x).ToList();
        EnsureRosterCoverage(activeCategories, rosterItemIds);

        var now = _dateTimeProvider.UtcNow;
        var calculatedBy = _currentUser.Username ?? "system";

        var existingResults = await _dbContext.GradeResults
            .Where(x => x.CourseOfferingId == courseOffering.Id)
            .ToListAsync(cancellationToken);
        var existingByRosterItemId = existingResults.ToDictionary(x => x.RosterItemId);
        var hadExisting = existingResults.Count > 0;

        foreach (var rosterItem in rosterSnapshot.Items.OrderBy(x => x.StudentCode))
        {
            var weightedFinalScore = CalculateWeightedFinalScore(activeCategories, rosterItem.Id);
            var isPassed = weightedFinalScore >= passingScore;

            if (existingByRosterItemId.TryGetValue(rosterItem.Id, out var existing))
            {
                existing.CourseOfferingRosterSnapshotId = rosterSnapshot.Id;
                existing.WeightedFinalScore = weightedFinalScore;
                existing.PassingScore = passingScore;
                existing.IsPassed = isPassed;
                existing.CalculatedAtUtc = now;
                existing.CalculatedBy = calculatedBy;
                existing.ModifiedBy = calculatedBy;
            }
            else
            {
                await _dbContext.AddAsync(new GradeResult
                {
                    CourseOfferingId = courseOffering.Id,
                    CourseOfferingRosterSnapshotId = rosterSnapshot.Id,
                    RosterItemId = rosterItem.Id,
                    WeightedFinalScore = weightedFinalScore,
                    PassingScore = passingScore,
                    IsPassed = isPassed,
                    CalculatedAtUtc = now,
                    CalculatedBy = calculatedBy,
                    CreatedBy = calculatedBy
                }, cancellationToken);
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        await _auditService.WriteAsync(
            hadExisting ? "graderesult.recalculate" : "graderesult.calculate",
            nameof(GradeResult),
            courseOffering.Id.ToString(),
            new
            {
                courseOffering.Id,
                RosterSnapshotId = rosterSnapshot.Id,
                PassingScore = passingScore,
                ResultCount = rosterItemIds.Count
            },
            _currentUser.UserId,
            cancellationToken);

        return await GetResultsForOfferingAsync(courseOffering.Id, cancellationToken);
    }

    public async Task<GradeResultModel> GetByIdAsync(GetGradeResultByIdQuery query, CancellationToken cancellationToken = default)
    {
        var result = await BuildQuery()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == query.Id, cancellationToken)
            ?? throw new AuthException("Grade result was not found.");

        return Map(result);
    }

    public async Task<IReadOnlyCollection<GradeResultListItemModel>> GetListAsync(GetGradeResultsQuery query, CancellationToken cancellationToken = default)
    {
        var results = _dbContext.GradeResults
            .AsNoTracking()
            .Include(x => x.CourseOffering)
                .ThenInclude(x => x!.Course)
            .Include(x => x.CourseOffering)
                .ThenInclude(x => x!.Semester)
            .Include(x => x.RosterItem)
            .AsQueryable();

        if (query.CourseOfferingId.HasValue)
        {
            results = results.Where(x => x.CourseOfferingId == query.CourseOfferingId.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.StudentCode))
        {
            var studentCode = query.StudentCode.Trim();
            results = results.Where(x =>
                x.RosterItem != null && x.RosterItem.StudentCode.Contains(studentCode));
        }

        if (!string.IsNullOrWhiteSpace(query.StudentFullName))
        {
            var studentFullName = query.StudentFullName.Trim();
            results = results.Where(x =>
                x.RosterItem != null && x.RosterItem.StudentFullName.Contains(studentFullName));
        }

        return await results
            .OrderBy(x => x.RosterItem!.StudentCode)
            .Select(x => new GradeResultListItemModel
            {
                Id = x.Id,
                CourseOfferingId = x.CourseOfferingId,
                CourseOfferingCode = x.CourseOffering != null ? x.CourseOffering.Code : string.Empty,
                CourseName = x.CourseOffering != null && x.CourseOffering.Course != null ? x.CourseOffering.Course.Name : string.Empty,
                SemesterName = x.CourseOffering != null && x.CourseOffering.Semester != null ? x.CourseOffering.Semester.Name : string.Empty,
                RosterItemId = x.RosterItemId,
                StudentCode = x.RosterItem != null ? x.RosterItem.StudentCode : string.Empty,
                StudentFullName = x.RosterItem != null ? x.RosterItem.StudentFullName : string.Empty,
                WeightedFinalScore = x.WeightedFinalScore,
                PassingScore = x.PassingScore,
                IsPassed = x.IsPassed,
                CalculatedAtUtc = x.CalculatedAtUtc
            })
            .ToListAsync(cancellationToken);
    }

    private IQueryable<GradeResult> BuildQuery()
    {
        return _dbContext.GradeResults
            .Include(x => x.CourseOffering)
                .ThenInclude(x => x!.Course)
            .Include(x => x.CourseOffering)
                .ThenInclude(x => x!.Semester)
            .Include(x => x.RosterItem);
    }

    private async Task<IReadOnlyCollection<GradeResultModel>> GetResultsForOfferingAsync(Guid courseOfferingId, CancellationToken cancellationToken)
    {
        var results = await BuildQuery()
            .AsNoTracking()
            .Where(x => x.CourseOfferingId == courseOfferingId)
            .OrderBy(x => x.RosterItem!.StudentCode)
            .ToListAsync(cancellationToken);

        return results.Select(Map).ToList();
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

    private async Task<List<GradeCategory>> RequireActiveCategoriesAsync(Guid courseOfferingId, Guid rosterSnapshotId, CancellationToken cancellationToken)
    {
        var categories = await _dbContext.GradeCategories
            .Include(x => x.Entries)
            .Where(x => x.CourseOfferingId == courseOfferingId && x.IsActive)
            .OrderBy(x => x.OrderIndex)
            .ThenBy(x => x.Name)
            .ToListAsync(cancellationToken);

        if (categories.Count == 0)
        {
            throw new AuthException("Active grade categories were not found.");
        }

        if (categories.Any(x => x.CourseOfferingRosterSnapshotId != rosterSnapshotId))
        {
            throw new AuthException("Grade categories do not match the current roster snapshot.");
        }

        return categories;
    }

    private static void EnsureRosterFinalized(CourseOffering courseOffering)
    {
        if (!courseOffering.IsRosterFinalized)
        {
            throw new AuthException("Course offering roster was not finalized.");
        }
    }

    private static decimal NormalizePassingScore(decimal passingScore)
    {
        if (passingScore < 0m || passingScore > 100m)
        {
            throw new AuthException("Passing score is invalid.");
        }

        return decimal.Round(passingScore, 4, MidpointRounding.AwayFromZero);
    }

    private static void EnsureActiveWeightEquals100(IReadOnlyCollection<GradeCategory> activeCategories)
    {
        var totalWeight = activeCategories.Sum(x => x.Weight);
        if (totalWeight != 100m)
        {
            throw new AuthException("Total active grade weight must equal 100.");
        }
    }

    private static void EnsureRosterCoverage(IReadOnlyCollection<GradeCategory> activeCategories, IReadOnlyCollection<Guid> rosterItemIds)
    {
        foreach (var category in activeCategories)
        {
            var entryByRosterItemId = category.Entries.ToDictionary(x => x.RosterItemId);
            foreach (var rosterItemId in rosterItemIds)
            {
                if (!entryByRosterItemId.TryGetValue(rosterItemId, out var entry))
                {
                    throw new AuthException("Grade entries do not cover the entire roster snapshot.");
                }

                if (!entry.Score.HasValue)
                {
                    throw new AuthException("Active grade scores must be fully entered before calculation.");
                }
            }

            if (entryByRosterItemId.Keys.Except(rosterItemIds).Any())
            {
                throw new AuthException("Grade entries do not match the current roster snapshot.");
            }
        }
    }

    private static decimal CalculateWeightedFinalScore(IReadOnlyCollection<GradeCategory> activeCategories, Guid rosterItemId)
    {
        decimal total = 0m;

        foreach (var category in activeCategories)
        {
            var entry = category.Entries.Single(x => x.RosterItemId == rosterItemId);
            var score = entry.Score!.Value;
            total += (score / category.MaxScore) * category.Weight;
        }

        return decimal.Round(total, 4, MidpointRounding.AwayFromZero);
    }

    private static GradeResultModel Map(GradeResult result)
    {
        return new GradeResultModel
        {
            Id = result.Id,
            CourseOfferingId = result.CourseOfferingId,
            CourseOfferingCode = result.CourseOffering?.Code ?? string.Empty,
            CourseName = result.CourseOffering?.Course?.Name ?? string.Empty,
            SemesterName = result.CourseOffering?.Semester?.Name ?? string.Empty,
            CourseOfferingRosterSnapshotId = result.CourseOfferingRosterSnapshotId,
            RosterItemId = result.RosterItemId,
            StudentProfileId = result.RosterItem?.StudentProfileId ?? Guid.Empty,
            StudentCode = result.RosterItem?.StudentCode ?? string.Empty,
            StudentFullName = result.RosterItem?.StudentFullName ?? string.Empty,
            StudentClassName = result.RosterItem?.StudentClassName ?? string.Empty,
            WeightedFinalScore = result.WeightedFinalScore,
            PassingScore = result.PassingScore,
            IsPassed = result.IsPassed,
            CalculatedAtUtc = result.CalculatedAtUtc,
            CalculatedBy = result.CalculatedBy
        };
    }
}
