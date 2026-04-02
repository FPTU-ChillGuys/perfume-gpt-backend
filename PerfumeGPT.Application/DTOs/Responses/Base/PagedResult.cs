namespace PerfumeGPT.Application.DTOs.Responses.Base
{
    public record PagedResult<T>
    {
        public IEnumerable<T> Items { get; init; }

        // Pagination metadata
        public int PageNumber { get; init; }
        public int PageSize { get; init; }
        public int TotalCount { get; init; }
        public int TotalPages { get; init; }

        // Optional: Flags for client-side convenience
        public bool HasPreviousPage => PageNumber > 1;
        public bool HasNextPage => PageNumber < TotalPages;

        public PagedResult(IEnumerable<T> items, int pageNumber, int pageSize, int totalCount)
        {
            Items = items;
            PageNumber = pageNumber;
            PageSize = pageSize;
            TotalCount = totalCount;
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        }
    }
}
