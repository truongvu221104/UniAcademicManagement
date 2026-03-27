using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using UniAcademic.Application.Abstractions.CourseOfferings;
using UniAcademic.Application.Abstractions.LecturerAssignments;
using UniAcademic.Application.Abstractions.LecturerProfiles;
using UniAcademic.Application.Common;
using UniAcademic.Application.Models.CourseOfferings;
using UniAcademic.Application.Models.LecturerAssignments;
using UniAcademic.Application.Models.LecturerProfiles;
using UniAcademic.Application.Security;
using UniAcademic.Web.Helpers;
using UniAcademic.Web.Models.LecturerAssignments;

namespace UniAcademic.Web.Controllers;

[Authorize(Roles = RoleConstants.AcademicManagement)]
public sealed class LecturerAssignmentsController : Controller
{
    private readonly ILecturerAssignmentService _lecturerAssignmentService;
    private readonly ILecturerProfileService _lecturerProfileService;
    private readonly ICourseOfferingService _courseOfferingService;

    public LecturerAssignmentsController(
        ILecturerAssignmentService lecturerAssignmentService,
        ILecturerProfileService lecturerProfileService,
        ICourseOfferingService courseOfferingService)
    {
        _lecturerAssignmentService = lecturerAssignmentService;
        _lecturerProfileService = lecturerProfileService;
        _courseOfferingService = courseOfferingService;
    }

    [Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.LecturerAssignments.View)]
    [HttpGet]
    public async Task<IActionResult> Index(Guid? courseOfferingId, Guid? lecturerProfileId, int? pageNumber, int? pageSize, CancellationToken cancellationToken)
    {
        var assignments = await _lecturerAssignmentService.GetListAsync(new GetLecturerAssignmentsQuery
        {
            CourseOfferingId = courseOfferingId,
            LecturerProfileId = lecturerProfileId
        }, cancellationToken);
        var pagedAssignments = PaginationHelper.Paginate(assignments, pageNumber, pageSize);

        ViewBag.CourseOfferingId = courseOfferingId;
        ViewBag.LecturerProfileId = lecturerProfileId;
        ViewBag.CourseOfferingOptions = await BuildCourseOfferingOptionsAsync(cancellationToken, courseOfferingId, includeEmpty: true);
        ViewBag.LecturerProfileOptions = await BuildLecturerOptionsAsync(cancellationToken, lecturerProfileId, includeEmpty: true);
        ViewData["Pagination"] = pagedAssignments.Pagination;
        return View(pagedAssignments.Items);
    }

    [Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.LecturerAssignments.Assign)]
    [HttpGet]
    public async Task<IActionResult> Create(Guid? courseOfferingId, CancellationToken cancellationToken)
    {
        await LoadReferenceOptionsAsync(cancellationToken, courseOfferingId);
        return View(new AssignLecturerViewModel
        {
            CourseOfferingId = courseOfferingId ?? Guid.Empty
        });
    }

    [Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.LecturerAssignments.Assign)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AssignLecturerViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            await LoadReferenceOptionsAsync(cancellationToken, model.CourseOfferingId, model.LecturerProfileId);
            return View(model);
        }

        try
        {
            await _lecturerAssignmentService.AssignAsync(new AssignLecturerCommand
            {
                CourseOfferingId = model.CourseOfferingId,
                LecturerProfileId = model.LecturerProfileId,
                IsPrimary = model.IsPrimary
            }, cancellationToken);

            return RedirectToAction(nameof(Index), new { courseOfferingId = model.CourseOfferingId });
        }
        catch (AuthException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            await LoadReferenceOptionsAsync(cancellationToken, model.CourseOfferingId, model.LecturerProfileId);
            return View(model);
        }
    }

    [Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.LecturerAssignments.Unassign)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id, Guid? courseOfferingId, CancellationToken cancellationToken)
    {
        try
        {
            await _lecturerAssignmentService.UnassignAsync(new UnassignLecturerCommand { Id = id }, cancellationToken);
        }
        catch (AuthException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(Index), new { courseOfferingId });
    }

    private async Task LoadReferenceOptionsAsync(CancellationToken cancellationToken, Guid? selectedCourseOfferingId = null, Guid? selectedLecturerProfileId = null)
    {
        ViewBag.CourseOfferingOptions = await BuildCourseOfferingOptionsAsync(cancellationToken, selectedCourseOfferingId, includeEmpty: false);
        ViewBag.LecturerProfileOptions = await BuildLecturerOptionsAsync(cancellationToken, selectedLecturerProfileId, includeEmpty: false);
    }

    private async Task<IReadOnlyCollection<SelectListItem>> BuildCourseOfferingOptionsAsync(CancellationToken cancellationToken, Guid? selectedCourseOfferingId, bool includeEmpty)
    {
        var courseOfferings = await _courseOfferingService.GetListAsync(new GetCourseOfferingsQuery(), cancellationToken);
        var options = courseOfferings
            .Select(x => new SelectListItem
            {
                Value = x.Id.ToString(),
                Text = $"{x.Code} - {x.DisplayName}",
                Selected = selectedCourseOfferingId.HasValue && x.Id == selectedCourseOfferingId.Value
            })
            .ToList();

        if (includeEmpty)
        {
            options.Insert(0, new SelectListItem
            {
                Value = string.Empty,
                Text = "Tat ca lop hoc phan",
                Selected = !selectedCourseOfferingId.HasValue
            });
        }

        return options;
    }

    private async Task<IReadOnlyCollection<SelectListItem>> BuildLecturerOptionsAsync(CancellationToken cancellationToken, Guid? selectedLecturerProfileId, bool includeEmpty)
    {
        var lecturers = await _lecturerProfileService.GetListAsync(new GetLecturerProfilesQuery { IsActive = true }, cancellationToken);
        var options = lecturers
            .Select(x => new SelectListItem
            {
                Value = x.Id.ToString(),
                Text = $"{x.Code} - {x.FullName}",
                Selected = selectedLecturerProfileId.HasValue && x.Id == selectedLecturerProfileId.Value
            })
            .ToList();

        if (includeEmpty)
        {
            options.Insert(0, new SelectListItem
            {
                Value = string.Empty,
                Text = "Tat ca giang vien",
                Selected = !selectedLecturerProfileId.HasValue
            });
        }

        return options;
    }
}
