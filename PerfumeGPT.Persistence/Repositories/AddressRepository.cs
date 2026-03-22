using Microsoft.EntityFrameworkCore;
using PerfumeGPT.Application.Interfaces.Repositories;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Persistence.Contexts;
using PerfumeGPT.Persistence.Repositories.Commons;

namespace PerfumeGPT.Persistence.Repositories
{
	public class AddressRepository : GenericRepository<Address>, IAddressRepository
	{
		public AddressRepository(PerfumeDbContext context) : base(context) { }

		public async Task<Address?> GetUserAddressById(Guid userId, Guid addressId)
		=> await _context.Addresses
			.Where(a => a.UserId == userId && a.Id == addressId)
			.FirstOrDefaultAsync();

		public async Task<Address?> GetDefaultAddress(Guid userId)
		=> await _context.Addresses
			.Where(a => a.UserId == userId && a.IsDefault)
			.FirstOrDefaultAsync();

		public async Task<List<Address>> GetUserAddresses(Guid userId)
		=> await _context.Addresses
			.AsNoTracking()
			.Where(a => a.UserId == userId)
			.OrderByDescending(a => a.CreatedAt)
			.ToListAsync();
	}
}
