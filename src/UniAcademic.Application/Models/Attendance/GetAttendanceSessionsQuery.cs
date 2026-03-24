namespace UniAcademic.Application.Models.Attendance;

public sealed class GetAttendanceSessionsQuery
{
    public Guid? CourseOfferingId { get; set; }
}
