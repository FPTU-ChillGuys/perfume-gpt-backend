using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Domain.Entities;

namespace PerfumeGPT.Application.Interfaces.Repositories
{
	public interface IBatchRepository : IGenericRepository<Batch>
	{
		Task<bool> DeductBathAsync(Guid variantId, int quantity);
		Task<bool> IsValidForDeductionAsync(Guid variantId, int requiredQuantity);
		Task<List<Batch>> GetAvailableBatchesByVariantAsync(Guid variantId);
	}
}

