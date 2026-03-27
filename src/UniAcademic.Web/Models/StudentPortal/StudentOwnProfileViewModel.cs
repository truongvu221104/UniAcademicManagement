using System.ComponentModel.DataAnnotations;
using UniAcademic.Domain.Enums;

namespace UniAcademic.Web.Models.StudentPortal;

public sealed class StudentOwnProfileViewModel
{
    public Guid Id { get; set; }

    public string StudentCode { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string StudentClassCode { get; set; } = string.Empty;

    public string StudentClassName { get; set; } = string.Empty;

    [EmailAddress]
    public string? Email { get; set; }

    public string? Phone { get; set; }

    [DataType(DataType.Date)]
    public DateTime? DateOfBirth { get; set; }

    public StudentGender Gender { get; set; } = StudentGender.Unknown;

    public string? Address { get; set; }

    public StudentProfileStatus Status { get; set; }

    public string? Note { get; set; }
}
