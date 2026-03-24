namespace UniAcademic.Application.Models.Grades;

public sealed class GradeEntryModel
{
    public Guid Id { get; set; }

    public Guid RosterItemId { get; set; }

    public Guid StudentProfileId { get; set; }

    public string StudentCode { get; set; } = string.Empty;

    public string StudentFullName { get; set; } = string.Empty;

    public string StudentClassName { get; set; } = string.Empty;

    public decimal? Score { get; set; }

    public string? Note { get; set; }
}
