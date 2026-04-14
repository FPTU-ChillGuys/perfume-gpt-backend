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
		=> await _context.ProductVariants
			  .Where(v => !v.IsDeleted
					&& v.Status != VariantStatus.Discontinued
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

		public async Task<ProductVariantResponse?> GetByBarcodeAsync(string barcode)
		=> await _context.ProductVariants
			   .Where(v => !v.IsDeleted && v.Barcode == barcode)
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
						.ToList()
				})
				.AsSplitQuery()
				.AsNoTracking()
				.FirstOrDefaultAsync();

		public async Task<ProductVariantForPosResponse?> GetVariantByInfoAsync(string keyword)
		{
			keyword = keyword.Trim();

			var query = _context.ProductVariants
				.Where(v => !v.IsDeleted && v.Status == VariantStatus.Active)
				.AsQueryable();
			query = query.Where(v =>
				(v.Barcode != null && v.Barcode.Contains(keyword)) ||
				(v.Sku != null && v.Sku.Contains(keyword)) ||
				(v.Product.Name != null && v.Product.Name.Contains(keyword)));

			return await query
				.OrderByDescending(v => v.CreatedAt)
				.Select(v => new ProductVariantForPosResponse
				{
					Id = v.Id,
					Barcode = v.Barcode,
					Sku = v.Sku,
					Name = v.Product.Name,
					VolumeMl = v.VolumeMl,
					ConcentrationName = v.Concentration.Name,
					DisplayName = $"{v.Product.Name} - {v.VolumeMl}ml - {v.Concentration.Name}",
					BasePrice = v.BasePrice,
					PrimaryImageUrl = v.Media
						.Where(m => m.IsPrimary && !m.IsDeleted)
						.Select(m => m.Url)
						.FirstOrDefault()
				})
				.AsNoTracking()
				.FirstOrDefaultAsync();
		}

		public async Task<ProductVariant?> GetBySkuAsync(string sku)
		=> await _context.ProductVariants
		   .Where(v => !v.IsDeleted && v.Sku == sku)
			.FirstOrDefaultAsync();

		public async Task<ProductVariant?> GetByIdWithAttributesAsync(Guid variantId)
		=> await _context.ProductVariants
			.Where(v => !v.IsDeleted && v.Id == variantId)
			.Include(v => v.ProductAttributes)
			.FirstOrDefaultAsync();

		public async Task<ProductVariant?> GetByIdWithSuppliersAsync(Guid variantId)
		=> await _context.ProductVariants
			.Where(v => !v.IsDeleted && v.Id == variantId)
			.Include(v => v.Suppliers)
			.FirstOrDefaultAsync();

		public async Task<List<ProductVariant>> GetVariantsWithDetailsByIdsAsync(IEnumerable<Guid> variantIds)
		=> await _context.ProductVariants
				.Where(v => !v.IsDeleted && variantIds.Contains(v.Id))
				.Include(v => v.Product)
				.Include(v => v.Concentration)
				.AsNoTracking()
				.ToListAsync();

		public async Task<ProductVariantResponse?> GetVariantWithDetailsAsync(Guid variantId)
		{
			var now = DateTime.UtcNow;

			var raw = await _context.ProductVariants
		.AsNoTracking()
		.AsSplitQuery()
		.Where(v => !v.IsDeleted && v.Id == variantId)
		.Select(v => new
		{
			Variant = new ProductVariantResponse
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
				StockQuantity = v.Stock != null ? v.Stock.TotalQuantity - v.Stock.ReservedQuantity : 0,
				Media = v.Media
							.Where(m => !m.IsDeleted)
							.OrderBy(m => m.DisplayOrder)
							.Select(m => new MediaResponse
							{
								Id = m.Id,
								Url = m.Url,
								AltText = m.AltText,
								IsPrimary = m.IsPrimary,
								DisplayOrder = m.DisplayOrder,
								MimeType = m.MimeType,
								FileSize = m.FileSize
							})
							.ToList(),
				Attributes = v.ProductAttributes
							.Select(pa => new ProductAttributeResponse
							{
								AttributeId = pa.AttributeId,
								ValueId = pa.ValueId,
								Attribute = pa.Attribute.Name,
								Value = pa.Value.Value
							})
						   .ToList(),
				Suppliers = v.Suppliers
							.OrderByDescending(s => s.IsPrimary)
							.ThenBy(s => s.Supplier.Name)
							.Select(s => new VariantSupplierResponse
							{
								SupplierId = s.SupplierId,
								SupplierName = s.Supplier.Name,
								NegotiatedPrice = s.NegotiatedPrice,
								EstimatedLeadTimeDays = s.EstimatedLeadTimeDays,
								IsPrimary = s.IsPrimary
							})
							.ToList()
			},

			BestPromotion = v.PromotionItems
				.Where(pi =>
					!pi.IsDeleted &&
					pi.IsActive &&
					!pi.Campaign.IsDeleted &&
					pi.Campaign.Status == CampaignStatus.Active &&
					pi.Campaign.StartDate <= now &&
					pi.Campaign.EndDate >= now &&
					(!pi.MaxUsage.HasValue || pi.CurrentUsage < pi.MaxUsage.Value)) // Chặn số lượt
				.Select(pi => new
				{
					CampaignName = pi.Campaign.Name,
					EndDate = pi.Campaign.EndDate,
					DiscountType = pi.DiscountType,
					DiscountValue = pi.DiscountValue,

					// MAGIC Ở ĐÂY: Ép EF Core tính ra số tiền giảm thực tế để dễ so sánh
					CalculatedDiscountAmount = pi.DiscountType == DiscountType.Percentage
						? (v.BasePrice * pi.DiscountValue / 100m)
						: pi.DiscountValue
				})
				.OrderByDescending(x => x.CalculatedDiscountAmount) // Giờ thì so sánh cực kỳ chính xác!
				.FirstOrDefault(),

			// Lấy voucher tốt nhất từ campaign active duy nhất
			AvailableVouchers = v.PromotionItems
						.Where(pi =>
							!pi.IsDeleted &&
							pi.IsActive &&
							!pi.Campaign.IsDeleted &&
							pi.Campaign.Status == CampaignStatus.Active &&
							pi.Campaign.StartDate <= now &&
							pi.Campaign.EndDate >= now)
						.SelectMany(pi => pi.Campaign.Vouchers)
				.Where(voucher => !voucher.IsDeleted && voucher.ExpiryDate >= now && (voucher.RemainingQuantity == null || voucher.RemainingQuantity > 0))
                .GroupBy(voucher => voucher.Code)
				.Select(g => g.Key)
				.ToList()
		})
		.FirstOrDefaultAsync();

			if (raw?.Variant == null)
				return null;

			var response = raw.Variant;

			// Apply best promotion to calculate discounted price
			if (raw.BestPromotion != null)
			{
				var safeDiscount = Math.Min(raw.BestPromotion.CalculatedDiscountAmount, response.BasePrice);
				var discountedPrice = Math.Round(response.BasePrice - safeDiscount, 2, MidpointRounding.AwayFromZero);

				response = response with
				{
					DiscountedPrice = discountedPrice,
					CampaignName = raw.BestPromotion.CampaignName,
					VoucherCode = raw.AvailableVouchers.FirstOrDefault()
				};
			}
			else
			{
				response = response with
				{
					DiscountedPrice = response.BasePrice,
					VoucherCode = raw.AvailableVouchers.FirstOrDefault()
				};
			}

			return response;
		}

		public async Task<VariantCreateOrder?> GetVariantForCreateOrderAsync(Guid variantId)
		=> await _context.ProductVariants
		  .Where(v => !v.IsDeleted && v.Id == variantId)
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
			var query = _context.ProductVariants
				   .Where(v => !v.IsDeleted)
				   .AsQueryable();

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

		public async Task<(List<VariantPagedItem> Items, int TotalCount)> GetPagedVariantsByCampaignIdAsync(
			Guid campaignId,
			GetPagedVariantsRequest request)
		{
			var now = DateTime.UtcNow;

			var query = _context.ProductVariants
				.Where(v => !v.IsDeleted && v.PromotionItems.Any(pi => !pi.IsDeleted && pi.CampaignId == campaignId));

			var totalCount = await query.CountAsync();

			var rawItems = await query
				   .OrderByDescending(v => v.CreatedAt)
				   .Skip((request.PageNumber - 1) * request.PageSize)
				   .Take(request.PageSize)
				  .Select(v => new
				  {
					  Variant = new VariantPagedItem
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
							   .Select(m => m.Url)
							   .FirstOrDefault(),
						  Attributes = v.ProductAttributes
							   .Select(pa => new ProductAttributeResponse
							   {
								   AttributeId = pa.AttributeId,
								   ValueId = pa.ValueId,
								   Attribute = pa.Attribute.Name,
								   Value = pa.Value.Value
							   })
							   .ToList()
					  },
					  BestPromotion = v.PromotionItems
						   .Where(pi =>
							   !pi.IsDeleted &&
							   pi.CampaignId == campaignId &&
							   pi.IsActive &&
							   !pi.Campaign.IsDeleted &&
							   pi.Campaign.Status == CampaignStatus.Active &&
							   pi.Campaign.StartDate <= now &&
							   pi.Campaign.EndDate >= now &&
							   (!pi.MaxUsage.HasValue || pi.CurrentUsage < pi.MaxUsage.Value))
						   .Select(pi => new
						   {
							   CalculatedDiscountAmount = pi.DiscountType == DiscountType.Percentage
								   ? (v.BasePrice * pi.DiscountValue / 100m)
								   : pi.DiscountValue
						   })
						   .OrderByDescending(x => x.CalculatedDiscountAmount)
						   .FirstOrDefault()
				  })
				   .AsNoTracking()
				   .ToListAsync();

			var items = rawItems
				.Select(x =>
				{
					var item = x.Variant;

					if (x.BestPromotion == null)
						return item;

					var safeDiscount = Math.Min(x.BestPromotion.CalculatedDiscountAmount, item.BasePrice);
					var discountedPrice = Math.Round(item.BasePrice - safeDiscount, 2, MidpointRounding.AwayFromZero);

					return item with { BasePrice = discountedPrice };
				})
				.ToList();

			return (items, totalCount);
		}

		public async Task<List<Guid>> GetExistingIdsAsync(List<Guid> ids)
		=> await _context.ProductVariants
		 .Where(v => !v.IsDeleted && ids.Contains(v.Id))
			.Select(v => v.Id)
			.ToListAsync();
	}
}

