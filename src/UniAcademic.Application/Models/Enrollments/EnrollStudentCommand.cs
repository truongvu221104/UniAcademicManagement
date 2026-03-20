namespace UniAcademic.Application.Models.Enrollments;

public sealed class EnrollStudentCommand
{
    public Guid StudentProfileId { get; set; }

    public Guid CourseOfferingId { get; set; }

    public string? Note { get; set; }
}
