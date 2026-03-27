namespace UniAcademic.Infrastructure.SeedData.Models;

public sealed class DemoFoundationSeedData
{
    public IReadOnlyCollection<DemoUserSeedItem> Users { get; set; } = [];

    public IReadOnlyCollection<StudentClassSeedItem> StudentClasses { get; set; } = [];

    public IReadOnlyCollection<CourseSeedItem> Courses { get; set; } = [];

    public IReadOnlyCollection<CoursePrerequisiteSeedItem> CoursePrerequisites { get; set; } = [];

    public IReadOnlyCollection<SemesterSeedItem> Semesters { get; set; } = [];

    public IReadOnlyCollection<StudentProfileSeedItem> StudentProfiles { get; set; } = [];

    public IReadOnlyCollection<LecturerProfileSeedItem> LecturerProfiles { get; set; } = [];

    public IReadOnlyCollection<CourseOfferingSeedItem> CourseOfferings { get; set; } = [];

    public IReadOnlyCollection<LecturerAssignmentSeedItem> LecturerAssignments { get; set; } = [];
}

public sealed class DemoUserSeedItem
{
    public string Username { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string? StudentCode { get; set; }

    public string? LecturerCode { get; set; }

    public bool IsActive { get; set; } = true;
}

public sealed class StudentClassSeedItem
{
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string FacultyCode { get; set; } = string.Empty;

    public int IntakeYear { get; set; }

    public string Status { get; set; } = string.Empty;

    public string? Description { get; set; }
}

public sealed class CourseSeedItem
{
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public int Credits { get; set; }

    public string? FacultyCode { get; set; }

    public string Status { get; set; } = string.Empty;

    public string? Description { get; set; }
}

public sealed class SemesterSeedItem
{
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string AcademicYear { get; set; } = string.Empty;

    public int TermNo { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public string Status { get; set; } = string.Empty;

    public string? Description { get; set; }
}

public sealed class CoursePrerequisiteSeedItem
{
    public string CourseCode { get; set; } = string.Empty;

    public string PrerequisiteCourseCode { get; set; } = string.Empty;
}

public sealed class StudentProfileSeedItem
{
    public string StudentCode { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string StudentClassCode { get; set; } = string.Empty;

    public string? Email { get; set; }

    public string? Phone { get; set; }

    public string Gender { get; set; } = "Unknown";

    public string Status { get; set; } = string.Empty;

    public string? Note { get; set; }
}

public sealed class LecturerProfileSeedItem
{
    public string Code { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string? Email { get; set; }

    public string? PhoneNumber { get; set; }

    public string FacultyCode { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public string? Note { get; set; }
}

public sealed class CourseOfferingSeedItem
{
    public string Code { get; set; } = string.Empty;

    public string CourseCode { get; set; } = string.Empty;

    public string SemesterCode { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public int Capacity { get; set; }

    public string Status { get; set; } = string.Empty;

    public string? Description { get; set; }
}

public sealed class LecturerAssignmentSeedItem
{
    public string CourseOfferingCode { get; set; } = string.Empty;

    public string LecturerCode { get; set; } = string.Empty;

    public bool IsPrimary { get; set; }

    public DateTime AssignedAtUtc { get; set; }
}
