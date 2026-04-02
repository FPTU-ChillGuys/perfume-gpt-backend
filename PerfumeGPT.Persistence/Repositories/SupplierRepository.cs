using PerfumeGPT.Application.Interfaces.Repositories;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Persistence.Contexts;
using PerfumeGPT.Persistence.Repositories.Commons;
using Microsoft.EntityFrameworkCore;
using PerfumeGPT.Application.DTOs.Responses.Metadatas.Suppliers;

namespace PerfumeGPT.Persistence.Repositories
{
	public class SupplierRepository : GenericRepository<Supplier>, ISupplierRepository
	{
		public SupplierRepository(PerfumeDbContext context) : base(context) { }

		public async Task<List<SupplierLookupItem>> GetSupplierLookupListAsync()
		=> await _context.Suppliers
			.AsNoTracking()
			.OrderBy(s => s.Name)
			.Select(s => new SupplierLookupItem
			{
				Id = s.Id,
				Name = s.Name,
				Phone = s.Phone,
				ContactEmail = s.ContactEmail
			})
			.ToListAsync();

		public async Task<List<SupplierResponse>> GetAllSuppliersAsync()
		=> await _context.Suppliers
			.AsNoTracking()
			.OrderBy(s => s.Name)
		  .Select(s => new SupplierResponse
		  {
			  Id = s.Id,
			  Name = s.Name,
			  ContactEmail = s.ContactEmail,
			  Phone = s.Phone,
			  Address = s.Address
		  })
			.ToListAsync();

		public async Task<SupplierResponse?> GetSupplierByIdAsync(int id)
		=> await _context.Suppliers
			.AsNoTracking()
			.Where(s => s.Id == id)
		  .Select(s => new SupplierResponse
		  {
			  Id = s.Id,
			  Name = s.Name,
			  ContactEmail = s.ContactEmail,
			  Phone = s.Phone,
			  Address = s.Address
		  })
			.FirstOrDefaultAsync();

		public async Task<bool> HasImportTicketsAsync(int supplierId)
		=> await _context.ImportTickets.AnyAsync(i => i.SupplierId == supplierId);
	}
}
