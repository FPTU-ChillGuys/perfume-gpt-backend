namespace PerfumeGPT.Application.DTOs.Responses.Nats;

/// <summary>
/// Response cho AI backend qua NATS - Review Media
/// </summary>
public sealed record NatsReviewMediaResponse
{
	public required string Id { get; init; }
	public required string Url { get; init; }
	public string? ThumbnailUrl { get; init; }
	public required string Type { get; init; }
}

/// <summary>
/// Response cho AI backend qua NATS - Review List Item
/// </summary>
public sealed record NatsReviewListItemResponse
{
	public required string Id { get; init; }
	public required string UserId { get; init; }
	public required string UserFullName { get; init; }
	public string? UserProfilePictureUrl { get; init; }
	public required string OrderDetailId { get; init; }
	public required string VariantId { get; init; }
	public required string VariantName { get; init; }
	public required int Rating { get; init; }
	public required string Comment { get; init; }
	public string? StaffFeedbackComment { get; init; }
	public string? StaffFeedbackAt { get; init; }
	public required List<NatsReviewMediaResponse> Images { get; init; }
	public required string CreatedAt { get; init; }
	public string? UpdatedAt { get; init; }
}

/// <summary>
/// Response cho AI backend qua NATS - Review Paged
/// </summary>
public sealed record NatsReviewPagedResponse
{
	public required int TotalCount { get; init; }
	public required int PageNumber { get; init; }
	public required int PageSize { get; init; }
	public required int TotalPages { get; init; }
	public required List<NatsReviewListItemResponse> Items { get; init; }
}

/// <summary>
/// Response cho AI backend qua NATS - Review Variant Stats
/// </summary>
public sealed record NatsReviewVariantStats
{
	public required string VariantId { get; init; }
	public required int TotalReviews { get; init; }
	public required double AverageRating { get; init; }
	public required int FiveStarCount { get; init; }
	public required int FourStarCount { get; init; }
	public required int ThreeStarCount { get; init; }
	public required int TwoStarCount { get; init; }
	public required int OneStarCount { get; init; }
}
