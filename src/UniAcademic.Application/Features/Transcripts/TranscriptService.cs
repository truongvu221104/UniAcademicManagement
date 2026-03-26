using Microsoft.EntityFrameworkCore;
using UniAcademic.Application.Abstractions.Persistence;
using UniAcademic.Application.Abstractions.Transcripts;
using UniAcademic.Application.Common;
using UniAcademic.Application.Models.Transcripts;

namespace UniAcademic.Application.Features.Transcripts;

public sealed class TranscriptService : ITranscriptService
{
    private readonly IAppDbContext _dbContext;

    public TranscriptService(IAppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<TranscriptModel> GetTranscriptAsync(GetTranscriptQuery query, CancellationToken cancellationToken = default)
    {
        if (query.StudentProfileId == Guid.Empty)
        {
            throw new AuthException("Student profile was not found.");
        }

        var student = await _dbContext.StudentProfiles
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Include(x => x.StudentClass)
                .ThenInclude(x => x!.Faculty)
            .FirstOrDefaultAsync(x => x.Id == query.StudentProfileId, cancellationToken);

        if (student is null || student.IsDeleted)
        {
            throw new AuthException("Student profile was not found.");
        }

        var results = await _dbContext.GradeResults
            .AsNoTracking()
            .Include(x => x.RosterItem)
            .Include(x => x.CourseOffering)
                .ThenInclude(x => x!.Course)
            .Include(x => x.CourseOffering)
                .ThenInclude(x => x!.Semester)
            .Where(x => x.RosterItem != null && x.RosterItem.StudentProfileId == query.StudentProfileId)
            .Select(x => new TranscriptProjection
            {
                CourseName = x.CourseOffering != null && x.CourseOffering.Course != null ? x.CourseOffering.Course.Name : string.Empty,
                CourseCode = x.CourseOffering != null && x.CourseOffering.Course != null ? x.CourseOffering.Course.Code : string.Empty,
                Credits = x.CourseOffering != null && x.CourseOffering.Course != null ? x.CourseOffering.Course.Credits : 0,
                WeightedFinalScore = x.WeightedFinalScore,
                IsPassed = x.IsPassed,
                SemesterName = x.CourseOffering != null && x.CourseOffering.Semester != null ? x.CourseOffering.Semester.Name : string.Empty,
                AcademicYear = x.CourseOffering != null && x.CourseOffering.Semester != null ? x.CourseOffering.Semester.AcademicYear : string.Empty,
                TermNo = x.CourseOffering != null && x.CourseOffering.Semester != null ? x.CourseOffering.Semester.TermNo : 0
            })
            .ToListAsync(cancellationToken);

        var semesterModels = results
            .GroupBy(x => new { x.SemesterName, x.AcademicYear, x.TermNo })
            .OrderBy(x => x.Key.AcademicYear)
            .ThenBy(x => x.Key.TermNo)
            .Select(group =>
            {
                var courses = group
                    .OrderBy(x => x.CourseCode)
                    .Select(x => new TranscriptCourseItemModel
                    {
                        CourseName = x.CourseName,
                        CourseCode = x.CourseCode,
                        Credits = x.Credits,
                        WeightedFinalScore = x.WeightedFinalScore,
                        GradeSymbol = MapGradeSymbol(x.WeightedFinalScore),
                        IsPassed = x.IsPassed
                    })
                    .ToList();

                return new TranscriptSemesterModel
                {
                    SemesterName = group.Key.SemesterName,
                    AcademicYear = group.Key.AcademicYear,
                    Courses = courses,
                    SemesterGPA = CalculateGpa(courses),
                    SemesterCreditsEarned = courses.Where(x => x.IsPassed).Sum(x => x.Credits)
                };
            })
            .ToList();

        var allCourses = semesterModels.SelectMany(x => x.Courses).ToList();

        return new TranscriptModel
        {
            StudentCode = student.StudentCode,
            StudentFullName = student.FullName,
            StudentClassName = student.StudentClass?.Name ?? string.Empty,
            FacultyName = student.StudentClass?.Faculty?.Name ?? string.Empty,
            Semesters = semesterModels,
            OverallGPA = CalculateGpa(allCourses),
            TotalCreditsEarned = allCourses.Where(x => x.IsPassed).Sum(x => x.Credits)
        };
    }

    private static string MapGradeSymbol(decimal score)
    {
        return score switch
        {
            >= 90m => "A",
            >= 80m => "B",
            >= 65m => "C",
            >= 50m => "D",
            _ => "F"
        };
    }

    private static decimal CalculateGpa(IEnumerable<TranscriptCourseItemModel> courses)
    {
        var materialized = courses.Where(x => x.Credits > 0).ToList();
        var totalCredits = materialized.Sum(x => x.Credits);
        if (totalCredits <= 0)
        {
            return 0m;
        }

        var totalPoints = materialized.Sum(x => x.Credits * ((x.WeightedFinalScore / 100m) * 4m));
        return Math.Round(totalPoints / totalCredits, 2, MidpointRounding.AwayFromZero);
    }

    private sealed class TranscriptProjection
    {
        public string CourseName { get; set; } = string.Empty;

        public string CourseCode { get; set; } = string.Empty;

        public int Credits { get; set; }

        public decimal WeightedFinalScore { get; set; }

        public bool IsPassed { get; set; }

        public string SemesterName { get; set; } = string.Empty;

        public string AcademicYear { get; set; } = string.Empty;

        public int TermNo { get; set; }
    }
}
