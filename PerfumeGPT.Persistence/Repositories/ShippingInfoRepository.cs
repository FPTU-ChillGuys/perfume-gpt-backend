using PerfumeGPT.Application.Interfaces.Repositories;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Persistence.Contexts;
using PerfumeGPT.Persistence.Repositories.Commons;

namespace PerfumeGPT.Persistence.Repositories
{
	public class ShippingInfoRepository : GenericRepository<ShippingInfo>, IShippingInfoRepository
	{
		public ShippingInfoRepository(PerfumeDbContext context) : base(context)
		{
		}
	}
}
