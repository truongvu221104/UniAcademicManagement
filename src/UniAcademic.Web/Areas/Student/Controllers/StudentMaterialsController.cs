using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniAcademic.Application.Abstractions.StudentPortal;
using UniAcademic.Application.Common;
using UniAcademic.Application.Models.Materials;
using UniAcademic.Application.Models.StudentPortal;
using UniAcademic.Application.Security;
using UniAcademic.Web.Models.StudentPortal;

namespace UniAcademic.Web.Areas.Student.Controllers;

[Area("Student")]
[Authorize]
public sealed class StudentMaterialsController : Controller
{
    private readonly IStudentPortalService _studentPortalService;

    public StudentMaterialsController(IStudentPortalService studentPortalService)
    {
        _studentPortalService = studentPortalService;
    }

    [Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.CourseMaterials.View)]
    [HttpGet]
    public async Task<IActionResult> Index(string? keyword, CancellationToken cancellationToken)
    {
        var offerings = await _studentPortalService.GetMyCourseOfferingsAsync(new GetMyCourseOfferingsQuery
        {
            Keyword = keyword
        }, cancellationToken);
        var materials = await _studentPortalService.GetMyMaterialsAsync(new GetMyMaterialsQuery
        {
        }, cancellationToken);

        var groupedMaterials = materials
            .GroupBy(x => x.CourseOfferingId)
            .ToDictionary(x => x.Key, x => x.ToList());

        var overview = offerings
            .Select(x =>
            {
                groupedMaterials.TryGetValue(x.Id, out var items);
                items ??= new List<CourseMaterialListItemModel>();

                return new StudentMaterialsOverviewItemViewModel
                {
                    CourseOfferingId = x.Id,
                    CourseOfferingCode = x.Code,
                    CourseName = x.CourseName,
                    SemesterName = x.SemesterName,
                    MaterialCount = items.Count,
                    LatestUploadedAtUtc = items.Count == 0 ? null : items.Max(y => y.UploadedAtUtc)
                };
            })
            .ToList();

        ViewBag.Keyword = keyword;
        return View(overview);
    }

    [Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.CourseMaterials.View)]
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

        var materials = await _studentPortalService.GetMyMaterialsAsync(new GetMyMaterialsQuery
        {
            CourseOfferingId = courseOfferingId
        }, cancellationToken);

        var model = new StudentMaterialDetailsViewModel
        {
            CourseOfferingId = offering.Id,
            CourseOfferingCode = offering.Code,
            CourseName = offering.CourseName,
            SemesterName = offering.SemesterName,
            Materials = materials
        };

        return View(model);
    }

    [Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.CourseMaterials.Download)]
    [HttpGet]
    public async Task<IActionResult> Download(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _studentPortalService.DownloadMaterialAsync(id, cancellationToken);
            HttpContext.Response.RegisterForDispose(result.Content);
            return File(result.Content, result.ContentType, result.FileName);
        }
        catch (AuthException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction(nameof(Index));
        }
    }
}
