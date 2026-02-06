using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Domain.Entities;

namespace PerfumeGPT.Application.Interfaces.Repositories
{
	public interface IAddressRepository : IGenericRepository<Address>
	{
		Task<List<Address>> GetUserAddresses(Guid userId);
		Task<Address?> GetUserAddressById(Guid userId, Guid addressId);
		Task<Address?> GetDefaultAddress(Guid userId);
	}
}
