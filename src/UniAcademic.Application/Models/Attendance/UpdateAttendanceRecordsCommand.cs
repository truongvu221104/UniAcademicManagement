using UniAcademic.Domain.Enums;

namespace UniAcademic.Application.Models.Attendance;

public sealed class UpdateAttendanceRecordsCommand
{
    public Guid Id { get; set; }

    public int SessionNo { get; set; }

    public string? Title { get; set; }

    public string? Note { get; set; }

    public IReadOnlyCollection<UpdateAttendanceRecordItemCommand> Records { get; set; } = [];
}

public sealed class UpdateAttendanceRecordItemCommand
{
    public Guid RosterItemId { get; set; }

    public AttendanceStatus Status { get; set; }

    public string? Note { get; set; }
}
