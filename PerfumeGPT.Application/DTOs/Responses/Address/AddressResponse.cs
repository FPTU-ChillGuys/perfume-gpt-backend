namespace PerfumeGPT.Application.DTOs.Responses.Address
{
	public class AddressResponse
	{
		public Guid Id { get; set; }
		public string ReceiverName { get; set; } = string.Empty;
		public string Phone { get; set; } = null!;

		// Address details
		public string Street { get; set; } = string.Empty;
		public string Ward { get; set; } = string.Empty;
		public string District { get; set; } = string.Empty;
		public string City { get; set; } = string.Empty;

		public bool IsDefault { get; set; }
	}
}
