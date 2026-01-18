using PerfumeGPT.Application.DTOs.Responses.Address;
using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Domain.Entities;

namespace PerfumeGPT.Application.Interfaces.Repositories
{
	public interface IAddressRepository : IGenericRepository<Address>
	{
		Task<List<AddressResponse>> GetUserAddressesWithDetails(Guid userId);
	}
}
