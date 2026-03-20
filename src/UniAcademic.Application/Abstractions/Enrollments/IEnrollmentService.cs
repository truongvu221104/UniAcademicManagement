using UniAcademic.Application.Models.Enrollments;

namespace UniAcademic.Application.Abstractions.Enrollments;

public interface IEnrollmentService
{
    Task<EnrollmentModel> EnrollAsync(EnrollStudentCommand command, CancellationToken cancellationToken = default);

    Task DropAsync(DropEnrollmentCommand command, CancellationToken cancellationToken = default);

    Task<EnrollmentModel> GetByIdAsync(GetEnrollmentByIdQuery query, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<EnrollmentListItemModel>> GetListAsync(GetEnrollmentsQuery query, CancellationToken cancellationToken = default);
}
