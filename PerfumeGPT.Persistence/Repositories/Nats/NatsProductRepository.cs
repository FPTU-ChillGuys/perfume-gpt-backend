using Microsoft.EntityFrameworkCore;
using PerfumeGPT.Application.DTOs.Responses.Nats;
using PerfumeGPT.Application.Interfaces.Repositories.Nats;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Persistence.Contexts;
using PerfumeGPT.Persistence.Repositories.Commons;

namespace PerfumeGPT.Persistence.Repositories.Nats;

/// <summary>
/// NATS-specific repository implementation for Product operations
/// Returns NATS-optimized DTOs that match AI backend expectations
/// </summary>
public sealed class NatsProductRepository : GenericRepository<Product>, INatsProductRepository
{
	public NatsProductRepository(PerfumeDbContext context) : base(context) { }

	public async Task<List<NatsProductResponse>> GetProductsByIdsForNatsAsync(IEnumerable<Guid> productIds)
	{
		var productIdList = productIds.ToList();
		if (!productIdList.Any())
		{
			return [];
		}

		return await _context.Products
			.Where(p => !p.IsDeleted && productIdList.Contains(p.Id))
			.Select(p => new NatsProductResponse
			{
				Id = p.Id.ToString(),
				Name = p.Name,
				Gender = p.Gender.ToString(),
				Origin = p.Origin,
				ReleaseYear = p.ReleaseYear,
				BrandId = p.BrandId,
				BrandName = p.Brand.Name,
				CategoryId = p.CategoryId,
				CategoryName = p.Category.Name,
				Description = p.Description,
				NumberOfVariants = p.Variants.Count(v => !v.IsDeleted),
				Media = p.Media
					.Where(m => !m.IsDeleted)
					.Select(m => new NatsProductMediaResponse
					{
						Id = m.Id.ToString(),
						Url = m.Url,
						ThumbnailUrl = null,
						Type = "Image"
					}).ToList(),
				Variants = p.Variants
					.Where(v => !v.IsDeleted)
					.Select(v => new NatsProductVariantResponse
					{
						Id = v.Id.ToString(),
						Sku = v.Sku,
						VolumeMl = v.VolumeMl,
						ConcentrationName = v.Concentration.Name,
						Type = v.Type.ToString(),
						BasePrice = v.BasePrice,
						RetailPrice = v.RetailPrice,
						StockQuantity = v.Stock != null ? v.Stock.TotalQuantity : 0,
						ProductName = p.Name ?? string.Empty,
						Media = v.Media
							.Where(m => !m.IsDeleted)
							.Select(m => new NatsProductMediaResponse
							{
								Id = m.Id.ToString(),
								Url = m.Url,
								ThumbnailUrl = null,
								Type = "Image"
							}).ToList(),
						CampaignName = null,
						VoucherCode = null,
						DiscountedPrice = null
					}).ToList(),
				Attributes = p.ProductAttributes
					.Where(pa => pa.ProductId == p.Id)
					.Select(pa => new NatsProductAttributeResponse
					{
						AttributeId = pa.AttributeId,
						Name = pa.Attribute.Name,
						Value = pa.ValueId.ToString()
					}).ToList(),
				OlfactoryFamilies = p.ProductFamilyMaps
					.Select(pfm => new NatsProductOlfactoryFamilyResponse
					{
						OlfactoryFamilyId = pfm.OlfactoryFamilyId,
						Name = pfm.OlfactoryFamily.Name
					}).ToList(),
				ScentNotes = p.ProductScentMaps
					.Select(psm => new NatsProductScentNoteResponse
					{
						NoteId = psm.ScentNoteId,
						Name = psm.ScentNote.Name,
						NoteType = "ScentNote"
					}).ToList()
			})
			.AsNoTracking()
			.ToListAsync();
	}

	public async Task<NatsProductResponse?> GetProductByIdForNatsAsync(Guid productId)
	{
		var products = await GetProductsByIdsForNatsAsync([productId]);
		return products.FirstOrDefault();
	}
}
