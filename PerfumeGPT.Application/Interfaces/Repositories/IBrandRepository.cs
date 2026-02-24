using PerfumeGPT.Application.DTOs.Responses.Brands;
using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Domain.Entities;

namespace PerfumeGPT.Application.Interfaces.Repositories
{
	public interface IBrandRepository : IGenericRepository<Brand>
	{
		Task<List<BrandLookupItem>> GetBrandLookupAsync();
	}
}
