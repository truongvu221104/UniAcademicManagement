using System.ComponentModel.DataAnnotations;

namespace UniAcademic.Web.Models.LecturerPortal;

public sealed class LecturerOwnProfileViewModel
{
    public Guid Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string FacultyCode { get; set; } = string.Empty;

    public string FacultyName { get; set; } = string.Empty;

    [EmailAddress]
    public string? Email { get; set; }

    public string? PhoneNumber { get; set; }

    public bool IsActive { get; set; }

    public string? Note { get; set; }
}
