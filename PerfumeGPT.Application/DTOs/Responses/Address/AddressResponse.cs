namespace PerfumeGPT.Application.DTOs.Responses.Address
{
	public class AddressResponse
	{
		public Guid Id { get; set; }

		// Address details
		public string Street { get; set; } = string.Empty;
		public string Ward { get; set; } = string.Empty;
		public string District { get; set; } = string.Empty;
		public string City { get; set; } = string.Empty;

		// Address from GHN
		public string WardCode { get; set; } = null!;
		public int DistrictId { get; set; }
		public int ProvinceId { get; set; }

		public bool IsDefault { get; set; }
	}
}
