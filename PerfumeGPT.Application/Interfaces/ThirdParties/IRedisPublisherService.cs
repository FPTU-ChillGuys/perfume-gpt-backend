using System;
using System.Threading.Tasks;

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

		/// <summary>
		/// Publishes a "cart:updated" event to Redis so other services (AI backend, other .NET instances)
		/// know the cart has changed. The subscriber on this side will forward it to SignalR.
		/// Implementations should be fire-and-forget safe — errors must not propagate to the caller.
		/// </summary>
		Task PublishCartUpdatedAsync(Guid userId, int cartItemCount);

		/// <summary>
		/// Publishes a "product:updated" event to Redis when a product is created, updated, or deleted.
		/// The NestJS backend listens on this channel to rebuild embeddings and refresh BM25 indexes.
		/// Implementations should be fire-and-forget safe — errors must not propagate to the caller.
		/// </summary>
		Task PublishProductUpdatedAsync(Guid productId, string action);
	}
}
