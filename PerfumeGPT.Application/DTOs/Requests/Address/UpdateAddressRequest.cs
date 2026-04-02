namespace PerfumeGPT.Application.DTOs.Requests.Address
{
	public record UpdateAddressRequest
	{
		public required string RecipientName { get; init; }
		public required string RecipientPhoneNumber { get; init; }
		public required string Street { get; init; }
		public required string Ward { get; init; }
		public required string District { get; init; }
		public required string City { get; init; }
		public required string WardCode { get; init; }
		public int DistrictId { get; init; }
		public int ProvinceId { get; init; }
	}
}
