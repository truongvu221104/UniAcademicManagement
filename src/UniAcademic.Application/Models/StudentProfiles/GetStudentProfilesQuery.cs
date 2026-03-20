using UniAcademic.Domain.Enums;

namespace UniAcademic.Application.Models.StudentProfiles;

public sealed class GetStudentProfilesQuery
{
    public string? Keyword { get; set; }

    public Guid? StudentClassId { get; set; }

    public StudentGender? Gender { get; set; }

    public StudentProfileStatus? Status { get; set; }
}
