using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using UniAcademic.Application.Abstractions.StudentPortal;
using UniAcademic.Application.Common;
using UniAcademic.Application.Models.StudentPortal;
using UniAcademic.Application.Security;
using UniAcademic.Web.Helpers;

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
    public async Task<IActionResult> Index(Guid? courseOfferingId, int? page, int? pageSize, CancellationToken cancellationToken)
    {
        var offerings = await _studentPortalService.GetMyCourseOfferingsAsync(new GetMyCourseOfferingsQuery(), cancellationToken);
        var materials = await _studentPortalService.GetMyMaterialsAsync(new GetMyMaterialsQuery
        {
            CourseOfferingId = courseOfferingId
        }, cancellationToken);
        var pagedMaterials = PaginationHelper.Paginate(materials, page, pageSize);

        ViewBag.CourseOfferingId = courseOfferingId;
        ViewBag.CourseOfferingOptions = offerings
            .Select(x => new SelectListItem
            {
                Value = x.Id.ToString(),
                Text = $"{x.Code} - {x.DisplayName}",
                Selected = courseOfferingId.HasValue && x.Id == courseOfferingId.Value
            })
            .Prepend(new SelectListItem
            {
                Value = string.Empty,
                Text = "Tat ca hoc phan",
                Selected = !courseOfferingId.HasValue
            })
            .ToList();

        ViewData["Pagination"] = pagedMaterials.Pagination;
        return View(pagedMaterials.Items);
    }

    [Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.CourseMaterials.Download)]
    [HttpGet]
    public async Task<IActionResult> Download(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await using var result = await _studentPortalService.DownloadMaterialAsync(id, cancellationToken);
            return File(result.Content, result.ContentType, result.FileName);
        }
        catch (AuthException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction(nameof(Index));
        }
    }
}
