namespace UniAcademic.Contracts.Attendance;

public sealed class CreateAttendanceSessionRequest
{
    public Guid CourseOfferingId { get; set; }

    public DateTime SessionDate { get; set; }

    public int SessionNo { get; set; }

    public string? Title { get; set; }

    public string? Note { get; set; }
}
