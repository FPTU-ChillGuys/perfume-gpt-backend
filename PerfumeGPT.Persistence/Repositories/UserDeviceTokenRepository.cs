using PerfumeGPT.Persistence.Contexts;
using PerfumeGPT.Persistence.Repositories.Commons;

namespace PerfumeGPT.Persistence.Repositories
{
	public class UserDeviceTokenRepository : GenericRepository<Domain.Entities.UserDeviceToken>, Application.Interfaces.Repositories.IUserDeviceTokenRepository
	{

		public UserDeviceTokenRepository(PerfumeDbContext context) : base(context)
		{
		}
	}
}
