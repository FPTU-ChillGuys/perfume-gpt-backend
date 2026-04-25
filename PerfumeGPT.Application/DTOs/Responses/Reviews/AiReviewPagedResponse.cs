namespace PerfumeGPT.Application.DTOs.Responses.Reviews
{
    /// <summary>
    /// Paged response cho AI backend qua NATS khi lấy danh sách reviews
    /// Đảm bảo đầy đủ metadata phân trang cho AI backend
    /// </summary>
    public record AiReviewPagedResponse
    {
        public int TotalCount { get; init; }
        public int PageNumber { get; init; }
        public int PageSize { get; init; }
        public int TotalPages { get; init; }
        public List<AiReviewListItemResponse> Items { get; init; } = [];
    }
}
