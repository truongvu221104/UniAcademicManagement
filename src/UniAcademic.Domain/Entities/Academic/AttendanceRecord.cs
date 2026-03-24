using UniAcademic.Domain.Enums;
using UniAcademic.SharedKernel;

namespace UniAcademic.Domain.Entities.Academic;

public sealed class AttendanceRecord : AuditableEntity, IAuditableEntity
{
    public Guid AttendanceSessionId { get; set; }

    public Guid RosterItemId { get; set; }

    public AttendanceStatus Status { get; set; } = AttendanceStatus.Unmarked;

    public string? Note { get; set; }

    public byte[] RowVersion { get; set; } = [];

    public AttendanceSession? AttendanceSession { get; set; }

    public CourseOfferingRosterItem? RosterItem { get; set; }
}
