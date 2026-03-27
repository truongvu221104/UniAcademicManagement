using UniAcademic.Application.Models.Attendance;

namespace UniAcademic.Application.Abstractions.Attendance;

public interface IAttendanceService
{
    Task<AttendanceSessionModel> CreateSessionAsync(CreateAttendanceSessionCommand command, CancellationToken cancellationToken = default);

    Task<AttendanceSessionModel> UpdateSessionAsync(UpdateAttendanceSessionCommand command, CancellationToken cancellationToken = default);

    Task<AttendanceSessionModel> UpdateRecordsAsync(UpdateAttendanceRecordsCommand command, CancellationToken cancellationToken = default);

    Task<AttendanceSessionModel> GetByIdAsync(GetAttendanceSessionByIdQuery query, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<AttendanceSessionListItemModel>> GetListAsync(GetAttendanceSessionsQuery query, CancellationToken cancellationToken = default);
}
