using UniAcademic.Domain.Enums;

namespace UniAcademic.Contracts.StudentProfiles;

public sealed class StudentProfileResponse
{
    public Guid Id { get; set; }

    public string StudentCode { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public Guid StudentClassId { get; set; }

    public string StudentClassCode { get; set; } = string.Empty;

    public string StudentClassName { get; set; } = string.Empty;

    public string? Email { get; set; }

    public string? Phone { get; set; }

    public DateTime? DateOfBirth { get; set; }

    public StudentGender Gender { get; set; }

    public string? Address { get; set; }

    public StudentProfileStatus Status { get; set; }

    public string? Note { get; set; }
}
