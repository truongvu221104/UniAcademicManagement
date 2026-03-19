using UniAcademic.Contracts.StudentClasses;

namespace UniAcademic.AdminApp.Services.StudentClasses;

public interface IStudentClassApiClient
{
    Task<IReadOnlyCollection<StudentClassListItemResponse>> GetListAsync(string? keyword = null, Guid? facultyId = null, int? intakeYear = null, string? status = null, CancellationToken cancellationToken = default);

    Task<StudentClassResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<StudentClassResponse> CreateAsync(CreateStudentClassRequest request, CancellationToken cancellationToken = default);

    Task<StudentClassResponse> UpdateAsync(Guid id, UpdateStudentClassRequest request, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
