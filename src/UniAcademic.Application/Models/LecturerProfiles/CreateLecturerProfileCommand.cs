namespace UniAcademic.Application.Models.LecturerProfiles;

public sealed class CreateLecturerProfileCommand
{
    public string Code { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string? Email { get; set; }

    public string? PhoneNumber { get; set; }

    public Guid FacultyId { get; set; }

    public bool IsActive { get; set; } = true;

    public string? Note { get; set; }
}
