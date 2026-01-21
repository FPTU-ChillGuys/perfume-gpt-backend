using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Domain.Entities;

namespace PerfumeGPT.Application.Interfaces.Repositories
{
	public interface IVariantRepository : IGenericRepository<ProductVariant>
	{
		Task<ProductVariant?> GetByBarcodeAsync(string barcode);
	}
}
