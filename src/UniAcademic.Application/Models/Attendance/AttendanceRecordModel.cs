using UniAcademic.Domain.Enums;

namespace UniAcademic.Application.Models.Attendance;

public sealed class AttendanceRecordModel
{
    public Guid Id { get; set; }

    public Guid RosterItemId { get; set; }

    public Guid StudentProfileId { get; set; }

    public string StudentCode { get; set; } = string.Empty;

    public string StudentFullName { get; set; } = string.Empty;

    public string StudentClassName { get; set; } = string.Empty;

    public AttendanceStatus Status { get; set; }

    public string? Note { get; set; }
}
