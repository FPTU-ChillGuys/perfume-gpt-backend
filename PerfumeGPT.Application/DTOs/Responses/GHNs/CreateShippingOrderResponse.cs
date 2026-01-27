namespace PerfumeGPT.Application.DTOs.Responses.GHNs
{
	public class CreateShippingOrderResponse
	{
		public string OrderCode { get; set; } = null!;
		public string SortCode { get; set; } = null!;
		public string TransType { get; set; } = null!;
		public string WardEncode { get; set; } = null!;
		public string DistrictEncode { get; set; } = null!;
		public ShippingFeeDetails Fee { get; set; } = null!;
		public int TotalFee { get; set; }
		public DateTime? ExpectedDeliveryTime { get; set; }
	}

	public class ShippingFeeDetails
	{
		public int MainService { get; set; }
		public int Insurance { get; set; }
		public int StationDo { get; set; }
		public int StationPu { get; set; }
		public int Return { get; set; }
		public int R2S { get; set; }
		public int Coupon { get; set; }
		public int CodFailedFee { get; set; }
	}
}
