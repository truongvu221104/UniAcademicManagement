using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using UniAcademic.Application.Abstractions.LecturerPortal;
using UniAcademic.Application.Common;
using UniAcademic.Application.Models.LecturerPortal;
using UniAcademic.Application.Models.Materials;
using UniAcademic.Application.Security;
using UniAcademic.Web.Helpers;
using UniAcademic.Web.Models.Materials;

namespace UniAcademic.Web.Areas.Lecturer.Controllers;

[Area("Lecturer")]
[Authorize]
public sealed class LecturerMaterialsController : Controller
{
    private readonly ILecturerPortalService _lecturerPortalService;

    public LecturerMaterialsController(ILecturerPortalService lecturerPortalService)
    {
        _lecturerPortalService = lecturerPortalService;
    }

    [Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.CourseMaterials.View)]
    [HttpGet]
    public async Task<IActionResult> Index(Guid? courseOfferingId, int? page, int? pageSize, CancellationToken cancellationToken)
    {
        await LoadOfferingOptionsAsync(cancellationToken, courseOfferingId, includeEmpty: true);
        var materials = await _lecturerPortalService.GetCourseMaterialsAsync(new GetLecturerCourseMaterialsQuery
        {
            CourseOfferingId = courseOfferingId
        }, cancellationToken);
        var pagedMaterials = PaginationHelper.Paginate(materials, page, pageSize);
        ViewData["Pagination"] = pagedMaterials.Pagination;
        return View(pagedMaterials.Items);
    }

    [Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.CourseMaterials.View)]
    [HttpGet]
    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var material = await _lecturerPortalService.GetCourseMaterialByIdAsync(id, cancellationToken);
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
    public async Task<IActionResult> Create(Guid? courseOfferingId, CancellationToken cancellationToken)
    {
        await LoadOfferingOptionsAsync(cancellationToken, courseOfferingId, includeEmpty: false);
        return View(new UploadCourseMaterialViewModel
        {
            CourseOfferingId = courseOfferingId ?? Guid.Empty
        });
    }

    [Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.CourseMaterials.Create)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(UploadCourseMaterialViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            await LoadOfferingOptionsAsync(cancellationToken, model.CourseOfferingId, includeEmpty: false);
            return View(model);
        }

        try
        {
            await using var stream = model.File!.OpenReadStream();
            var result = await _lecturerPortalService.UploadCourseMaterialAsync(new UploadCourseMaterialCommand
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
            await LoadOfferingOptionsAsync(cancellationToken, model.CourseOfferingId, includeEmpty: false);
            return View(model);
        }
    }

    [Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.CourseMaterials.Edit)]
    [HttpGet]
    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var material = await _lecturerPortalService.GetCourseMaterialByIdAsync(id, cancellationToken);
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
            await _lecturerPortalService.UpdateCourseMaterialAsync(new UpdateCourseMaterialCommand
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
            await _lecturerPortalService.SetCourseMaterialPublishStateAsync(new SetCourseMaterialPublishStateCommand
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
            await using var result = await _lecturerPortalService.DownloadCourseMaterialAsync(id, cancellationToken);
            return File(result.Content, result.ContentType, result.FileName);
        }
        catch (AuthException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction(nameof(Index));
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
}
