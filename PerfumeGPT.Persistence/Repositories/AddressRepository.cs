using Microsoft.EntityFrameworkCore;
using PerfumeGPT.Application.DTOs.Responses.Address;
using PerfumeGPT.Application.Interfaces.Repositories;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Persistence.Contexts;
using PerfumeGPT.Persistence.Repositories.Commons;

namespace PerfumeGPT.Persistence.Repositories
{
	public class AddressRepository : GenericRepository<Address>, IAddressRepository
	{
		public AddressRepository(PerfumeDbContext context) : base(context)
		{
		}

		public async Task<List<AddressResponse>> GetUserAddressesWithDetails(Guid userId)
		{
			var addresses = await _context.Addresses
				.Where(a => a.UserId == userId)
				.ToListAsync();

			var addressResponses = addresses.Select(a => new AddressResponse
			{
				Id = a.Id,
				ReceiverName = a.ReceiverName,
				Phone = a.Phone,
				Street = a.Street,
				Ward = a.Ward,
				District = a.District,
				City = a.City,
				IsDefault = a.IsDefault
			}).ToList();

			return addressResponses;
		}
	}
}
