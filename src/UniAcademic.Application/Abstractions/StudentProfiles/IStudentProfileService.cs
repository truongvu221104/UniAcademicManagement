using UniAcademic.Application.Models.StudentProfiles;

namespace UniAcademic.Application.Abstractions.StudentProfiles;

public interface IStudentProfileService
{
    Task<StudentProfileModel> CreateAsync(CreateStudentProfileCommand command, CancellationToken cancellationToken = default);

    Task<StudentProfileModel> UpdateAsync(UpdateStudentProfileCommand command, CancellationToken cancellationToken = default);

    Task DeleteAsync(DeleteStudentProfileCommand command, CancellationToken cancellationToken = default);

    Task<StudentProfileModel> GetByIdAsync(GetStudentProfileByIdQuery query, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<StudentProfileListItemModel>> GetListAsync(GetStudentProfilesQuery query, CancellationToken cancellationToken = default);
}
