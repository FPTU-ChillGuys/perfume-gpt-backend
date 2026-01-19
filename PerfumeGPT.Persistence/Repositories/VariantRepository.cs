using PerfumeGPT.Application.Interfaces.Repositories;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Persistence.Contexts;
using PerfumeGPT.Persistence.Repositories.Commons;

namespace PerfumeGPT.Persistence.Repositories
{
	public class VariantRepository : GenericRepository<ProductVariant>, IVariantRepository
	{
		public VariantRepository(PerfumeDbContext context) : base(context)
		{
		}
	}
}
