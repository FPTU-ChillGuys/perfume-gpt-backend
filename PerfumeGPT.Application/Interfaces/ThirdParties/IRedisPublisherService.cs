namespace PerfumeGPT.Application.Interfaces.ThirdParties
{
	/// <summary>
	/// Publishes order-related events to a Redis channel for consumption by other services (e.g., AI backend for email).
	/// </summary>
	public interface IRedisPublisherService
	{
		/// <summary>
		/// Publishes an "order_created" event to Redis with the order ID and user ID.
		/// Implementations should be fire-and-forget safe — errors must not propagate to the caller.
		/// </summary>
		Task PublishOrderCreatedAsync(Guid orderId, Guid userId);

		/// <summary>
		/// Publishes a "review_created" event to Redis with the variant ID.
		/// Implementations should be fire-and-forget safe — errors must not propagate to the caller.
		/// </summary>
		Task PublishReviewCreatedAsync(Guid variantId);
	}
}
