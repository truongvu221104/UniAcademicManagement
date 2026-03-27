using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using UniAcademic.Application.Abstractions.CourseOfferings;
using UniAcademic.Application.Abstractions.Enrollments;
using UniAcademic.Application.Abstractions.StudentProfiles;
using UniAcademic.Application.Common;
using UniAcademic.Application.Models.CourseOfferings;
using UniAcademic.Application.Models.Enrollments;
using UniAcademic.Application.Models.StudentProfiles;
using UniAcademic.Application.Security;
using UniAcademic.Domain.Enums;
using UniAcademic.Web.Models.Enrollments;

namespace UniAcademic.Web.Controllers;

[Authorize(Roles = RoleConstants.AcademicManagement)]
public sealed class EnrollmentsController : Controller
{
    private readonly IEnrollmentService _enrollmentService;
    private readonly IStudentProfileService _studentProfileService;
    private readonly ICourseOfferingService _courseOfferingService;

    public EnrollmentsController(
        IEnrollmentService enrollmentService,
        IStudentProfileService studentProfileService,
        ICourseOfferingService courseOfferingService)
    {
        _enrollmentService = enrollmentService;
        _studentProfileService = studentProfileService;
        _courseOfferingService = courseOfferingService;
    }

    [Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.Enrollments.View)]
    [HttpGet]
    public async Task<IActionResult> Index(string? keyword, Guid? studentProfileId, Guid? courseOfferingId, EnrollmentStatus? status, int? page, int? pageSize, CancellationToken cancellationToken)
    {
        var enrollments = await _enrollmentService.GetListAsync(new GetEnrollmentsQuery
        {
            Keyword = keyword,
            StudentProfileId = studentProfileId,
            CourseOfferingId = courseOfferingId,
            Status = status
        }, cancellationToken);
        var pagedEnrollments = UniAcademic.Web.Helpers.PaginationHelper.Paginate(enrollments, page, pageSize);

        ViewBag.Keyword = keyword;
        ViewBag.StudentProfileId = studentProfileId;
        ViewBag.CourseOfferingId = courseOfferingId;
        ViewBag.Status = status;
        ViewBag.StudentProfileOptions = await BuildStudentProfileOptionsAsync(cancellationToken, studentProfileId, includeEmpty: true);
        ViewBag.CourseOfferingOptions = await BuildCourseOfferingOptionsAsync(cancellationToken, courseOfferingId, includeEmpty: true);

        ViewData["Pagination"] = pagedEnrollments.Pagination;
        return View(pagedEnrollments.Items);
    }

    [Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.Enrollments.View)]
    [HttpGet]
    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var enrollment = await _enrollmentService.GetByIdAsync(new GetEnrollmentByIdQuery { Id = id }, cancellationToken);
            return View(enrollment);
        }
        catch (AuthException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction(nameof(Index));
        }
    }

    [Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.Enrollments.Create)]
    [HttpGet]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        await LoadReferenceOptionsAsync(cancellationToken);
        return View(new CreateEnrollmentViewModel());
    }

    [Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.Enrollments.Create)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateEnrollmentViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            await LoadReferenceOptionsAsync(cancellationToken, model.StudentProfileId, model.CourseOfferingId);
            return View(model);
        }

        try
        {
            await _enrollmentService.EnrollAsync(new EnrollStudentCommand
            {
                StudentProfileId = model.StudentProfileId,
                CourseOfferingId = model.CourseOfferingId,
                Note = model.Note
            }, cancellationToken);

            return RedirectToAction(nameof(Index));
        }
        catch (AuthException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            await LoadReferenceOptionsAsync(cancellationToken, model.StudentProfileId, model.CourseOfferingId);
            return View(model);
        }
    }

    [Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.Enrollments.Delete)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await _enrollmentService.DropAsync(new DropEnrollmentCommand { Id = id }, cancellationToken);
        }
        catch (AuthException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task LoadReferenceOptionsAsync(CancellationToken cancellationToken, Guid? selectedStudentProfileId = null, Guid? selectedCourseOfferingId = null)
    {
        ViewBag.StudentProfileOptions = await BuildStudentProfileOptionsAsync(cancellationToken, selectedStudentProfileId, includeEmpty: false);
        ViewBag.CourseOfferingOptions = await BuildCourseOfferingOptionsAsync(cancellationToken, selectedCourseOfferingId, includeEmpty: false);
    }

    private async Task<IReadOnlyCollection<SelectListItem>> BuildStudentProfileOptionsAsync(CancellationToken cancellationToken, Guid? selectedStudentProfileId, bool includeEmpty)
    {
        var studentProfiles = await _studentProfileService.GetListAsync(new GetStudentProfilesQuery(), cancellationToken);
        var options = studentProfiles
            .Select(x => new SelectListItem
            {
                Value = x.Id.ToString(),
                Text = $"{x.StudentCode} - {x.FullName}",
                Selected = selectedStudentProfileId.HasValue && x.Id == selectedStudentProfileId.Value
            })
            .ToList();

        if (includeEmpty)
        {
            options.Insert(0, new SelectListItem
            {
                Value = string.Empty,
                Text = "Tat ca sinh vien",
                Selected = !selectedStudentProfileId.HasValue
            });
        }

        return options;
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
}
