using UniAcademic.Domain.Enums;
using UniAcademic.SharedKernel;

namespace UniAcademic.Domain.Entities.Academic;

public sealed class StudentProfile : AuditableEntity, IAuditableEntity
{
    public string StudentCode { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public Guid StudentClassId { get; set; }

    public string? Email { get; set; }

    public string? Phone { get; set; }

    public DateTime? DateOfBirth { get; set; }

    public StudentGender Gender { get; set; } = StudentGender.Unknown;

    public string? Address { get; set; }

    public StudentProfileStatus Status { get; set; } = StudentProfileStatus.Active;

    public string? Note { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAtUtc { get; set; }

    public string? DeletedBy { get; set; }

    public byte[] RowVersion { get; set; } = [];

    public StudentClass? StudentClass { get; set; }
}
