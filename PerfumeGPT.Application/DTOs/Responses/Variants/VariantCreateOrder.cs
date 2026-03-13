namespace PerfumeGPT.Application.DTOs.Responses.Variants
{
	public class VariantCreateOrder
	{
		public Guid Id { get; set; }
		public decimal UnitPrice { get; set; }
		public string Snapshot { get; set; } = null!;
	}
}
