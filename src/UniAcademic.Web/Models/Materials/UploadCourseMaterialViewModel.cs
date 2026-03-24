using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using UniAcademic.Domain.Enums;

namespace UniAcademic.Web.Models.Materials;

public sealed class UploadCourseMaterialViewModel
{
    [Required]
    public Guid CourseOfferingId { get; set; }

    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    [Required]
    public CourseMaterialType MaterialType { get; set; } = CourseMaterialType.Document;

    [Range(0, int.MaxValue)]
    public int SortOrder { get; set; }

    public bool IsPublished { get; set; }

    [Required]
    public IFormFile? File { get; set; }
}
