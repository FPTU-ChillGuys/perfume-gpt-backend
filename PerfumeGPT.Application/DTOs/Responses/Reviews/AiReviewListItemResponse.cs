namespace PerfumeGPT.Application.DTOs.Responses.Reviews
{
    /// <summary>
    /// Response cho AI backend qua NATS khi lấy danh sách reviews
    /// Đảm bảo type safety và camelCase serialization
    /// </summary>
    public record AiReviewListItemResponse
    {
        public string Id { get; init; } = string.Empty;
        public string UserId { get; init; } = string.Empty;
        public string UserFullName { get; init; } = string.Empty;
        public string? UserProfilePictureUrl { get; init; }
        public string OrderDetailId { get; init; } = string.Empty;
        public string VariantId { get; init; } = string.Empty;
        public string VariantName { get; init; } = string.Empty;
        public int Rating { get; init; }
        public string Comment { get; init; } = string.Empty;
        public string? StaffFeedbackComment { get; init; }
        public string? StaffFeedbackAt { get; init; }
        public List<AiReviewMediaResponse> Images { get; init; } = [];
        public string CreatedAt { get; init; } = string.Empty;
        public string? UpdatedAt { get; init; }
    }

    /// <summary>
    /// Media response đơn giản cho AI review
    /// </summary>
    public record AiReviewMediaResponse
    {
        public string Id { get; init; } = string.Empty;
        public string Url { get; init; } = string.Empty;
        public string? ThumbnailUrl { get; init; }
        public string Type { get; init; } = "Image";
    }
}
