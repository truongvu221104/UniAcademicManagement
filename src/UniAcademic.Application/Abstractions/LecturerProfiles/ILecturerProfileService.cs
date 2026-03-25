using UniAcademic.Application.Models.LecturerProfiles;

namespace UniAcademic.Application.Abstractions.LecturerProfiles;

public interface ILecturerProfileService
{
    Task<LecturerProfileModel> CreateAsync(CreateLecturerProfileCommand command, CancellationToken cancellationToken = default);

    Task<LecturerProfileModel> UpdateAsync(UpdateLecturerProfileCommand command, CancellationToken cancellationToken = default);

    Task DeleteAsync(DeleteLecturerProfileCommand command, CancellationToken cancellationToken = default);

    Task<LecturerProfileModel> GetByIdAsync(GetLecturerProfileByIdQuery query, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<LecturerProfileListItemModel>> GetListAsync(GetLecturerProfilesQuery query, CancellationToken cancellationToken = default);
}
