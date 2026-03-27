namespace UniAcademic.Application.Models.Attendance;

public sealed class UpdateAttendanceSessionCommand
{
    public Guid Id { get; set; }

    public int SessionNo { get; set; }

    public string? Title { get; set; }

    public string? Note { get; set; }
}
