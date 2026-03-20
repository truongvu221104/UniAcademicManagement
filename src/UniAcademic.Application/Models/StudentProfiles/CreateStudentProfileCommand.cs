using UniAcademic.Domain.Enums;

namespace UniAcademic.Application.Models.StudentProfiles;

public sealed class CreateStudentProfileCommand
{
    public string Code { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public Guid StudentClassId { get; set; }

    public string? Email { get; set; }

    public string? Phone { get; set; }

    public DateTime? DateOfBirth { get; set; }

    public StudentGender Gender { get; set; } = StudentGender.Unknown;

    public string? Address { get; set; }

    public StudentProfileStatus Status { get; set; } = StudentProfileStatus.Active;

    public string? Note { get; set; }
}
