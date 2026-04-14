using PerfumeGPT.Application.DTOs.Requests.Ledgers;
using PerfumeGPT.Application.DTOs.Responses.Ledgers;
using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Domain.Entities;

namespace PerfumeGPT.Application.Interfaces.Repositories
{
	public interface IInventoryLedgerRepository : IGenericRepository<InventoryLedger>
	{
		Task<(List<InventoryLedgerItemResponse> Items, int TotalCount)> GetPagedAsync(GetInventoryLedgersRequest request);
	}
}
