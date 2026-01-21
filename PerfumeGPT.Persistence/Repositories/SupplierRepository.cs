using PerfumeGPT.Application.Interfaces.Repositories;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Persistence.Contexts;
using PerfumeGPT.Persistence.Repositories.Commons;

namespace PerfumeGPT.Persistence.Repositories
{
	public class SupplierRepository : GenericRepository<Supplier>, ISupplierRepository
	{
		public SupplierRepository(PerfumeDbContext context) : base(context)
		{
		}
	}
}
