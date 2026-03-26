namespace UniAcademic.Application.Models.Transcripts;

public sealed class TranscriptModel
{
    public string StudentCode { get; set; } = string.Empty;

    public string StudentFullName { get; set; } = string.Empty;

    public string StudentClassName { get; set; } = string.Empty;

    public string FacultyName { get; set; } = string.Empty;

    public IReadOnlyCollection<TranscriptSemesterModel> Semesters { get; set; } = [];

    public decimal OverallGPA { get; set; }

    public int TotalCreditsEarned { get; set; }
}
