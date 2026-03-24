namespace UniAcademic.Application.Models.Attendance;

public sealed class CreateAttendanceSessionCommand
{
    public Guid CourseOfferingId { get; set; }

    public DateTime SessionDate { get; set; }

    public int SessionNo { get; set; }

    public string? Title { get; set; }

    public string? Note { get; set; }
}
