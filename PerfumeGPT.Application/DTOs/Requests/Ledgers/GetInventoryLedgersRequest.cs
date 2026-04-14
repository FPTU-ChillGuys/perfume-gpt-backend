using PerfumeGPT.Application.DTOs.Requests.Base;
using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.DTOs.Requests.Ledgers
{
	public record GetInventoryLedgersRequest : PagingAndSortingQuery
	{
		public DateTime? FromDate { get; init; }
		public DateTime? ToDate { get; init; }
		public Guid? VariantId { get; init; }
		public Guid? BatchId { get; init; }
		public StockTransactionType? Type { get; init; }
		public Guid? ReferenceId { get; init; }
		public Guid? ActorId { get; init; }
	}
}
