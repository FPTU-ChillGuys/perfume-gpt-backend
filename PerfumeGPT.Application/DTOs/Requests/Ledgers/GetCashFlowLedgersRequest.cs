using PerfumeGPT.Application.DTOs.Requests.Base;
using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.DTOs.Requests.Ledgers
{
	public record GetCashFlowLedgersRequest : PagingAndSortingQuery
	{
		public DateTime? FromDate { get; init; }
		public DateTime? ToDate { get; init; }
		public CashFlowType? FlowType { get; init; }
		public CashFlowCategory? Category { get; init; }
		public Guid? ReferenceId { get; init; }
		public string? ReferenceCode { get; init; }
	}
}
