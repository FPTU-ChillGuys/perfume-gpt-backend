using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.DTOs.Responses.Imports
{
	public class ImportTicketResponse
	{
		public Guid Id { get; set; }
		public Guid CreatedById { get; set; }
		public string CreatedByName { get; set; } = null!;
		public Guid? VerifiedById { get; set; }
		public string? VerifiedByName { get; set; }
		public int SupplierId { get; set; }
		public string SupplierName { get; set; } = null!;
		public DateTime ImportDate { get; set; }
		public decimal TotalCost { get; set; }
		public ImportStatus Status { get; set; }
		public DateTime CreatedAt { get; set; }
		public List<ImportDetailResponse> ImportDetails { get; set; } = [];
	}

	public class ImportDetailResponse
	{
		public Guid Id { get; set; }
		public Guid VariantId { get; set; }
		public string VariantName { get; set; } = null!;
		public string VariantSku { get; set; } = null!;
		public int Quantity { get; set; }
		public decimal UnitPrice { get; set; }
		public decimal TotalPrice { get; set; }
		public int RejectQuantity { get; set; }
		public string? Note { get; set; }
		public List<BatchResponse> Batches { get; set; } = [];
	}

	public class BatchResponse
	{
		public Guid Id { get; set; }
		public string BatchCode { get; set; } = null!;
		public DateTime ManufactureDate { get; set; }
		public DateTime ExpiryDate { get; set; }
		public int ImportQuantity { get; set; }
		public int RemainingQuantity { get; set; }
		public DateTime CreatedAt { get; set; }
	}
}
