namespace UniAcademic.Contracts.LecturerAssignments;

public sealed class LecturerAssignmentResponse
{
    public Guid Id { get; set; }

    public Guid CourseOfferingId { get; set; }

    public string CourseOfferingCode { get; set; } = string.Empty;

    public string CourseName { get; set; } = string.Empty;

    public string SemesterName { get; set; } = string.Empty;

    public Guid LecturerProfileId { get; set; }

    public string LecturerCode { get; set; } = string.Empty;

    public string LecturerFullName { get; set; } = string.Empty;

    public string FacultyName { get; set; } = string.Empty;

    public bool IsPrimary { get; set; }

    public DateTime AssignedAtUtc { get; set; }
}
