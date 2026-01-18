namespace PerfumeGPT.Application.DTOs.Requests.Address
{
	public class CreateAddressRequest
	{
		public string ReceiverName { get; set; } = string.Empty;
		public string Phone { get; set; } = null!;

		// Address details
		public string Street { get; set; } = string.Empty;
		public string Ward { get; set; } = string.Empty;
		public string District { get; set; } = string.Empty;
		public string City { get; set; } = string.Empty;
		public string WardCode { get; set; } = null!;
		public int DistrictId { get; set; }
		public int ProvinceId { get; set; }
	}
}
