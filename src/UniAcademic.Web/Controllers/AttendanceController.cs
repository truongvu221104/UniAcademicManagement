using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using UniAcademic.Application.Abstractions.Attendance;
using UniAcademic.Application.Abstractions.CourseOfferings;
using UniAcademic.Application.Common;
using UniAcademic.Application.Models.Attendance;
using UniAcademic.Application.Models.CourseOfferings;
using UniAcademic.Application.Security;
using UniAcademic.Web.Helpers;
using UniAcademic.Web.Models.Attendance;

namespace UniAcademic.Web.Controllers;

[Authorize(Roles = RoleConstants.AcademicManagement)]
public sealed class AttendanceController : Controller
{
    private readonly IAttendanceService _attendanceService;
    private readonly ICourseOfferingService _courseOfferingService;

    public AttendanceController(
        IAttendanceService attendanceService,
        ICourseOfferingService courseOfferingService)
    {
        _attendanceService = attendanceService;
        _courseOfferingService = courseOfferingService;
    }

    [Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.Attendance.View)]
    [HttpGet]
    public async Task<IActionResult> Index(Guid? courseOfferingId, int? page, int? pageSize, CancellationToken cancellationToken)
    {
        var sessions = await _attendanceService.GetListAsync(new GetAttendanceSessionsQuery
        {
            CourseOfferingId = courseOfferingId
        }, cancellationToken);
        var pagedSessions = PaginationHelper.Paginate(sessions, page, pageSize);

        ViewBag.CourseOfferingId = courseOfferingId;
        ViewBag.CourseOfferingOptions = await BuildCourseOfferingOptionsAsync(cancellationToken, courseOfferingId, includeEmpty: true);
        ViewData["Pagination"] = pagedSessions.Pagination;
        return View(pagedSessions.Items);
    }

    [Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.Attendance.View)]
    [HttpGet]
    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var session = await _attendanceService.GetByIdAsync(new GetAttendanceSessionByIdQuery { Id = id }, cancellationToken);
            return View(session);
        }
        catch (AuthException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction(nameof(Index));
        }
    }

    [Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.Attendance.Create)]
    [HttpGet]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        ViewBag.CourseOfferingOptions = await BuildCourseOfferingOptionsAsync(cancellationToken, null, includeEmpty: false);
        return View(new CreateAttendanceSessionViewModel());
    }

    [Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.Attendance.Create)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateAttendanceSessionViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.CourseOfferingOptions = await BuildCourseOfferingOptionsAsync(cancellationToken, model.CourseOfferingId, includeEmpty: false);
            return View(model);
        }

        try
        {
            var result = await _attendanceService.CreateSessionAsync(new CreateAttendanceSessionCommand
            {
                CourseOfferingId = model.CourseOfferingId,
                SessionDate = model.SessionDate,
                SessionNo = model.SessionNo,
                Title = model.Title,
                Note = model.Note
            }, cancellationToken);

            return RedirectToAction(nameof(Edit), new { id = result.Id });
        }
        catch (AuthException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            ViewBag.CourseOfferingOptions = await BuildCourseOfferingOptionsAsync(cancellationToken, model.CourseOfferingId, includeEmpty: false);
            return View(model);
        }
    }

    [Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.Attendance.Edit)]
    [HttpGet]
    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var session = await _attendanceService.GetByIdAsync(new GetAttendanceSessionByIdQuery { Id = id }, cancellationToken);
            return View(Map(session));
        }
        catch (AuthException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction(nameof(Index));
        }
    }

    [Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.Attendance.Edit)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(UpdateAttendanceRecordsViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            await _attendanceService.UpdateRecordsAsync(new UpdateAttendanceRecordsCommand
            {
                Id = model.Id,
                Records = model.Records.Select(x => new UpdateAttendanceRecordItemCommand
                {
                    RosterItemId = x.RosterItemId,
                    Status = x.Status,
                    Note = x.Note
                }).ToList()
            }, cancellationToken);

            return RedirectToAction(nameof(Details), new { id = model.Id });
        }
        catch (AuthException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
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

    private static UpdateAttendanceRecordsViewModel Map(AttendanceSessionModel model)
    {
        return new UpdateAttendanceRecordsViewModel
        {
            Id = model.Id,
            CourseOfferingId = model.CourseOfferingId,
            CourseOfferingCode = model.CourseOfferingCode,
            CourseName = model.CourseName,
            SemesterName = model.SemesterName,
            SessionDate = model.SessionDate,
            SessionNo = model.SessionNo,
            Title = model.Title,
            Note = model.Note,
            Records = model.Records
                .Select(x => new UpdateAttendanceRecordItemViewModel
                {
                    RosterItemId = x.RosterItemId,
                    StudentCode = x.StudentCode,
                    StudentFullName = x.StudentFullName,
                    StudentClassName = x.StudentClassName,
                    Status = x.Status,
                    Note = x.Note
                })
                .ToList()
        };
    }
}
