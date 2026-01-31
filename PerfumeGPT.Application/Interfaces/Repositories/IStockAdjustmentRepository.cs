using PerfumeGPT.Application.DTOs.Requests.StockAdjustments;
using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Domain.Entities;

namespace PerfumeGPT.Application.Interfaces.Repositories
{
	public interface IStockAdjustmentRepository : IGenericRepository<StockAdjustment>
	{
		Task<StockAdjustment?> GetByIdWithDetailsAsync(Guid id);
		Task<StockAdjustment?> GetByIdWithDetailsForDeleteAsync(Guid id);
		Task<(IEnumerable<StockAdjustment> Items, int TotalCount)> GetPagedWithDetailsAsync(GetPagedStockAdjustmentsRequest request);
	}
}
