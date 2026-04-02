namespace PerfumeGPT.Application.DTOs.Responses.GHNs
{
	using System.Text.Json.Serialization;

	public record CreateShippingOrderResponse
	{
		[JsonPropertyName("order_code")]
		public required string OrderCode { get; init; }

		[JsonPropertyName("sort_code")]
		public required string SortCode { get; init; }

		[JsonPropertyName("trans_type")]
		public required string TransType { get; init; }

		[JsonPropertyName("ward_encode")]
		public required string WardEncode { get; init; }

		[JsonPropertyName("district_encode")]
		public required string DistrictEncode { get; init; }

		[JsonPropertyName("fee")]
		public required ShippingFeeDetails Fee { get; init; }

		[JsonPropertyName("total_fee")]
		public int TotalFee { get; init; }

		[JsonPropertyName("expected_delivery_time")]
		public DateTime? ExpectedDeliveryTime { get; init; }
	}

	public record ShippingFeeDetails
	{
		[JsonPropertyName("main_service")]
		public int MainService { get; init; }

		[JsonPropertyName("insurance")]
		public int Insurance { get; init; }

		[JsonPropertyName("station_do")]
		public int StationDo { get; init; }

		[JsonPropertyName("station_pu")]
		public int StationPu { get; init; }

		[JsonPropertyName("return")]
		public int Return { get; init; }

		[JsonPropertyName("r2s")]
		public int R2S { get; init; }

		[JsonPropertyName("coupon")]
		public int Coupon { get; init; }

		[JsonPropertyName("cod_failed_fee")]
		public int CodFailedFee { get; init; }
	}
}
