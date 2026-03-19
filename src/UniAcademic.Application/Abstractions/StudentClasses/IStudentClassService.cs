using UniAcademic.Application.Models.StudentClasses;

namespace UniAcademic.Application.Abstractions.StudentClasses;

public interface IStudentClassService
{
    Task<StudentClassModel> CreateAsync(CreateStudentClassCommand command, CancellationToken cancellationToken = default);

    Task<StudentClassModel> UpdateAsync(UpdateStudentClassCommand command, CancellationToken cancellationToken = default);

    Task DeleteAsync(DeleteStudentClassCommand command, CancellationToken cancellationToken = default);

    Task<StudentClassModel> GetByIdAsync(GetStudentClassByIdQuery query, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<StudentClassListItemModel>> GetListAsync(GetStudentClassesQuery query, CancellationToken cancellationToken = default);
}
