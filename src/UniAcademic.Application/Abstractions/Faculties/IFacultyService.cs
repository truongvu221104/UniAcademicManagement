using UniAcademic.Application.Models.Faculties;

namespace UniAcademic.Application.Abstractions.Faculties;

public interface IFacultyService
{
    Task<FacultyModel> CreateAsync(CreateFacultyCommand command, CancellationToken cancellationToken = default);

    Task<FacultyModel> UpdateAsync(UpdateFacultyCommand command, CancellationToken cancellationToken = default);

    Task DeleteAsync(DeleteFacultyCommand command, CancellationToken cancellationToken = default);

    Task<FacultyModel> GetByIdAsync(GetFacultyByIdQuery query, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<FacultyListItemModel>> GetListAsync(GetFacultiesQuery query, CancellationToken cancellationToken = default);
}
