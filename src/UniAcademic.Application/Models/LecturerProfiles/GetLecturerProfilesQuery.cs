namespace UniAcademic.Application.Models.LecturerProfiles;

public sealed class GetLecturerProfilesQuery
{
    public string? Keyword { get; set; }

    public Guid? FacultyId { get; set; }

    public bool? IsActive { get; set; }
}
