using PerfumeGPT.Application.DTOs.Requests.StockAdjustments;
using PerfumeGPT.Application.DTOs.Responses.StockAdjustments;
using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Domain.Entities;

namespace PerfumeGPT.Application.Interfaces.Repositories
{
	public interface IStockAdjustmentRepository : IGenericRepository<StockAdjustment>
	{
		Task<StockAdjustmentResponse?> GetByIdToViewAsync(Guid id);
		Task<StockAdjustment?> GetByIdWithDetailsAsync(Guid id);
		Task<(IEnumerable<StockAdjustmentListItem> Items, int TotalCount)> GetPagedAsync(GetPagedStockAdjustmentsRequest request);
	}
}
