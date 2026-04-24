namespace PerfumeGPT.Application.DTOs.Responses.Carts
{
	public record GetCartTotalResponse
	{
		public decimal Subtotal { get; init; }
		public decimal ShippingFee { get; init; }
		public decimal Discount { set; get; }
		public decimal TotalPrice { get; init; }
		public DepositPolicyPreviewResponse DepositPolicy { get; init; } = new();
	}

	public record DepositPolicyPreviewResponse
	{
		public bool IsDepositRequired { get; init; }
		public decimal DepositRate { get; init; }
		public decimal DepositAmount { get; init; }
		public decimal RemainingAmount { get; init; }
	}
}
