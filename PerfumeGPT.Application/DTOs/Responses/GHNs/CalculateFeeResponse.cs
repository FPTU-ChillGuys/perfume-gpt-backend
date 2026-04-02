using System.Text.Json.Serialization;

namespace PerfumeGPT.Application.DTOs.Responses.GHNs
{
	public record CalculateFeeResponse
	{
		[JsonPropertyName("code")]
		public int Code { get; init; }

		[JsonPropertyName("message")]
		public required string Message { get; init; }

		[JsonPropertyName("data")]
		public CalculateFeeData? Data { get; init; }
	}

	public record CalculateFeeData
	{
		[JsonPropertyName("total")]
		public int Total { get; init; }

		[JsonPropertyName("service_fee")]
		public int ServiceFee { get; init; }

		[JsonPropertyName("insurance_fee")]
		public int InsuranceFee { get; init; }

		[JsonPropertyName("pick_station_fee")]
		public int PickStationFee { get; init; }

		[JsonPropertyName("coupon_value")]
		public int CouponValue { get; init; }

		[JsonPropertyName("r2s_fee")]
		public int R2sFee { get; init; }

		[JsonPropertyName("document_return")]
		public int DocumentReturn { get; init; }

		[JsonPropertyName("double_check")]
		public int DoubleCheck { get; init; }

		[JsonPropertyName("cod_fee")]
		public int CodFee { get; init; }

		[JsonPropertyName("pick_remote_areas_fee")]
		public int PickRemoteAreasFee { get; init; }

		[JsonPropertyName("deliver_remote_areas_fee")]
		public int DeliverRemoteAreasFee { get; init; }

		[JsonPropertyName("cod_failed_fee")]
		public int CodFailedFee { get; init; }
	}
}
