namespace PerfumeGPT.Application.DTOs.Responses.Batches
{
	public record BatchDetailResponse : BatchResponse
	{
		public Guid VariantId { get; init; }
		public required string VariantSku { get; init; }
		public required string ProductName { get; init; }
		public int VolumeMl { get; init; }
		public required string ConcentrationName { get; init; }
		public bool IsExpired => ExpiryDate < DateTime.UtcNow;
		public int DaysUntilExpiry => (int)(ExpiryDate - DateTime.UtcNow).TotalDays;
	}
}
