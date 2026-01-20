using Microsoft.EntityFrameworkCore;
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

		public async Task<Address?> GetAddressByIdWithDetails(Guid userId, Guid addressId)
		{
			var address = await _context.Addresses
				.Where(a => a.UserId == userId && a.Id == addressId)
				.FirstOrDefaultAsync();

			return address;
		}

		public async Task<Address?> GetDefaultAddressWithDetails(Guid userId)
		{
			var address = await _context.Addresses
				.Where(a => a.UserId == userId && a.IsDefault)
				.FirstOrDefaultAsync();

			return address;
		}

		public async Task<List<Address>> GetUserAddressesWithDetails(Guid userId)
		{
			var addresses = await _context.Addresses
				.Where(a => a.UserId == userId)
				.ToListAsync();

			return addresses;
		}
	}
}
