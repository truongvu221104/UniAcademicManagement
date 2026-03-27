using Microsoft.EntityFrameworkCore;
using UniAcademic.Domain.Entities.Academic;
using UniAcademic.Domain.Enums;
using UniAcademic.Infrastructure.Persistence;
using UniAcademic.Infrastructure.Persistence.SeedData;
using UniAcademic.Infrastructure.SeedData.Models;

namespace UniAcademic.Infrastructure.SeedData.Services;

public sealed class DemoLiveDatasetSynchronizer
{
    public const string DatasetName = "academic.demo-live";

    private readonly AppDbContext _dbContext;

    public DemoLiveDatasetSynchronizer(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> SynchronizeAsync(
        string filePath,
        string fileHash,
        DemoLiveSeedData data,
        CancellationToken cancellationToken = default)
    {
        var datasetState = await _dbContext.SeedDatasetStates
            .FirstOrDefaultAsync(x => x.DatasetName == DatasetName, cancellationToken);

        if (datasetState is not null && string.Equals(datasetState.FileHash, fileHash, StringComparison.Ordinal))
        {
            return false;
        }

        Validate(data);

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        var offeringsByCode = await LoadOfferingsAsync(data, cancellationToken);
        var studentsByCode = await LoadStudentsAsync(data, cancellationToken);

        foreach (var offeringSeed in data.Offerings)
        {
            var offering = offeringsByCode[NormalizeRequired(offeringSeed.CourseOfferingCode, "Offering.CourseOfferingCode")];
            await ResetOfferingLiveDataAsync(offering.Id, cancellationToken);
            await SeedOfferingLiveDataAsync(offering, offeringSeed, studentsByCode, cancellationToken);
        }

        if (datasetState is null)
        {
            datasetState = new SeedDatasetState
            {
                DatasetName = DatasetName
            };

            await _dbContext.SeedDatasetStates.AddAsync(datasetState, cancellationToken);
        }

        datasetState.FilePath = filePath;
        datasetState.FileHash = fileHash;
        datasetState.AppliedAtUtc = DateTime.UtcNow;
        datasetState.Status = "Applied";

        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    private async Task<Dictionary<string, CourseOffering>> LoadOfferingsAsync(DemoLiveSeedData data, CancellationToken cancellationToken)
    {
        var offeringCodes = data.Offerings
            .Select(x => NormalizeRequired(x.CourseOfferingCode, "Offering.CourseOfferingCode"))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var offeringsByCode = await _dbContext.CourseOfferingsSet
            .Include(x => x.Course)
            .Include(x => x.Semester)
            .Where(x => offeringCodes.Contains(x.Code))
            .ToDictionaryAsync(x => x.Code, StringComparer.OrdinalIgnoreCase, cancellationToken);

        foreach (var offeringCode in offeringCodes)
        {
            if (!offeringsByCode.ContainsKey(offeringCode))
            {
                throw new InvalidOperationException($"Demo live seed requires course offering '{offeringCode}' to exist.");
            }
        }

        return offeringsByCode;
    }

    private async Task<Dictionary<string, StudentProfile>> LoadStudentsAsync(DemoLiveSeedData data, CancellationToken cancellationToken)
    {
        var studentCodes = data.Offerings
            .SelectMany(x => x.Students)
            .Concat(data.Offerings.SelectMany(x => x.AttendanceSessions).SelectMany(x => x.Records).Select(x => x.StudentCode))
            .Concat(data.Offerings.SelectMany(x => x.GradeCategories).SelectMany(x => x.Entries).Select(x => x.StudentCode))
            .Select(x => NormalizeRequired(x, "StudentCode"))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var studentsByCode = await _dbContext.StudentProfilesSet
            .Include(x => x.StudentClass)
            .Where(x => studentCodes.Contains(x.StudentCode))
            .ToDictionaryAsync(x => x.StudentCode, StringComparer.OrdinalIgnoreCase, cancellationToken);

        foreach (var studentCode in studentCodes)
        {
            if (!studentsByCode.ContainsKey(studentCode))
            {
                throw new InvalidOperationException($"Demo live seed requires student '{studentCode}' to exist.");
            }
        }

        return studentsByCode;
    }

    private async Task ResetOfferingLiveDataAsync(Guid courseOfferingId, CancellationToken cancellationToken)
    {
        var gradeResults = await _dbContext.GradeResultsSet
            .Where(x => x.CourseOfferingId == courseOfferingId)
            .ToListAsync(cancellationToken);
        foreach (var item in gradeResults)
        {
            _dbContext.GradeResultsSet.Remove(item);
        }

        var gradeCategories = await _dbContext.GradeCategoriesSet
            .Include(x => x.Entries)
            .Where(x => x.CourseOfferingId == courseOfferingId)
            .ToListAsync(cancellationToken);
        foreach (var category in gradeCategories)
        {
            foreach (var entry in category.Entries.ToList())
            {
                _dbContext.GradeEntriesSet.Remove(entry);
            }

            _dbContext.GradeCategoriesSet.Remove(category);
        }

        var attendanceSessions = await _dbContext.AttendanceSessionsSet
            .Include(x => x.Records)
            .Where(x => x.CourseOfferingId == courseOfferingId)
            .ToListAsync(cancellationToken);
        foreach (var session in attendanceSessions)
        {
            foreach (var record in session.Records.ToList())
            {
                _dbContext.AttendanceRecordsSet.Remove(record);
            }

            _dbContext.AttendanceSessionsSet.Remove(session);
        }

        var handoffLogs = await _dbContext.ExamHandoffLogsSet
            .Where(x => x.CourseOfferingId == courseOfferingId)
            .ToListAsync(cancellationToken);
        foreach (var log in handoffLogs)
        {
            _dbContext.ExamHandoffLogsSet.Remove(log);
        }

        var snapshots = await _dbContext.CourseOfferingRosterSnapshotsSet
            .Include(x => x.Items)
            .Where(x => x.CourseOfferingId == courseOfferingId)
            .ToListAsync(cancellationToken);
        foreach (var snapshot in snapshots)
        {
            foreach (var item in snapshot.Items.ToList())
            {
                _dbContext.CourseOfferingRosterItemsSet.Remove(item);
            }

            _dbContext.CourseOfferingRosterSnapshotsSet.Remove(snapshot);
        }

        var enrollments = await _dbContext.EnrollmentsSet
            .Where(x => x.CourseOfferingId == courseOfferingId)
            .ToListAsync(cancellationToken);
        foreach (var enrollment in enrollments)
        {
            _dbContext.EnrollmentsSet.Remove(enrollment);
        }

        var offering = await _dbContext.CourseOfferingsSet.FirstAsync(x => x.Id == courseOfferingId, cancellationToken);
        offering.IsRosterFinalized = false;
        offering.RosterFinalizedAtUtc = null;
        offering.ModifiedBy = "seed-data";

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedOfferingLiveDataAsync(
        CourseOffering offering,
        DemoLiveOfferingSeedItem offeringSeed,
        IReadOnlyDictionary<string, StudentProfile> studentsByCode,
        CancellationToken cancellationToken)
    {
        var finalizedAtUtc = offeringSeed.FinalizedAtUtc.HasValue
            ? EnsureUtc(offeringSeed.FinalizedAtUtc.Value, "Offering.FinalizedAtUtc")
            : (DateTime?)null;
        var enrollmentTime = (finalizedAtUtc ?? DateTime.UtcNow).AddDays(-3);

        var enrollmentByStudentCode = new Dictionary<string, Enrollment>(StringComparer.OrdinalIgnoreCase);

        foreach (var studentCode in offeringSeed.Students.Select(x => NormalizeRequired(x, "Offering.Students")))
        {
            var student = studentsByCode[studentCode];
            var enrollment = new Enrollment
            {
                StudentProfileId = student.Id,
                CourseOfferingId = offering.Id,
                Status = EnrollmentStatus.Enrolled,
                EnrolledAtUtc = enrollmentTime,
                Note = "Demo live seed enrollment",
                CreatedBy = "seed-data"
            };

            await _dbContext.EnrollmentsSet.AddAsync(enrollment, cancellationToken);
            enrollmentByStudentCode[studentCode] = enrollment;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        if (!finalizedAtUtc.HasValue)
        {
            return;
        }

        var snapshot = new CourseOfferingRosterSnapshot
        {
            CourseOfferingId = offering.Id,
            FinalizedAtUtc = finalizedAtUtc.Value,
            FinalizedBy = "seed-data",
            ItemCount = offeringSeed.Students.Count,
            Note = NormalizeOptional(offeringSeed.FinalizeNote),
            CreatedBy = "seed-data"
        };

        foreach (var studentCode in offeringSeed.Students.Select(x => NormalizeRequired(x, "Offering.Students")))
        {
            var student = studentsByCode[studentCode];
            var enrollment = enrollmentByStudentCode[studentCode];
            snapshot.Items.Add(new CourseOfferingRosterItem
            {
                EnrollmentId = enrollment.Id,
                StudentProfileId = student.Id,
                StudentCode = student.StudentCode,
                StudentFullName = student.FullName,
                StudentClassName = student.StudentClass?.Name ?? string.Empty,
                CourseOfferingCode = offering.Code,
                CourseCode = offering.Course?.Code ?? string.Empty,
                CourseName = offering.Course?.Name ?? string.Empty,
                SemesterName = offering.Semester?.Name ?? string.Empty,
                CreatedBy = "seed-data"
            });
        }

        await _dbContext.CourseOfferingRosterSnapshotsSet.AddAsync(snapshot, cancellationToken);

        offering.IsRosterFinalized = true;
        offering.RosterFinalizedAtUtc = finalizedAtUtc.Value;
        offering.ModifiedBy = "seed-data";

        await _dbContext.SaveChangesAsync(cancellationToken);

        var rosterItemsByStudentCode = snapshot.Items.ToDictionary(x => x.StudentCode, StringComparer.OrdinalIgnoreCase);

        foreach (var sessionSeed in offeringSeed.AttendanceSessions)
        {
            var recordSeedByStudentCode = sessionSeed.Records
                .ToDictionary(
                    x => NormalizeRequired(x.StudentCode, "Attendance.Record.StudentCode"),
                    StringComparer.OrdinalIgnoreCase);

            var attendanceSession = new AttendanceSession
            {
                CourseOfferingId = offering.Id,
                CourseOfferingRosterSnapshotId = snapshot.Id,
                SessionDate = EnsureUtc(sessionSeed.SessionDate, "Attendance.SessionDate"),
                SessionNo = sessionSeed.SessionNo,
                Title = NormalizeOptional(sessionSeed.Title),
                Note = NormalizeOptional(sessionSeed.Note),
                CreatedBy = "seed-data"
            };

            foreach (var studentCode in offeringSeed.Students.Select(x => NormalizeRequired(x, "Offering.Students")))
            {
                recordSeedByStudentCode.TryGetValue(studentCode, out var recordSeed);
                attendanceSession.Records.Add(new AttendanceRecord
                {
                    RosterItemId = rosterItemsByStudentCode[studentCode].Id,
                    Status = recordSeed is null
                        ? GenerateDefaultAttendanceStatus(studentCode, sessionSeed.SessionNo)
                        : ParseEnum<AttendanceStatus>(recordSeed.Status, "Attendance.Record.Status"),
                    Note = NormalizeOptional(recordSeed?.Note),
                    CreatedBy = "seed-data"
                });
            }

            await _dbContext.AttendanceSessionsSet.AddAsync(attendanceSession, cancellationToken);
        }

        var activeCategories = new List<GradeCategory>();
        foreach (var categorySeed in offeringSeed.GradeCategories.OrderBy(x => x.OrderIndex))
        {
            var entrySeedByStudentCode = categorySeed.Entries
                .ToDictionary(
                    x => NormalizeRequired(x.StudentCode, "GradeEntry.StudentCode"),
                    StringComparer.OrdinalIgnoreCase);

            var category = new GradeCategory
            {
                CourseOfferingId = offering.Id,
                CourseOfferingRosterSnapshotId = snapshot.Id,
                Name = NormalizeRequired(categorySeed.Name, "GradeCategory.Name"),
                Weight = categorySeed.Weight,
                MaxScore = categorySeed.MaxScore,
                OrderIndex = categorySeed.OrderIndex,
                IsActive = categorySeed.IsActive,
                CreatedBy = "seed-data"
            };

            foreach (var studentCode in offeringSeed.Students.Select(x => NormalizeRequired(x, "Offering.Students")))
            {
                entrySeedByStudentCode.TryGetValue(studentCode, out var entrySeed);
                category.Entries.Add(new GradeEntry
                {
                    RosterItemId = rosterItemsByStudentCode[studentCode].Id,
                    Score = entrySeed?.Score ?? GenerateDefaultScore(studentCode, categorySeed.OrderIndex, categorySeed.MaxScore),
                    Note = NormalizeOptional(entrySeed?.Note),
                    CreatedBy = "seed-data"
                });
            }

            if (category.IsActive)
            {
                activeCategories.Add(category);
            }

            await _dbContext.GradeCategoriesSet.AddAsync(category, cancellationToken);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        if (!offeringSeed.GenerateGradeResults)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            return;
        }

        var calculatedAtUtc = finalizedAtUtc.Value.AddDays(21);
        foreach (var rosterItem in snapshot.Items.OrderBy(x => x.StudentCode))
        {
            var weightedFinalScore = CalculateWeightedFinalScore(activeCategories, rosterItem.Id);
            var passingScore = decimal.Round(offeringSeed.PassingScore, 4, MidpointRounding.AwayFromZero);
            await _dbContext.GradeResultsSet.AddAsync(new GradeResult
            {
                CourseOfferingId = offering.Id,
                CourseOfferingRosterSnapshotId = snapshot.Id,
                RosterItemId = rosterItem.Id,
                WeightedFinalScore = weightedFinalScore,
                PassingScore = passingScore,
                IsPassed = weightedFinalScore >= passingScore,
                CalculatedAtUtc = calculatedAtUtc,
                CalculatedBy = "seed-data",
                CreatedBy = "seed-data"
            }, cancellationToken);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static decimal CalculateWeightedFinalScore(IReadOnlyCollection<GradeCategory> categories, Guid rosterItemId)
    {
        decimal total = 0m;

        foreach (var category in categories)
        {
            var entry = category.Entries.Single(x => x.RosterItemId == rosterItemId);
            var score = entry.Score ?? 0m;
            total += (score / category.MaxScore) * category.Weight;
        }

        return decimal.Round(total, 4, MidpointRounding.AwayFromZero);
    }

    private static AttendanceStatus GenerateDefaultAttendanceStatus(string studentCode, int sessionNo)
    {
        var seed = ExtractStudentSequence(studentCode) + sessionNo;
        return (seed % 6) switch
        {
            0 => AttendanceStatus.Absent,
            1 => AttendanceStatus.Present,
            2 => AttendanceStatus.Present,
            3 => AttendanceStatus.Late,
            4 => AttendanceStatus.Present,
            _ => AttendanceStatus.Excused
        };
    }

    private static decimal GenerateDefaultScore(string studentCode, int orderIndex, decimal maxScore)
    {
        var seed = ExtractStudentSequence(studentCode) + (orderIndex * 3);
        var ratio = 0.58m + ((seed % 8) * 0.045m);
        var score = maxScore * ratio;
        return decimal.Round(score, 2, MidpointRounding.AwayFromZero);
    }

    private static int ExtractStudentSequence(string studentCode)
    {
        var digits = new string(studentCode.Where(char.IsDigit).ToArray());
        return int.TryParse(digits, out var sequence) ? sequence : 0;
    }

    private static void Validate(DemoLiveSeedData data)
    {
        EnsureNoDuplicates(data.Offerings.Select(x => x.CourseOfferingCode), "Offering.CourseOfferingCode");

        foreach (var offering in data.Offerings)
        {
            EnsureNoDuplicates(offering.Students, $"Offering({offering.CourseOfferingCode}).Students");

            if (!offering.FinalizedAtUtc.HasValue
                && (offering.AttendanceSessions.Count > 0 || offering.GradeCategories.Count > 0))
            {
                throw new InvalidOperationException($"Offering({offering.CourseOfferingCode}) must define FinalizedAtUtc when attendance or grade data is provided.");
            }

            EnsureNoDuplicates(
                offering.AttendanceSessions.Select(x => $"{x.SessionDate:O}::{x.SessionNo}"),
                $"Offering({offering.CourseOfferingCode}).AttendanceSessions");
            EnsureNoDuplicates(
                offering.GradeCategories.Select(x => x.Name),
                $"Offering({offering.CourseOfferingCode}).GradeCategories");

            foreach (var session in offering.AttendanceSessions)
            {
                EnsureNoDuplicates(
                    session.Records.Select(x => x.StudentCode),
                    $"AttendanceSession({offering.CourseOfferingCode}:{session.SessionNo}).Records");
            }

            foreach (var category in offering.GradeCategories)
            {
                EnsureNoDuplicates(
                    category.Entries.Select(x => x.StudentCode),
                    $"GradeCategory({offering.CourseOfferingCode}:{category.Name}).Entries");
            }
        }
    }

    private static void EnsureNoDuplicates(IEnumerable<string> values, string fieldName)
    {
        var duplicates = values
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .GroupBy(x => x, StringComparer.OrdinalIgnoreCase)
            .Where(x => x.Count() > 1)
            .Select(x => x.Key)
            .ToList();

        if (duplicates.Count > 0)
        {
            throw new InvalidOperationException($"Demo live seed contains duplicate values for {fieldName}: {string.Join(", ", duplicates)}");
        }
    }

    private static string NormalizeRequired(string value, string fieldName)
    {
        var normalized = value.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new InvalidOperationException($"{fieldName} is required.");
        }

        return normalized;
    }

    private static string? NormalizeOptional(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim();
    }

    private static DateTime EnsureUtc(DateTime value, string fieldName)
    {
        if (value.Kind == DateTimeKind.Unspecified)
        {
            return DateTime.SpecifyKind(value, DateTimeKind.Utc);
        }

        if (value.Kind == DateTimeKind.Local)
        {
            return value.ToUniversalTime();
        }

        return value;
    }

    private static TEnum ParseEnum<TEnum>(string value, string fieldName)
        where TEnum : struct, Enum
    {
        if (Enum.TryParse<TEnum>(NormalizeRequired(value, fieldName), true, out var parsed) && Enum.IsDefined(parsed))
        {
            return parsed;
        }

        throw new InvalidOperationException($"{fieldName} value '{value}' is invalid.");
    }
}
