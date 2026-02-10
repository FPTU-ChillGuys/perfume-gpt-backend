namespace PerfumeGPT.Application.DTOs.Requests.GHNs
{
	public class UpdateOrderRequest
	{
		public string OrderCode { get; set; } = null!;

		// Sender
		public string? FromName { get; set; }
		public string? FromPhone { get; set; }
		public string? FromAddress { get; set; }
		public string? FromWardCode { get; set; }
		public int? FromDistrictId { get; set; }

		// Recipient
		public string? ToName { get; set; }
		public string? ToPhone { get; set; }
		public string? ToAddress { get; set; }
		public string? ToWardCode { get; set; }
		public int? ToDistrictId { get; set; }

		// Return info
		public string? ReturnPhone { get; set; }
		public string? ReturnAddress { get; set; }
		public string? ReturnWardCode { get; set; }
		public int? ReturnDistrictId { get; set; }

		public string? ClientOrderCode { get; set; }
		public int? CodAmount { get; set; }
		public string? Content { get; set; }

		// Dimensions
		public int? Weight { get; set; }
		public int? Length { get; set; }
		public int? Width { get; set; }
		public int? Height { get; set; }

		public int? PickStationId { get; set; }
		public int? InsuranceValue { get; set; }
		public string? Coupon { get; set; }
		public int? PaymentTypeId { get; set; }
		public string? Note { get; set; }
		public string? RequiredNote { get; set; }
		public string? PickShift { get; set; }
	}
}
