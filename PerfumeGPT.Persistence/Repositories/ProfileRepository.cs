using PerfumeGPT.Application.Interfaces.Repositories;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Persistence.Contexts;
using PerfumeGPT.Persistence.Repositories.Commons;

namespace PerfumeGPT.Persistence.Repositories
{
    public class ProfileRepository : GenericRepository<CustomerProfile>, IProfileRepository
    {
        public ProfileRepository(PerfumeDbContext context) : base(context) { }
    }
}
