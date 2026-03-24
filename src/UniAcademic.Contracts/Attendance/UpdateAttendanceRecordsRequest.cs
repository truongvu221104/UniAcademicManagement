using UniAcademic.Domain.Enums;

namespace UniAcademic.Contracts.Attendance;

public sealed class UpdateAttendanceRecordsRequest
{
    public IReadOnlyCollection<UpdateAttendanceRecordItemRequest> Records { get; set; } = [];
}

public sealed class UpdateAttendanceRecordItemRequest
{
    public Guid RosterItemId { get; set; }

    public AttendanceStatus Status { get; set; }

    public string? Note { get; set; }
}
