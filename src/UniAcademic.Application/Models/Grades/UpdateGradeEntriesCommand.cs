namespace UniAcademic.Application.Models.Grades;

public sealed class UpdateGradeEntriesCommand
{
    public Guid Id { get; set; }

    public IReadOnlyCollection<UpdateGradeEntryItemCommand> Entries { get; set; } = [];
}

public sealed class UpdateGradeEntryItemCommand
{
    public Guid RosterItemId { get; set; }

    public decimal? Score { get; set; }

    public string? Note { get; set; }
}
