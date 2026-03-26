namespace UniAcademic.Application.Models.Transcripts;

public sealed class TranscriptCourseItemModel
{
    public string CourseName { get; set; } = string.Empty;

    public string CourseCode { get; set; } = string.Empty;

    public int Credits { get; set; }

    public decimal WeightedFinalScore { get; set; }

    public string GradeSymbol { get; set; } = string.Empty;

    public bool IsPassed { get; set; }
}
