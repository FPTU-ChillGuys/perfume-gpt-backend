namespace PerfumeGPT.Application.DTOs.Requests.GHNs
{
	public class CreateShippingOrderRequest
	{
		public string? FromName { get; set; }
		public string? FromPhone { get; set; }
		public string? FromAddress { get; set; }
		public string? FromWardName { get; set; }
		public string? FromDistrictName { get; set; }
		public string? FromProvinceName { get; set; }
		public string ToName { get; set; } = null!;
		public string ToPhone { get; set; } = null!;
		public string ToAddress { get; set; } = null!;
		public string ToWardName { get; set; } = null!;
		public string ToDistrictName { get; set; } = null!;
		public string ToProvinceName { get; set; } = null!;
		public string? ReturnPhone { get; set; }
		public string? ReturnAddress { get; set; }
		public string? ReturnDistrictName { get; set; }
		public string? ReturnWardName { get; set; }
		public string? ReturnProvinceName { get; set; }
		public string? ClientOrderCode { get; set; }
		public int CodAmount { get; set; }
		public string? Content { get; set; }
		public int Weight { get; set; }
		public int Length { get; set; }
		public int Width { get; set; }
		public int Height { get; set; }
		public int? PickStationId { get; set; }
		public int InsuranceValue { get; set; }
		public string? Coupon { get; set; }
		public int ServiceTypeId { get; set; }
		public int PaymentTypeId { get; set; }
		public string? Note { get; set; }
		public string RequiredNote { get; set; } = "KHONGCHOXEMHANG";
		public List<int>? PickShift { get; set; }
		public long? PickupTime { get; set; }
		public List<ShippingOrderItem>? Items { get; set; }
		public int? CodFailedAmount { get; set; }
	}

	public class ShippingOrderItem
	{
		public string? Name { get; set; }
		public string? Code { get; set; }
		public int Quantity { get; set; }
		public int Price { get; set; }
		public int? Length { get; set; }
		public int? Width { get; set; }
		public int? Height { get; set; }
		public int? Weight { get; set; }
		public ShippingOrderItemCategory? Category { get; set; }
	}

	public class ShippingOrderItemCategory
	{
		public string? Level1 { get; set; }
	}
}
