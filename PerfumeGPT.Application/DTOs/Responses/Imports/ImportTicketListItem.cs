using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.DTOs.Responses.Imports
{
	public class ImportTicketListItem
	{
		public Guid Id { get; set; }
		public string CreatedByName { get; set; } = null!;
		public string SupplierName { get; set; } = null!;
		public DateTime ImportDate { get; set; }
		public decimal TotalCost { get; set; }
		public ImportStatus Status { get; set; }
		public int TotalItems { get; set; }
		public DateTime CreatedAt { get; set; }
	}
}
