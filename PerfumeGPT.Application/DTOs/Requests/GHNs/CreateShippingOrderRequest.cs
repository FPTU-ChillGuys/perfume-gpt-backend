namespace PerfumeGPT.Application.DTOs.Requests.GHNs
{
	public record CreateShippingOrderRequest
	{
		public string? FromName { get; init; }
		public string? FromPhone { get; init; }
		public string? FromAddress { get; init; }
		public string? FromWardName { get; init; }
		public string? FromDistrictName { get; init; }
		public string? FromProvinceName { get; init; }
		public required string ToName { get; init; }
		public required string ToPhone { get; init; }
		public required string ToAddress { get; init; }
		public required string ToWardName { get; init; }
		public required string ToDistrictName { get; init; }
		public required string ToProvinceName { get; init; }
		public string? ReturnPhone { get; init; }
		public string? ReturnAddress { get; init; }
		public string? ReturnDistrictName { get; init; }
		public string? ReturnWardName { get; init; }
		public string? ReturnProvinceName { get; init; }
		public string? ClientOrderCode { get; init; }
		public required int CodAmount { get; init; }
		public required int Weight { get; init; }
		public required int Length { get; init; }
		public required int Width { get; init; }
		public required int Height { get; init; }
		public required int ServiceTypeId { get; init; }
		public required int PaymentTypeId { get; init; }
		public required int InsuranceValue { get; init; }
		public string RequiredNote { get; init; } = "KHONGCHOXEMHANG";
		public string? Content { get; init; }
		public int? PickStationId { get; init; }
		public string? Coupon { get; init; }
		public string? Note { get; init; }
		public List<int>? PickShift { get; init; }
		public long? PickupTime { get; init; }
		public List<ShippingOrderItem>? Items { get; init; }
		public int? CodFailedAmount { get; init; }
	}

	public record ShippingOrderItem
	{
		public required string Name { get; init; }
		public string? Code { get; init; }
		public required int Quantity { get; init; }
		public required int Price { get; init; }
		public int? Length { get; init; }
		public int? Width { get; init; }
		public int? Height { get; init; }
		public int? Weight { get; init; }
		public ShippingOrderItemCategory? Category { get; init; }
	}

	public record ShippingOrderItemCategory
	{
		public required string Level1 { get; init; }
	}
}
