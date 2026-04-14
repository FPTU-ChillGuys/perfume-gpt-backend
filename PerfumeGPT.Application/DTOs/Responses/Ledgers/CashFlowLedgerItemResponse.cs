using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.DTOs.Responses.Ledgers
{
	public record CashFlowLedgerItemResponse
	{
		public Guid Id { get; init; }
		public DateTime TransactionDate { get; init; }
		public decimal Amount { get; init; }
		public CashFlowType FlowType { get; init; }
		public CashFlowCategory Category { get; init; }
		public Guid ReferenceId { get; init; }
		public string? ReferenceCode { get; init; }
		public string? Description { get; init; }
	}
}
