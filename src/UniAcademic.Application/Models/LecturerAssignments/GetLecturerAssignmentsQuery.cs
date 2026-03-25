namespace UniAcademic.Application.Models.LecturerAssignments;

public sealed class GetLecturerAssignmentsQuery
{
    public Guid? CourseOfferingId { get; set; }

    public Guid? LecturerProfileId { get; set; }
}
