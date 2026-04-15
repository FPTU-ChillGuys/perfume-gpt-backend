using Microsoft.EntityFrameworkCore;
using PerfumeGPT.Application.DTOs.Responses.SourcingCatalogs;
using PerfumeGPT.Application.Interfaces.Repositories;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Persistence.Contexts;
using PerfumeGPT.Persistence.Repositories.Commons;

namespace PerfumeGPT.Persistence.Repositories
{
	public class VariantSupplierRepository : GenericRepository<VariantSupplier>, IVariantSupplierRepository
	{
		public VariantSupplierRepository(PerfumeDbContext context) : base(context) { }

		public async Task<List<CatalogItemResponse>> GetCatalogsAsync(int? supplierId, Guid? variantId)
		=> await _context.VariantSuppliers
			.Where(vs => (!supplierId.HasValue || vs.SupplierId == supplierId.Value)
				&& (!variantId.HasValue || vs.ProductVariantId == variantId.Value)
				&& !vs.ProductVariant.IsDeleted)
			.OrderBy(vs => vs.ProductVariantId)
			.ThenByDescending(vs => vs.IsPrimary)
			.ThenBy(vs => vs.Supplier.Name)
			.Select(vs => new CatalogItemResponse
			{
				Id = vs.Id,
				ProductVariantId = vs.ProductVariantId,
				SupplierId = vs.SupplierId,
				SupplierName = vs.Supplier.Name,
				VariantSku = vs.ProductVariant.Sku,
				NegotiatedPrice = vs.NegotiatedPrice,
				EstimatedLeadTimeDays = vs.EstimatedLeadTimeDays,
				IsPrimary = vs.IsPrimary,
				CreatedAt = vs.CreatedAt,
				UpdatedAt = vs.UpdatedAt
			})
			.AsNoTracking()
			.ToListAsync();

		public async Task<List<VariantSupplier>> GetByVariantIdAsync(Guid variantId)
		=> await _context.VariantSuppliers
			.Where(vs => vs.ProductVariantId == variantId)
			.ToListAsync();

		public async Task<VariantSupplier?> GetByVariantAndSupplierAsync(Guid variantId, int supplierId)
		=> await _context.VariantSuppliers
			.FirstOrDefaultAsync(vs => vs.ProductVariantId == variantId && vs.SupplierId == supplierId);
	}
}
