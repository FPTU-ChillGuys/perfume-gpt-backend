using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.DTOs.Responses.Imports
{
	public record ImportTicketListItem
	{
		public Guid Id { get; init; }
		public required string CreatedByName { get; init; }
		public string? VerifiedByName { get; init; }
		public required string SupplierName { get; init; }
		public DateTime ExpectedArrivalDate { get; init; }
		public DateTime ActualImportDate { get; init; }
		public decimal TotalCost { get; init; }
		public ImportStatus Status { get; init; }
		public int TotalItems { get; init; }
		public DateTime CreatedAt { get; init; }
	}
}
