using System.ComponentModel.DataAnnotations;

namespace UniAcademic.Web.Models.LecturerProfiles;

public sealed class UpdateLecturerProfileViewModel
{
    [Required]
    public Guid Id { get; set; }

    [Required]
    public string Code { get; set; } = string.Empty;

    [Required]
    public string FullName { get; set; } = string.Empty;

    [EmailAddress]
    public string? Email { get; set; }

    [StringLength(20, MinimumLength = 6)]
    public string? PhoneNumber { get; set; }

    [Required]
    public Guid FacultyId { get; set; }

    public bool IsActive { get; set; } = true;

    public string? Note { get; set; }
}
