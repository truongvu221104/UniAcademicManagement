namespace UniAcademic.Contracts.LecturerProfiles;

public sealed class UpdateLecturerProfileRequest
{
    public string Code { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string? Email { get; set; }

    public string? PhoneNumber { get; set; }

    public Guid FacultyId { get; set; }

    public bool IsActive { get; set; }

    public string? Note { get; set; }
}
