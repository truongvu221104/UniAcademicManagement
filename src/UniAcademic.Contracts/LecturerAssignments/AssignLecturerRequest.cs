namespace UniAcademic.Contracts.LecturerAssignments;

public sealed class AssignLecturerRequest
{
    public Guid CourseOfferingId { get; set; }

    public Guid LecturerProfileId { get; set; }

    public bool IsPrimary { get; set; }
}
