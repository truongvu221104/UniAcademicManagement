namespace UniAcademic.Contracts.Grades;

public sealed class UpdateGradeEntriesRequest
{
    public IReadOnlyCollection<UpdateGradeEntryItemRequest> Entries { get; set; } = [];
}

public sealed class UpdateGradeEntryItemRequest
{
    public Guid RosterItemId { get; set; }

    public decimal? Score { get; set; }

    public string? Note { get; set; }
}
