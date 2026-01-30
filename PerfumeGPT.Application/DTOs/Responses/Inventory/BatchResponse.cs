namespace PerfumeGPT.Application.DTOs.Responses.Inventory
{
	public class BatchResponse
	{
		public Guid Id { get; set; }
		public Guid VariantId { get; set; }
		public string VariantSku { get; set; } = null!;
		public string ProductName { get; set; } = null!;
		public int VolumeMl { get; set; }
		public string ConcentrationName { get; set; } = null!;
		public string BatchCode { get; set; } = null!;
		public DateTime ManufactureDate { get; set; }
		public DateTime ExpiryDate { get; set; }
		public int ImportQuantity { get; set; }
		public int RemainingQuantity { get; set; }
		public bool IsExpired { get; set; }
		public int DaysUntilExpiry { get; set; }
		public DateTime CreatedAt { get; set; }
	}
}
