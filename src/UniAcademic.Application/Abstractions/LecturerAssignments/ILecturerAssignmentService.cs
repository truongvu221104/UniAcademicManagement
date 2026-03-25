using UniAcademic.Application.Models.LecturerAssignments;

namespace UniAcademic.Application.Abstractions.LecturerAssignments;

public interface ILecturerAssignmentService
{
    Task<LecturerAssignmentModel> AssignAsync(AssignLecturerCommand command, CancellationToken cancellationToken = default);

    Task UnassignAsync(UnassignLecturerCommand command, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<LecturerAssignmentModel>> GetListAsync(GetLecturerAssignmentsQuery query, CancellationToken cancellationToken = default);
}
