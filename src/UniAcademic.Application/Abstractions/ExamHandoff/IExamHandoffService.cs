using UniAcademic.Application.Models.ExamHandoff;
using UniAcademic.Domain.Entities.Academic;

namespace UniAcademic.Application.Abstractions.ExamHandoff;

public interface IExamHandoffService
{
    Task HandoffAsync(CourseOfferingRosterSnapshot snapshot, CancellationToken cancellationToken = default);

    Task RetryHandoffAsync(Guid courseOfferingId, CancellationToken cancellationToken = default);

    Task<ExamHandoffLogModel?> GetLatestStatusAsync(Guid courseOfferingId, CancellationToken cancellationToken = default);
}
