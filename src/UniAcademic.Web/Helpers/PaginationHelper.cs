using UniAcademic.Web.Models.Common;

namespace UniAcademic.Web.Helpers;

public static class PaginationHelper
{
    private static readonly int[] AllowedPageSizes = [5, 10, 20, 50];

    public static PaginatedResult<T> Paginate<T>(IEnumerable<T> source, int? pageNumber, int? pageSize)
    {
        var items = source as IReadOnlyCollection<T> ?? source.ToArray();
        var normalizedPageSize = AllowedPageSizes.Contains(pageSize.GetValueOrDefault())
            ? pageSize.GetValueOrDefault()
            : 10;

        var totalItems = items.Count;
        var totalPages = Math.Max(1, (int)Math.Ceiling(totalItems / (double)normalizedPageSize));
        var normalizedPage = Math.Clamp(pageNumber.GetValueOrDefault(1), 1, totalPages);
        var skip = (normalizedPage - 1) * normalizedPageSize;
        var pageItems = items.Skip(skip).Take(normalizedPageSize).ToArray();

        return new PaginatedResult<T>(
            pageItems,
            new PaginationViewModel
            {
                Page = normalizedPage,
                PageSize = normalizedPageSize,
                TotalItems = totalItems,
                TotalPages = totalPages,
                StartItem = totalItems == 0 ? 0 : skip + 1,
                EndItem = totalItems == 0 ? 0 : skip + pageItems.Length,
                PageSizeOptions = AllowedPageSizes,
                VisiblePages = BuildVisiblePages(normalizedPage, totalPages)
            });
    }

    private static IReadOnlyCollection<int?> BuildVisiblePages(int currentPage, int totalPages)
    {
        if (totalPages <= 5)
        {
            return Enumerable.Range(1, totalPages).Select(static x => (int?)x).ToArray();
        }

        var pages = new SortedSet<int>
        {
            1,
            totalPages,
            currentPage
        };

        if (currentPage > 1)
        {
            pages.Add(currentPage - 1);
        }

        if (currentPage < totalPages)
        {
            pages.Add(currentPage + 1);
        }

        if (currentPage <= 2)
        {
            pages.Add(2);
            pages.Add(3);
        }

        if (currentPage >= totalPages - 1)
        {
            pages.Add(totalPages - 1);
            pages.Add(totalPages - 2);
        }

        var sequence = new List<int?>();
        int? previous = null;

        foreach (var page in pages.Where(x => x >= 1 && x <= totalPages))
        {
            if (previous.HasValue && page - previous.Value > 1)
            {
                sequence.Add(null);
            }

            sequence.Add(page);
            previous = page;
        }

        return sequence;
    }
}

public sealed record PaginatedResult<T>(
    IReadOnlyCollection<T> Items,
    PaginationViewModel Pagination);
