using PerfumeGPT.Application.Interfaces.Repositories;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Persistence.Contexts;
using PerfumeGPT.Persistence.Repositories.Commons;

namespace PerfumeGPT.Persistence.Repositories
{
    public class LoyaltyPointRepository : GenericRepository<LoyaltyPoint>, ILoyaltyPointRepository
    {
        public LoyaltyPointRepository(PerfumeDbContext context) : base(context)
        {

        }
    }
}
