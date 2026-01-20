using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Domain.Entities;

namespace PerfumeGPT.Application.Interfaces.Repositories
{
	public interface IAddressRepository : IGenericRepository<Address>
	{
		Task<List<Address>> GetUserAddressesWithDetails(Guid userId);
		Task<Address?> GetAddressByIdWithDetails(Guid userId, Guid addressId);
		Task<Address?> GetDefaultAddressWithDetails(Guid userId);
	}
}
