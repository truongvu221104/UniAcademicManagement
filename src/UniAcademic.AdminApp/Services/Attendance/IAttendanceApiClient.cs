using UniAcademic.Contracts.Attendance;

namespace UniAcademic.AdminApp.Services.Attendance;

public interface IAttendanceApiClient
{
    Task<IReadOnlyCollection<AttendanceSessionListItemResponse>> GetListAsync(Guid? courseOfferingId = null, CancellationToken cancellationToken = default);

    Task<AttendanceSessionResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<AttendanceSessionResponse> CreateAsync(CreateAttendanceSessionRequest request, CancellationToken cancellationToken = default);

    Task<AttendanceSessionResponse> UpdateRecordsAsync(Guid id, UpdateAttendanceRecordsRequest request, CancellationToken cancellationToken = default);
}
