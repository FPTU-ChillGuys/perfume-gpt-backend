namespace PerfumeGPT.Application.DTOs.Responses.Batches
{
	public class BatchLookupResponse
	{
		public Guid Id { get; set; }
		public string BatchCode { get; set; } = string.Empty;
		public Guid VariantId { get; set; }
		public string SKU { get; set; } = string.Empty;
	}
}
