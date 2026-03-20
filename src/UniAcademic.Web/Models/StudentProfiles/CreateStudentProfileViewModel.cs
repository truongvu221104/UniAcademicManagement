using System.ComponentModel.DataAnnotations;
using UniAcademic.Domain.Enums;

namespace UniAcademic.Web.Models.StudentProfiles;

public sealed class CreateStudentProfileViewModel
{
    [Required]
    public string StudentCode { get; set; } = string.Empty;

    [Required]
    public string FullName { get; set; } = string.Empty;

    [Required]
    public Guid StudentClassId { get; set; }

    [EmailAddress]
    public string? Email { get; set; }

    [StringLength(20, MinimumLength = 6)]
    public string? Phone { get; set; }

    [DataType(DataType.Date)]
    public DateTime? DateOfBirth { get; set; }

    [Required]
    public StudentGender Gender { get; set; } = StudentGender.Unknown;

    public string? Address { get; set; }

    [Required]
    public StudentProfileStatus Status { get; set; } = StudentProfileStatus.Active;

    public string? Note { get; set; }
}
