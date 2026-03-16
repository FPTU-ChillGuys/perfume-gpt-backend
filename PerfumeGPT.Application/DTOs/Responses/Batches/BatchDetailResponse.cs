namespace PerfumeGPT.Application.DTOs.Responses.Batches
{
	public class BatchDetailResponse : BatchResponse
	{
		public Guid VariantId { get; set; }
		public string VariantSku { get; set; } = null!;
		public string ProductName { get; set; } = null!;
		public int VolumeMl { get; set; }
		public string ConcentrationName { get; set; } = null!;
		public bool IsExpired => ExpiryDate < DateTime.UtcNow;
		public int DaysUntilExpiry => (int)(ExpiryDate - DateTime.UtcNow).TotalDays;
	}
}
