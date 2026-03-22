namespace PerfumeGPT.Application.DTOs.Requests.Address
{
	public class UpdateAddressRequest
	{
		public string RecipientName { get; set; } = string.Empty;
		public string RecipientPhoneNumber { get; set; } = string.Empty;

		public string Street { get; set; } = string.Empty;
		public string Ward { get; set; } = string.Empty;
		public string District { get; set; } = string.Empty;
		public string City { get; set; } = string.Empty;

		// GHN specific fields
		public string WardCode { get; set; } = null!;
		public int DistrictId { get; set; }
		public int ProvinceId { get; set; }
	}
}
