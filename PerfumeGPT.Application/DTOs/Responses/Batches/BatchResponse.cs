namespace PerfumeGPT.Application.DTOs.Responses.Batches
{
	public record BatchResponse
	{
		public Guid Id { get; init; }
		public required string BatchCode { get; init; }
		public DateTime ManufactureDate { get; init; }
		public DateTime ExpiryDate { get; init; }
		public int ImportQuantity { get; init; }
		public int RemainingQuantity { get; init; }
		public DateTime CreatedAt { get; init; }
	}
}
