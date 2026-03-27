using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniAcademic.Application.Abstractions.Rosters;
using UniAcademic.Application.Common;
using UniAcademic.Application.Models.Rosters;
using UniAcademic.Application.Security;

namespace UniAcademic.Web.Controllers;

[Authorize(Roles = RoleConstants.AcademicManagement)]
public sealed class CourseOfferingRostersController : Controller
{
    private readonly ICourseOfferingRosterService _courseOfferingRosterService;

    public CourseOfferingRostersController(ICourseOfferingRosterService courseOfferingRosterService)
    {
        _courseOfferingRosterService = courseOfferingRosterService;
    }

    [Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.CourseOfferingRosters.View)]
    [HttpGet]
    public async Task<IActionResult> Details(Guid courseOfferingId, CancellationToken cancellationToken)
    {
        try
        {
            var roster = await _courseOfferingRosterService.GetByCourseOfferingIdAsync(new GetCourseOfferingRosterQuery
            {
                CourseOfferingId = courseOfferingId
            }, cancellationToken);
            return View(roster);
        }
        catch (AuthException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction("Index", "CourseOfferings");
        }
    }

    [Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.CourseOfferingRosters.Finalize)]
    [HttpGet]
    public async Task<IActionResult> Finalize(Guid courseOfferingId, CancellationToken cancellationToken)
    {
        try
        {
            var roster = await _courseOfferingRosterService.GetByCourseOfferingIdAsync(new GetCourseOfferingRosterQuery
            {
                CourseOfferingId = courseOfferingId
            }, cancellationToken);
            return View(roster);
        }
        catch (AuthException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction("Index", "CourseOfferings");
        }
    }

    [Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.CourseOfferingRosters.Finalize)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Finalize(Guid courseOfferingId, string? note, CancellationToken cancellationToken)
    {
        try
        {
            await _courseOfferingRosterService.FinalizeAsync(new FinalizeCourseOfferingRosterCommand
            {
                CourseOfferingId = courseOfferingId,
                Note = note
            }, cancellationToken);

            return RedirectToAction(nameof(Details), new { courseOfferingId });
        }
        catch (AuthException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction(nameof(Finalize), new { courseOfferingId });
        }
    }
}
