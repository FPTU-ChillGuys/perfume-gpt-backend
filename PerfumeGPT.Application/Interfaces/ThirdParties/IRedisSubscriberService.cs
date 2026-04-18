namespace PerfumeGPT.Application.Interfaces.ThirdParties
{
	/// <summary>
	/// Marker interface for the Redis subscriber background service.
	/// This service listens on Redis Pub/Sub channels and handles incoming requests
	/// from external services (e.g., AI backend) by querying local data and publishing responses.
	/// </summary>
	public interface IRedisSubscriberService
	{
	}
}
