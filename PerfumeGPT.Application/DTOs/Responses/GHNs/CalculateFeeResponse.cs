using System.Text.Json.Serialization;

namespace PerfumeGPT.Application.DTOs.Responses.GHNs
{
	public class CalculateFeeResponse
	{
		[JsonPropertyName("code")]
		public int Code { get; set; }

		[JsonPropertyName("message")]
		public string Message { get; set; }

		[JsonPropertyName("data")]
		public CalculateFeeData Data { get; set; }
	}

	public class CalculateFeeData
	{
		[JsonPropertyName("total")]
		public int Total { get; set; }

		[JsonPropertyName("service_fee")]
		public int ServiceFee { get; set; }

		[JsonPropertyName("insurance_fee")]
		public int InsuranceFee { get; set; }

		[JsonPropertyName("pick_station_fee")]
		public int PickStationFee { get; set; }

		[JsonPropertyName("coupon_value")]
		public int CouponValue { get; set; }

		[JsonPropertyName("r2s_fee")]
		public int R2sFee { get; set; }

		[JsonPropertyName("document_return")]
		public int DocumentReturn { get; set; }

		[JsonPropertyName("double_check")]
		public int DoubleCheck { get; set; }

		[JsonPropertyName("cod_fee")]
		public int CodFee { get; set; }

		[JsonPropertyName("pick_remote_areas_fee")]
		public int PickRemoteAreasFee { get; set; }

		[JsonPropertyName("deliver_remote_areas_fee")]
		public int DeliverRemoteAreasFee { get; set; }

		[JsonPropertyName("cod_failed_fee")]
		public int CodFailedFee { get; set; }
	}
}
