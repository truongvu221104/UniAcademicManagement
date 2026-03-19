using UniAcademic.Application.Models.Semesters;

namespace UniAcademic.Application.Abstractions.Semesters;

public interface ISemesterService
{
    Task<SemesterModel> CreateAsync(CreateSemesterCommand command, CancellationToken cancellationToken = default);

    Task<SemesterModel> UpdateAsync(UpdateSemesterCommand command, CancellationToken cancellationToken = default);

    Task DeleteAsync(DeleteSemesterCommand command, CancellationToken cancellationToken = default);

    Task<SemesterModel> GetByIdAsync(GetSemesterByIdQuery query, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<SemesterListItemModel>> GetListAsync(GetSemestersQuery query, CancellationToken cancellationToken = default);
}
