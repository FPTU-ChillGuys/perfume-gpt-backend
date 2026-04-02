namespace PerfumeGPT.Application.DTOs.Requests.GHNs
{
	public record UpdateOrderRequest
	{
		public required string OrderCode { get; init; }

		// Sender
		public string? FromName { get; init; }
		public string? FromPhone { get; init; }
		public string? FromAddress { get; init; }
		public string? FromWardCode { get; init; }
		public int? FromDistrictId { get; init; }

		// Recipient
		public string? ToName { get; init; }
		public string? ToPhone { get; init; }
		public string? ToAddress { get; init; }
		public string? ToWardCode { get; init; }
		public int? ToDistrictId { get; init; }

		// Return info
		public string? ReturnPhone { get; init; }
		public string? ReturnAddress { get; init; }
		public string? ReturnWardCode { get; init; }
		public int? ReturnDistrictId { get; init; }

		public string? ClientOrderCode { get; init; }
		public int? CodAmount { get; init; }
		public string? Content { get; init; }

		// Dimensions
		public int? Weight { get; init; }
		public int? Length { get; init; }
		public int? Width { get; init; }
		public int? Height { get; init; }

		public int? PickStationId { get; init; }
		public int? InsuranceValue { get; init; }
		public string? Coupon { get; init; }
		public int? PaymentTypeId { get; init; }
		public string? Note { get; init; }
		public string? RequiredNote { get; init; }
		public string? PickShift { get; init; }
	}
}
