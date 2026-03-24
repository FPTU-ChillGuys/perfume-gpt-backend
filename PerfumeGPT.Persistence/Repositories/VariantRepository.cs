using Microsoft.EntityFrameworkCore;
using PerfumeGPT.Application.DTOs.Requests.Variants;
using PerfumeGPT.Application.DTOs.Responses.Media;
using PerfumeGPT.Application.DTOs.Responses.ProductAttributes;
using PerfumeGPT.Application.DTOs.Responses.Variants;
using PerfumeGPT.Application.Interfaces.Repositories;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Domain.Enums;
using PerfumeGPT.Persistence.Contexts;
using PerfumeGPT.Persistence.Repositories.Commons;

namespace PerfumeGPT.Persistence.Repositories
{
	public class VariantRepository : GenericRepository<ProductVariant>, IVariantRepository
	{
		public VariantRepository(PerfumeDbContext context) : base(context) { }

		public async Task<List<VariantLookupItem>> GetLookupList(Guid? productId = null)
		{
			return await _context.ProductVariants
				.Where(v => v.Status != VariantStatus.Discontinued
					&& (!productId.HasValue || v.ProductId == productId.Value))
				.Select(v => new VariantLookupItem
				{
					Id = v.Id,
					Sku = v.Sku,
					Barcode = v.Barcode,
					DisplayName = $"{v.Product.Name ?? "Unknown"} - {v.VolumeMl}ml {v.Concentration.Name ?? "Unknown"}",
					VolumeMl = v.VolumeMl,
					ConcentrationName = v.Concentration.Name ?? "Unknown",
					BasePrice = v.BasePrice,
					PrimaryImageUrl = v.Media
						.Where(m => m.IsPrimary && !m.IsDeleted)
						.Select(m => m.Url)
						.FirstOrDefault()
				})
				.AsNoTracking()
				.ToListAsync();
		}

		public async Task<ProductVariantResponse?> GetByBarcodeAsync(string barcode)
		{
			return await _context.ProductVariants
				.Where(v => v.Barcode == barcode)
				.Select(v => new ProductVariantResponse
				{
					Id = v.Id,
					ProductId = v.ProductId,
					ProductName = v.Product.Name,
					Barcode = v.Barcode,
					Sku = v.Sku,
					VolumeMl = v.VolumeMl,
					ConcentrationId = v.ConcentrationId,
					ConcentrationName = v.Concentration.Name,
					Type = v.Type,
					BasePrice = v.BasePrice,
					RetailPrice = v.RetailPrice,
					Status = v.Status,
					Sillage = v.Sillage,
					Longevity = v.Longevity,
					StockQuantity = v.Stock.TotalQuantity - v.Stock.ReservedQuantity,
					Media = v.Media.Where(m => !m.IsDeleted)
						.Select(m => new MediaResponse
						{
							Id = m.Id,
							Url = m.Url,
							AltText = m.AltText,
							IsPrimary = m.IsPrimary,
							DisplayOrder = m.DisplayOrder,
							MimeType = m.MimeType,
							FileSize = m.FileSize
						}).ToList(),
					Attributes = v.ProductAttributes
						.Select(pa => new ProductAttributeResponse
						{
							AttributeId = pa.AttributeId,
							ValueId = pa.ValueId,
							Attribute = pa.Attribute.Name,
							Value = pa.Value.Value
						}).ToList()
				})
				.AsSplitQuery()
				.AsNoTracking()
				.FirstOrDefaultAsync();
		}

		public async Task<ProductVariant?> GetBySkuAsync(string sku)
			=> await _context.ProductVariants
				.Where(v => v.Sku == sku)
				.FirstOrDefaultAsync();

		public async Task<ProductVariantResponse?> GetVariantWithDetailsAsync(Guid variantId)
		{
			var now = DateTime.UtcNow;

			var result = await _context.ProductVariants
				.Where(v => v.Id == variantId)
			 .Select(v => new
			 {
				 Item = new ProductVariantResponse
				 {
					 Id = v.Id,
					 ProductId = v.ProductId,
					 ProductName = v.Product.Name,
					 Barcode = v.Barcode,
					 Sku = v.Sku,
					 VolumeMl = v.VolumeMl,
					 ConcentrationId = v.ConcentrationId,
					 ConcentrationName = v.Concentration.Name,
					 Type = v.Type,
					 BasePrice = v.BasePrice,
					 RetailPrice = v.RetailPrice,
					 Status = v.Status,
					 Sillage = v.Sillage,
					 Longevity = v.Longevity,
					 StockQuantity = v.Stock.TotalQuantity - v.Stock.ReservedQuantity,
					 Media = v.Media.Where(m => !m.IsDeleted)
							.Select(m => new MediaResponse
							{
								Id = m.Id,
								Url = m.Url,
								AltText = m.AltText,
								IsPrimary = m.IsPrimary,
								DisplayOrder = m.DisplayOrder,
								MimeType = m.MimeType,
								FileSize = m.FileSize
							}).ToList(),
					 Attributes = v.ProductAttributes
							.Select(pa => new ProductAttributeResponse
							{
								AttributeId = pa.AttributeId,
								ValueId = pa.ValueId,
								Attribute = pa.Attribute.Name,
								Value = pa.Value.Value
							}).ToList()
				 },
				 ActiveCampaign = v.PromotionItems
						.Where(pi =>
							!pi.IsDeleted &&
							!pi.Campaign.IsDeleted &&
							pi.Campaign.Status == CampaignStatus.Active &&
							pi.Campaign.StartDate <= now &&
							pi.Campaign.EndDate >= now &&
							(pi.IsActive))
						.OrderByDescending(pi => pi.CreatedAt)
						.Select(pi => new
						{
							CampaignName = pi.Campaign.Name,
							Voucher = pi.Campaign.Vouchers
								.Where(voucher =>
									!voucher.IsDeleted &&
									voucher.ExpiryDate >= now &&
									voucher.ApplyType == VoucherType.Product &&
									(voucher.RemainingQuantity == null || voucher.RemainingQuantity > 0) &&
									(!voucher.TargetItemType.HasValue || voucher.TargetItemType == pi.ItemType))
								.OrderByDescending(voucher => voucher.CreatedAt)
								.Select(voucher => new
								{
									voucher.Code,
									voucher.DiscountType,
									voucher.DiscountValue
								})
								.FirstOrDefault()
						})
						.FirstOrDefault()
			 })
				.AsSplitQuery()
				.AsNoTracking()
				.FirstOrDefaultAsync();

			if (result?.Item == null)
				return null;

			result.Item.CampaignName = result.ActiveCampaign?.CampaignName;
			result.Item.VoucherCode = result.ActiveCampaign?.Voucher?.Code;

			if (result.ActiveCampaign?.Voucher != null)
			{
				var discounted = result.ActiveCampaign.Voucher.DiscountType == DiscountType.Percentage
					? result.Item.BasePrice - (result.Item.BasePrice * result.ActiveCampaign.Voucher.DiscountValue / 100m)
					: result.Item.BasePrice - result.ActiveCampaign.Voucher.DiscountValue;

				result.Item.DiscountedPrice = discounted < 0 ? 0 : discounted;
			}

			return result.Item;
		}

		public async Task<VariantCreateOrder?> GetVariantForCreateOrderAsync(Guid variantId)
			=> await _context.ProductVariants
				.Where(v => v.Id == variantId)
				.Select(v => new VariantCreateOrder
				{
					Id = v.Id,
					UnitPrice = v.BasePrice,
					Snapshot = $"{v.Product.Name} - {v.VolumeMl}ml - {v.Concentration.Name} - {v.Type}"
				})
				.AsNoTracking()
				.FirstOrDefaultAsync();

		public async Task<(List<VariantPagedItem> Items, int TotalCount)> GetPagedVariantsWithDetailsAsync(
		GetPagedVariantsRequest request)
		{
			var query = _context.ProductVariants.AsQueryable();

			var totalCount = await query.CountAsync();

			var items = await query
				.OrderByDescending(v => v.CreatedAt)
				.Skip((request.PageNumber - 1) * request.PageSize)
				.Take(request.PageSize)
				.Select(v => new VariantPagedItem
				{
					Id = v.Id,
					ProductId = v.ProductId,
					Barcode = v.Barcode,
					Sku = v.Sku,
					VolumeMl = v.VolumeMl,
					ConcentrationId = v.ConcentrationId,
					ConcentrationName = v.Concentration.Name,
					Type = v.Type,
					BasePrice = v.BasePrice,
					RetailPrice = v.RetailPrice,
					Status = v.Status,
					StockQuantity = v.Stock.TotalQuantity - v.Stock.ReservedQuantity,
					PrimaryImageUrl = v.Media
						.Where(m => m.IsPrimary && !m.IsDeleted)
						.Select(m => m.Url).FirstOrDefault(),
					Attributes = v.ProductAttributes
						.Select(pa => new ProductAttributeResponse
						{
							AttributeId = pa.AttributeId,
							ValueId = pa.ValueId,
							Attribute = pa.Attribute.Name,
							Value = pa.Value.Value
						}).ToList()
				})
				.AsNoTracking()
				.ToListAsync();

			return (items, totalCount);
		}

		public async Task<List<Guid>> GetExistingIdsAsync(List<Guid> ids)
			=> await _context.ProductVariants
				.Where(v => ids.Contains(v.Id))
				.Select(v => v.Id)
				.ToListAsync();
	}
}

