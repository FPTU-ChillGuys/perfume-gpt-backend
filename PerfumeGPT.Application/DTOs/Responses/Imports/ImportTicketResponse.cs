using PerfumeGPT.Application.DTOs.Responses.Batches;
using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.DTOs.Responses.Imports
{
	public record ImportTicketResponse
	{
		public Guid Id { get; init; }
		public required string CreatedByName { get; init; }
		public string? VerifiedByName { get; init; }
		public int SupplierId { get; init; }
		public required string SupplierName { get; init; }
		public DateTime ExpectedArrivalDate { get; init; }
		public DateTime? ActualImportDate { get; init; }
		public decimal TotalCost { get; init; }
		public ImportStatus Status { get; init; }
		public DateTime CreatedAt { get; init; }
		public required List<ImportDetailResponse> ImportDetails { get; init; }
	}

	public record ImportDetailResponse
	{
		public Guid Id { get; init; }
		public Guid VariantId { get; init; }
		public required string VariantName { get; init; }
		public required string VariantSku { get; init; }
		public int ExpectedQuantity { get; init; }
		public decimal UnitPrice { get; init; }
		public decimal TotalPrice { get; init; }
		public int RejectedQuantity { get; init; }
		public string? Note { get; init; }
		public required List<BatchResponse> Batches { get; init; }
	}
}
