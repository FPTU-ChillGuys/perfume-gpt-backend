using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.DTOs.Requests.Orders
{
	public record CreateOrderRequest
	{
		public string? VoucherCode { get; init; }
		public List<Guid> ItemIds { get; init; } = [];
		public decimal? ExpectedTotalPrice { get; init; }
		public DeliveryMethod DeliveryMethod { get; init; }
		public Guid? SavedAddressId { get; init; }
		public ContactAddressInformation? Recipient { get; init; }
		public PaymentInformation Payment { get; init; } = new();
	}

	public record ContactAddressInformation
	{
		public required string ContactName { get; init; }
		public required string ContactPhoneNumber { get; init; }
		public int DistrictId { get; init; }
		public required string DistrictName { get; init; }
		public required string WardCode { get; init; }
		public required string WardName { get; init; }
		public int ProvinceId { get; init; }
		public required string ProvinceName { get; init; }
		public required string FullAddress { get; init; }
	}

	public record PaymentInformation
	{
		public PaymentMethod Method { get; init; }
		public string? PosSessionId { get; init; }
	}
}
