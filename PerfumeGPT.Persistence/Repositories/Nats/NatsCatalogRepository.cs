using Microsoft.EntityFrameworkCore;
using PerfumeGPT.Application.DTOs.Responses.Nats;
using PerfumeGPT.Application.Interfaces.Repositories.Nats;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Persistence.Contexts;

namespace PerfumeGPT.Persistence.Repositories.Nats;

/// <summary>
/// NATS-specific repository implementation for Sourcing Catalog operations
/// Returns NATS-optimized DTOs that match AI backend expectations
/// </summary>
public sealed class NatsCatalogRepository : INatsCatalogRepository
{
	private readonly PerfumeDbContext _context;

	public NatsCatalogRepository(PerfumeDbContext context)
	{
		_context = context;
	}

	public async Task<List<NatsCatalogItemResponse>> GetCatalogsByVariantIdForNatsAsync(Guid variantId)
	{
		return await _context.VariantSuppliers
			.Where(vs => vs.ProductVariantId == variantId)
			.Select(vs => new NatsCatalogItemResponse
			{
				Id = vs.Id.ToString(),
				VariantId = vs.ProductVariantId.ToString(),
				SupplierId = vs.SupplierId,
				SupplierName = vs.Supplier.Name,
				BasePrice = vs.NegotiatedPrice,
				MinOrderQuantity = vs.EstimatedLeadTimeDays,
				LeadTimeDays = vs.EstimatedLeadTimeDays,
				IsPrimary = vs.IsPrimary
			})
			.AsNoTracking()
			.ToListAsync();
	}
}
