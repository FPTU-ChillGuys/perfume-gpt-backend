using PerfumeGPT.Application.DTOs.Requests.Inventory.Batches;

namespace PerfumeGPT.Application.DTOs.Requests.Imports.ImportDetails
{
	public record VerifyImportDetailRequest
	{
		public Guid ImportDetailId { get; init; }
		public int RejectedQuantity { get; init; }
		public string? Note { get; init; }
		public required List<CreateBatchRequest> Batches { get; init; }
	}
}
