using UniAcademic.Application.Models.StudentPortal;

namespace UniAcademic.Web.Models.StudentPortal;

public sealed class StudentAttendanceDetailsViewModel
{
    public Guid CourseOfferingId { get; set; }

    public string CourseOfferingCode { get; set; } = string.Empty;

    public string CourseName { get; set; } = string.Empty;

    public string SemesterName { get; set; } = string.Empty;

    public IReadOnlyCollection<StudentAttendanceItemModel> Sessions { get; set; } = [];
}
