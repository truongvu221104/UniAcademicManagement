using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using UniAcademic.Application.Abstractions.LecturerPortal;
using UniAcademic.Application.Common;
using UniAcademic.Application.Models.Attendance;
using UniAcademic.Application.Models.LecturerPortal;
using UniAcademic.Application.Security;
using UniAcademic.Web.Helpers;
using UniAcademic.Web.Models.Attendance;
using UniAcademic.Web.Models.LecturerPortal;

namespace UniAcademic.Web.Areas.Lecturer.Controllers;

[Area("Lecturer")]
[Authorize]
public sealed class LecturerAttendanceController : Controller
{
    private readonly ILecturerPortalService _lecturerPortalService;

    public LecturerAttendanceController(ILecturerPortalService lecturerPortalService)
    {
        _lecturerPortalService = lecturerPortalService;
    }

    [Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.Attendance.View)]
    [HttpGet]
    public async Task<IActionResult> Index(string? keyword, int? pageNumber, int? pageSize, CancellationToken cancellationToken)
    {
        var offerings = await _lecturerPortalService.GetMyTeachingOfferingsAsync(new GetMyTeachingOfferingsQuery
        {
            Keyword = keyword
        }, cancellationToken);
        var sessions = await _lecturerPortalService.GetAttendanceSessionsAsync(new GetLecturerAttendanceSessionsQuery(), cancellationToken);
        var sessionGroups = sessions
            .GroupBy(x => x.CourseOfferingId)
            .ToDictionary(
                x => x.Key,
                x => new
                {
                    Count = x.Count(),
                    LatestSessionDate = x.Max(item => item.SessionDate)
                });

        var overview = offerings.Select(x =>
        {
            var hasSessions = sessionGroups.TryGetValue(x.Id, out var group);
            return new LecturerAttendanceOfferingOverviewItemViewModel
            {
                CourseOfferingId = x.Id,
                CourseOfferingCode = x.Code,
                DisplayName = x.DisplayName,
                CourseCode = x.CourseCode,
                CourseName = x.CourseName,
                SemesterName = x.SemesterName,
                Capacity = x.Capacity,
                SessionCount = hasSessions ? group!.Count : 0,
                LatestSessionDate = hasSessions ? group!.LatestSessionDate : null
            };
        }).ToList();

        var pagedOverview = PaginationHelper.Paginate(overview, pageNumber, pageSize);
        ViewBag.Keyword = keyword;
        ViewData["Pagination"] = pagedOverview.Pagination;
        return View(pagedOverview.Items);
    }

    [Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.Attendance.View)]
    [HttpGet]
    public async Task<IActionResult> Sessions(Guid courseOfferingId, int? pageNumber, int? pageSize, CancellationToken cancellationToken)
    {
        var offerings = await _lecturerPortalService.GetMyTeachingOfferingsAsync(new GetMyTeachingOfferingsQuery(), cancellationToken);
        var selectedOffering = offerings.FirstOrDefault(x => x.Id == courseOfferingId);
        if (selectedOffering is null)
        {
            TempData["ErrorMessage"] = "The selected teaching offering could not be found.";
            return RedirectToAction(nameof(Index));
        }

        var sessions = await _lecturerPortalService.GetAttendanceSessionsAsync(new GetLecturerAttendanceSessionsQuery
        {
            CourseOfferingId = courseOfferingId
        }, cancellationToken);
        var pagedSessions = PaginationHelper.Paginate(sessions, pageNumber, pageSize);

        ViewBag.CourseOfferingId = courseOfferingId;
        ViewBag.SelectedOffering = selectedOffering;
        ViewData["Pagination"] = pagedSessions.Pagination;
        return View(pagedSessions.Items);
    }

    [Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.Attendance.View)]
    [HttpGet]
    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var session = await _lecturerPortalService.GetAttendanceSessionByIdAsync(id, cancellationToken);
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
    public async Task<IActionResult> Create(Guid? courseOfferingId, CancellationToken cancellationToken)
    {
        await LoadOfferingOptionsAsync(cancellationToken, courseOfferingId, includeEmpty: false);
        return View(new CreateAttendanceSessionViewModel
        {
            CourseOfferingId = courseOfferingId ?? Guid.Empty
        });
    }

    [Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.Attendance.Create)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateAttendanceSessionViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            await LoadOfferingOptionsAsync(cancellationToken, model.CourseOfferingId, includeEmpty: false);
            return View(model);
        }

        try
        {
            var result = await _lecturerPortalService.CreateAttendanceSessionAsync(new CreateAttendanceSessionCommand
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
            await LoadOfferingOptionsAsync(cancellationToken, model.CourseOfferingId, includeEmpty: false);
            return View(model);
        }
    }

    [Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.Attendance.Edit)]
    [HttpGet]
    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var session = await _lecturerPortalService.GetAttendanceSessionByIdAsync(id, cancellationToken);
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
            await _lecturerPortalService.UpdateAttendanceRecordsAsync(new UpdateAttendanceRecordsCommand
            {
                Id = model.Id,
                SessionNo = model.SessionNo,
                Title = model.Title,
                Note = model.Note,
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

    private async Task LoadOfferingOptionsAsync(CancellationToken cancellationToken, Guid? selectedCourseOfferingId, bool includeEmpty)
    {
        var offerings = await _lecturerPortalService.GetMyTeachingOfferingsAsync(new GetMyTeachingOfferingsQuery(), cancellationToken);
        var options = offerings.Select(x => new SelectListItem
        {
            Value = x.Id.ToString(),
            Text = $"{x.Code} - {x.DisplayName}",
            Selected = selectedCourseOfferingId.HasValue && x.Id == selectedCourseOfferingId.Value
        }).ToList();

        if (includeEmpty)
        {
            options.Insert(0, new SelectListItem
            {
                Value = string.Empty,
                Text = "Tat ca hoc phan",
                Selected = !selectedCourseOfferingId.HasValue
            });
        }

        ViewBag.CourseOfferingOptions = options;
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
            Records = model.Records.Select(x => new UpdateAttendanceRecordItemViewModel
            {
                RosterItemId = x.RosterItemId,
                StudentCode = x.StudentCode,
                StudentFullName = x.StudentFullName,
                StudentClassName = x.StudentClassName,
                Status = x.Status,
                Note = x.Note
            }).ToList()
        };
    }
}
