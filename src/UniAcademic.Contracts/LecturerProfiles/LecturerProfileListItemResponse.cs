namespace UniAcademic.Contracts.LecturerProfiles;

public sealed class LecturerProfileListItemResponse
{
    public Guid Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string? Email { get; set; }

    public string? PhoneNumber { get; set; }

    public Guid FacultyId { get; set; }

    public string FacultyName { get; set; } = string.Empty;

    public bool IsActive { get; set; }
}
