using PerfumeGPT.Application.Interfaces.Repositories;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Persistence.Contexts;
using PerfumeGPT.Persistence.Repositories.Commons;

namespace PerfumeGPT.Persistence.Repositories
{
	public class StockAdjustmentDetailRepository : GenericRepository<StockAdjustmentDetail>, IStockAdjustmentDetailRepository
	{
		public StockAdjustmentDetailRepository(PerfumeDbContext context) : base(context)
		{
		}
	}
}
