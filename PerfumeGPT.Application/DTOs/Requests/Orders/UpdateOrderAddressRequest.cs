namespace PerfumeGPT.Application.DTOs.Requests.Orders
{
	public record UpdateOrderAddressRequest
	{
		public Guid? SavedAddressId { get; init; }
		public RecipientInformation? RecipientInformation { get; init; }
	}
}
