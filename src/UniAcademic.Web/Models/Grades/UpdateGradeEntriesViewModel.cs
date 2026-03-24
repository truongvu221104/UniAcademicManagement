using System.ComponentModel.DataAnnotations;

namespace UniAcademic.Web.Models.Grades;

public sealed class UpdateGradeEntriesViewModel
{
    public Guid Id { get; set; }

    public string CourseOfferingCode { get; set; } = string.Empty;

    public string CourseName { get; set; } = string.Empty;

    public string SemesterName { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public decimal Weight { get; set; }

    public decimal MaxScore { get; set; }

    public List<UpdateGradeEntryItemViewModel> Entries { get; set; } = [];
}

public sealed class UpdateGradeEntryItemViewModel
{
    [Required]
    public Guid RosterItemId { get; set; }

    public string StudentCode { get; set; } = string.Empty;

    public string StudentFullName { get; set; } = string.Empty;

    public string StudentClassName { get; set; } = string.Empty;

    public decimal? Score { get; set; }

    [StringLength(1000)]
    public string? Note { get; set; }
}
