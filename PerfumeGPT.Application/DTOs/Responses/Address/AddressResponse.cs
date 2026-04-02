namespace PerfumeGPT.Application.DTOs.Responses.Address
{
	public record AddressResponse
	{
		public Guid Id { get; init; }
		public required string RecipientName { get; init; }
		public required string RecipientPhoneNumber { get; init; }

		// Address details
		public required string Street { get; init; }
		public required string Ward { get; init; }
		public required string District { get; init; }
		public required string City { get; init; }

		// Address from GHN
		public required string WardCode { get; init; }
		public int DistrictId { get; init; }
		public int ProvinceId { get; init; }

		public bool IsDefault { get; init; }
	}
}
