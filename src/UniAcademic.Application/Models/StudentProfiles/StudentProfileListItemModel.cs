using UniAcademic.Domain.Enums;

namespace UniAcademic.Application.Models.StudentProfiles;

public sealed class StudentProfileListItemModel
{
    public Guid Id { get; set; }

    public string StudentCode { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public Guid StudentClassId { get; set; }

    public string StudentClassName { get; set; } = string.Empty;

    public string? Email { get; set; }

    public string? Phone { get; set; }

    public DateTime? DateOfBirth { get; set; }

    public StudentGender Gender { get; set; }

    public StudentProfileStatus Status { get; set; }
}
