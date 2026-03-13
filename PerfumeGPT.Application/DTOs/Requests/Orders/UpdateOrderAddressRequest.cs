namespace PerfumeGPT.Application.DTOs.Requests.Orders
{
	public class UpdateOrderAddressRequest
	{
		public Guid? SavedAddressId { get; set; }
		public RecipientInformation? RecipientInformation { get; set; }
	}
}
