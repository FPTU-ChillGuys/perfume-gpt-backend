using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Domain.Entities;

namespace PerfumeGPT.Application.Interfaces.Repositories
{
	public interface IUserRepository : IGenericRepository<User>
	{
		Task<User?> FindByPhoneNumberAsync(string phoneNumber);
		Task<User?> FindByPhoneOrEmailAsync(string phoneOrEmail);
	}
}
