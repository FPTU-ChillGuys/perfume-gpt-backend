using PerfumeGPT.Application.Interfaces.Repositories;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Persistence.Contexts;
using PerfumeGPT.Persistence.Repositories.Commons;

namespace PerfumeGPT.Persistence.Repositories
{
	public class UserVoucherRepository : GenericRepository<UserVoucher>, IUserVoucherRepository
	{
		public UserVoucherRepository(PerfumeDbContext context) : base(context)
		{
		}
	}
}
