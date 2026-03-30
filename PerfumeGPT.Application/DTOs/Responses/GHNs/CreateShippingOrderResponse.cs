namespace PerfumeGPT.Application.DTOs.Responses.GHNs
{
	using System.Text.Json.Serialization;

	public class CreateShippingOrderResponse
	{
		[JsonPropertyName("order_code")]
		public string OrderCode { get; set; } = null!;

		[JsonPropertyName("sort_code")]
		public string SortCode { get; set; } = null!;

		[JsonPropertyName("trans_type")]
		public string TransType { get; set; } = null!;

		[JsonPropertyName("ward_encode")]
		public string WardEncode { get; set; } = null!;

		[JsonPropertyName("district_encode")]
		public string DistrictEncode { get; set; } = null!;

		[JsonPropertyName("fee")]
		public ShippingFeeDetails Fee { get; set; } = null!;

		[JsonPropertyName("total_fee")]
		public int TotalFee { get; set; }

		[JsonPropertyName("expected_delivery_time")]
		public DateTime? ExpectedDeliveryTime { get; set; }
	}

	public class ShippingFeeDetails
	{
		[JsonPropertyName("main_service")]
		public int MainService { get; set; }

		[JsonPropertyName("insurance")]
		public int Insurance { get; set; }

		[JsonPropertyName("station_do")]
		public int StationDo { get; set; }

		[JsonPropertyName("station_pu")]
		public int StationPu { get; set; }

		[JsonPropertyName("return")]
		public int Return { get; set; }

		[JsonPropertyName("r2s")]
		public int R2S { get; set; }

		[JsonPropertyName("coupon")]
		public int Coupon { get; set; }

		[JsonPropertyName("cod_failed_fee")]
		public int CodFailedFee { get; set; }
	}
}
