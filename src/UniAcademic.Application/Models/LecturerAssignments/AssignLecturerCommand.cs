namespace UniAcademic.Application.Models.LecturerAssignments;

public sealed class AssignLecturerCommand
{
    public Guid CourseOfferingId { get; set; }

    public Guid LecturerProfileId { get; set; }

    public bool IsPrimary { get; set; }
}
