using PerfumeGPT.Application.DTOs.Requests.Ledgers;
using PerfumeGPT.Application.DTOs.Responses.Ledgers;
using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Domain.Entities;

namespace PerfumeGPT.Application.Interfaces.Repositories
{
	public interface ICashFlowLedgerRepository : IGenericRepository<CashFlowLedger>
	{
		Task<(List<CashFlowLedgerItemResponse> Items, int TotalCount)> GetPagedAsync(GetCashFlowLedgersRequest request);
	}
}
