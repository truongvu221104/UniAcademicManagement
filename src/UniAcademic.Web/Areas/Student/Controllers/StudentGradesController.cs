using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniAcademic.Application.Abstractions.StudentPortal;
using UniAcademic.Application.Models.StudentPortal;
using UniAcademic.Application.Security;
using UniAcademic.Web.Models.StudentPortal;

namespace UniAcademic.Web.Areas.Student.Controllers;

[Area("Student")]
[Authorize]
public sealed class StudentGradesController : Controller
{
    private readonly IStudentPortalService _studentPortalService;

    public StudentGradesController(IStudentPortalService studentPortalService)
    {
        _studentPortalService = studentPortalService;
    }

    [Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.Grades.View)]
    [HttpGet]
    public async Task<IActionResult> Index(string? keyword, CancellationToken cancellationToken)
    {
        var offerings = await _studentPortalService.GetMyCourseOfferingsAsync(new GetMyCourseOfferingsQuery
        {
            Keyword = keyword
        }, cancellationToken);
        var gradeItems = await _studentPortalService.GetMyGradesAsync(new GetMyGradesQuery(), cancellationToken);
        var gradeResults = await _studentPortalService.GetMyGradeResultsAsync(new GetMyGradeResultsQuery(), cancellationToken);
        var groupedGrades = gradeItems
            .GroupBy(x => x.CourseOfferingId)
            .ToDictionary(x => x.Key, x => x.ToList());
        var groupedResults = gradeResults
            .GroupBy(x => x.CourseOfferingId)
            .ToDictionary(x => x.Key, x => x.OrderByDescending(y => y.CalculatedAtUtc).First());

        var overview = offerings
            .Select(x =>
            {
                groupedGrades.TryGetValue(x.Id, out var items);
                items ??= new List<StudentGradeItemModel>();
                var scoredItems = items.Where(y => y.Score.HasValue).ToList();
                groupedResults.TryGetValue(x.Id, out var result);

                return new StudentGradeOverviewItemViewModel
                {
                    CourseOfferingId = x.Id,
                    CourseOfferingCode = x.Code,
                    CourseName = x.CourseName,
                    SemesterName = x.SemesterName,
                    GradeItemCount = items.Count,
                    ScoredItemCount = scoredItems.Count,
                    PendingItemCount = items.Count - scoredItems.Count,
                    AverageScore = scoredItems.Count == 0 ? 0m : scoredItems.Average(y => y.Score!.Value),
                    FinalScore = result?.WeightedFinalScore,
                    PassingScore = result?.PassingScore,
                    IsPassed = result?.IsPassed,
                    CalculatedAtUtc = result?.CalculatedAtUtc
                };
            })
            .ToList();

        ViewBag.Keyword = keyword;
        return View(overview);
    }

    [Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.Grades.View)]
    [HttpGet]
    public async Task<IActionResult> Details(Guid courseOfferingId, CancellationToken cancellationToken)
    {
        var offerings = await _studentPortalService.GetMyCourseOfferingsAsync(new GetMyCourseOfferingsQuery(), cancellationToken);
        var offering = offerings.FirstOrDefault(x => x.Id == courseOfferingId);
        if (offering is null)
        {
            TempData["ErrorMessage"] = "Course offering was not found for the current student.";
            return RedirectToAction(nameof(Index));
        }

        var gradeItems = await _studentPortalService.GetMyGradesAsync(new GetMyGradesQuery
        {
            CourseOfferingId = courseOfferingId
        }, cancellationToken);
        var gradeResult = (await _studentPortalService.GetMyGradeResultsAsync(new GetMyGradeResultsQuery
        {
            CourseOfferingId = courseOfferingId
        }, cancellationToken)).OrderByDescending(x => x.CalculatedAtUtc).FirstOrDefault();

        var model = new StudentGradeDetailsViewModel
        {
            CourseOfferingId = offering.Id,
            CourseOfferingCode = offering.Code,
            CourseName = offering.CourseName,
            SemesterName = offering.SemesterName,
            GradeItems = gradeItems,
            FinalScore = gradeResult?.WeightedFinalScore,
            PassingScore = gradeResult?.PassingScore,
            IsPassed = gradeResult?.IsPassed,
            CalculatedAtUtc = gradeResult?.CalculatedAtUtc
        };

        return View(model);
    }
}
