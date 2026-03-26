namespace UniAcademic.Application.Models.Transcripts;

public sealed class TranscriptSemesterModel
{
    public string SemesterName { get; set; } = string.Empty;

    public string AcademicYear { get; set; } = string.Empty;

    public IReadOnlyCollection<TranscriptCourseItemModel> Courses { get; set; } = [];

    public decimal SemesterGPA { get; set; }

    public int SemesterCreditsEarned { get; set; }
}
