namespace UniAcademic.Web.Models.Common;

public sealed class PaginationViewModel
{
    public int Page { get; init; }

    public int PageSize { get; init; }

    public int TotalItems { get; init; }

    public int TotalPages { get; init; }

    public int StartItem { get; init; }

    public int EndItem { get; init; }

    public IReadOnlyCollection<int> PageSizeOptions { get; init; } = [];

    public IReadOnlyCollection<int?> VisiblePages { get; init; } = [];
}
