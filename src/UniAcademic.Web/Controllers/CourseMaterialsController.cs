using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using UniAcademic.Application.Abstractions.CourseOfferings;
using UniAcademic.Application.Abstractions.Materials;
using UniAcademic.Application.Common;
using UniAcademic.Application.Models.CourseOfferings;
using UniAcademic.Application.Models.Materials;
using UniAcademic.Application.Security;
using UniAcademic.Web.Helpers;
using UniAcademic.Web.Models.Materials;

namespace UniAcademic.Web.Controllers;

[Authorize(Roles = RoleConstants.AcademicManagement)]
public sealed class CourseMaterialsController : Controller
{
    private readonly ICourseMaterialService _courseMaterialService;
    private readonly ICourseOfferingService _courseOfferingService;

    public CourseMaterialsController(
        ICourseMaterialService courseMaterialService,
        ICourseOfferingService courseOfferingService)
    {
        _courseMaterialService = courseMaterialService;
        _courseOfferingService = courseOfferingService;
    }

    [Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.CourseMaterials.View)]
    [HttpGet]
    public async Task<IActionResult> Index(Guid? courseOfferingId, int? page, int? pageSize, CancellationToken cancellationToken)
    {
        var materials = await _courseMaterialService.GetListAsync(new GetCourseMaterialsQuery
        {
            CourseOfferingId = courseOfferingId
        }, cancellationToken);
        var pagedMaterials = PaginationHelper.Paginate(materials, page, pageSize);

        ViewBag.CourseOfferingId = courseOfferingId;
        ViewBag.CourseOfferingOptions = await BuildCourseOfferingOptionsAsync(cancellationToken, courseOfferingId, includeEmpty: true);
        ViewData["Pagination"] = pagedMaterials.Pagination;
        return View(pagedMaterials.Items);
    }

    [Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.CourseMaterials.View)]
    [HttpGet]
    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var material = await _courseMaterialService.GetByIdAsync(new GetCourseMaterialByIdQuery { Id = id }, cancellationToken);
            return View(material);
        }
        catch (AuthException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction(nameof(Index));
        }
    }

    [Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.CourseMaterials.Create)]
    [HttpGet]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        ViewBag.CourseOfferingOptions = await BuildCourseOfferingOptionsAsync(cancellationToken, null, includeEmpty: false);
        return View(new UploadCourseMaterialViewModel());
    }

    [Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.CourseMaterials.Create)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(UploadCourseMaterialViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.CourseOfferingOptions = await BuildCourseOfferingOptionsAsync(cancellationToken, model.CourseOfferingId, includeEmpty: false);
            return View(model);
        }

        try
        {
            await using var stream = model.File!.OpenReadStream();
            var result = await _courseMaterialService.UploadAsync(new UploadCourseMaterialCommand
            {
                CourseOfferingId = model.CourseOfferingId,
                Title = model.Title,
                Description = model.Description,
                MaterialType = model.MaterialType,
                SortOrder = model.SortOrder,
                IsPublished = model.IsPublished,
                OriginalFileName = model.File.FileName,
                ContentType = model.File.ContentType,
                SizeInBytes = model.File.Length,
                FileContent = stream
            }, cancellationToken);

            return RedirectToAction(nameof(Details), new { id = result.Id });
        }
        catch (AuthException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            ViewBag.CourseOfferingOptions = await BuildCourseOfferingOptionsAsync(cancellationToken, model.CourseOfferingId, includeEmpty: false);
            return View(model);
        }
    }

    [Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.CourseMaterials.Edit)]
    [HttpGet]
    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var material = await _courseMaterialService.GetByIdAsync(new GetCourseMaterialByIdQuery { Id = id }, cancellationToken);
            return View(new UpdateCourseMaterialViewModel
            {
                Id = material.Id,
                CourseOfferingId = material.CourseOfferingId,
                CourseOfferingCode = material.CourseOfferingCode,
                CourseName = material.CourseName,
                SemesterName = material.SemesterName,
                OriginalFileName = material.OriginalFileName,
                ContentType = material.ContentType,
                SizeInBytes = material.SizeInBytes,
                Title = material.Title,
                Description = material.Description,
                MaterialType = material.MaterialType,
                SortOrder = material.SortOrder,
                IsPublished = material.IsPublished
            });
        }
        catch (AuthException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction(nameof(Index));
        }
    }

    [Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.CourseMaterials.Edit)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(UpdateCourseMaterialViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            await _courseMaterialService.UpdateAsync(new UpdateCourseMaterialCommand
            {
                Id = model.Id,
                Title = model.Title,
                Description = model.Description,
                MaterialType = model.MaterialType,
                SortOrder = model.SortOrder
            }, cancellationToken);

            return RedirectToAction(nameof(Details), new { id = model.Id });
        }
        catch (AuthException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
    }

    [Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.CourseMaterials.Edit)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetPublishState(Guid id, bool isPublished, CancellationToken cancellationToken)
    {
        try
        {
            await _courseMaterialService.SetPublishStateAsync(new SetCourseMaterialPublishStateCommand
            {
                Id = id,
                IsPublished = isPublished
            }, cancellationToken);

            return RedirectToAction(nameof(Edit), new { id });
        }
        catch (AuthException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction(nameof(Index));
        }
    }

    [Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.CourseMaterials.Download)]
    [HttpGet]
    public async Task<IActionResult> Download(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await using var result = await _courseMaterialService.DownloadAsync(new DownloadCourseMaterialQuery { Id = id }, cancellationToken);
            return File(result.Content, result.ContentType, result.FileName);
        }
        catch (AuthException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction(nameof(Index));
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
}
