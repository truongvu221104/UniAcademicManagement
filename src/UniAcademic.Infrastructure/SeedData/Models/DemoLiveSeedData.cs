namespace UniAcademic.Infrastructure.SeedData.Models;

public sealed class DemoLiveSeedData
{
    public IReadOnlyCollection<DemoLiveOfferingSeedItem> Offerings { get; set; } = [];
}

public sealed class DemoLiveOfferingSeedItem
{
    public string CourseOfferingCode { get; set; } = string.Empty;

    public DateTime? FinalizedAtUtc { get; set; }

    public string? FinalizeNote { get; set; }

    public decimal PassingScore { get; set; } = 50m;

    public bool GenerateGradeResults { get; set; } = true;

    public IReadOnlyCollection<string> Students { get; set; } = [];

    public IReadOnlyCollection<DemoLiveAttendanceSessionSeedItem> AttendanceSessions { get; set; } = [];

    public IReadOnlyCollection<DemoLiveGradeCategorySeedItem> GradeCategories { get; set; } = [];
}

public sealed class DemoLiveAttendanceSessionSeedItem
{
    public DateTime SessionDate { get; set; }

    public int SessionNo { get; set; }

    public string? Title { get; set; }

    public string? Note { get; set; }

    public IReadOnlyCollection<DemoLiveAttendanceRecordSeedItem> Records { get; set; } = [];
}

public sealed class DemoLiveAttendanceRecordSeedItem
{
    public string StudentCode { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public string? Note { get; set; }
}

public sealed class DemoLiveGradeCategorySeedItem
{
    public string Name { get; set; } = string.Empty;

    public decimal Weight { get; set; }

    public decimal MaxScore { get; set; }

    public int OrderIndex { get; set; }

    public bool IsActive { get; set; } = true;

    public IReadOnlyCollection<DemoLiveGradeEntrySeedItem> Entries { get; set; } = [];
}

public sealed class DemoLiveGradeEntrySeedItem
{
    public string StudentCode { get; set; } = string.Empty;

    public decimal? Score { get; set; }

    public string? Note { get; set; }
}
