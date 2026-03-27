using UniAcademic.Application.Models.Materials;

namespace UniAcademic.Web.Models.StudentPortal;

public sealed class StudentMaterialDetailsViewModel
{
    public Guid CourseOfferingId { get; set; }

    public string CourseOfferingCode { get; set; } = string.Empty;

    public string CourseName { get; set; } = string.Empty;

    public string SemesterName { get; set; } = string.Empty;

    public IReadOnlyCollection<CourseMaterialListItemModel> Materials { get; set; } = [];
}
