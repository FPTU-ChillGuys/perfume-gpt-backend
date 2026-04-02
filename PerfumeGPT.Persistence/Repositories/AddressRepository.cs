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
		public AddressRepository(PerfumeDbContext context) : base(context) { }

		public async Task<AddressResponse?> GetUserAddressById(Guid userId, Guid addressId)
		=> await _context.Addresses
			.Where(a => a.UserId == userId && a.Id == addressId)
           .Select(a => new AddressResponse
			{
				Id = a.Id,
				RecipientName = a.RecipientName,
				RecipientPhoneNumber = a.RecipientPhoneNumber,
				Street = a.Street,
				Ward = a.Ward,
				District = a.District,
				City = a.City,
				WardCode = a.WardCode,
				DistrictId = a.DistrictId,
				ProvinceId = a.ProvinceId,
				IsDefault = a.IsDefault
			})
			.FirstOrDefaultAsync();

		public async Task<AddressResponse?> GetDefaultAddressAsync(Guid userId)
		=> await _context.Addresses
			.Where(a => a.UserId == userId && a.IsDefault)
           .Select(a => new AddressResponse
			{
				Id = a.Id,
				RecipientName = a.RecipientName,
				RecipientPhoneNumber = a.RecipientPhoneNumber,
				Street = a.Street,
				Ward = a.Ward,
				District = a.District,
				City = a.City,
				WardCode = a.WardCode,
				DistrictId = a.DistrictId,
				ProvinceId = a.ProvinceId,
				IsDefault = a.IsDefault
			})
			.FirstOrDefaultAsync();

		public async Task<List<AddressResponse>> GetUserAddresses(Guid userId)
		=> await _context.Addresses
			.AsNoTracking()
			.Where(a => a.UserId == userId)
			.OrderByDescending(a => a.CreatedAt)
			.Select(a => new AddressResponse
			{
				Id = a.Id,
				RecipientName = a.RecipientName,
				RecipientPhoneNumber = a.RecipientPhoneNumber,
				Street = a.Street,
				Ward = a.Ward,
				District = a.District,
				City = a.City,

				WardCode = a.WardCode,
				DistrictId = a.DistrictId,
				ProvinceId = a.ProvinceId,

				IsDefault = a.IsDefault
			})
			.ToListAsync();
	}
}
