using Microsoft.EntityFrameworkCore;
using Microsoft.SemanticKernel;
using PerfumeGPT.Application.DTOs.Requests.Products;
using PerfumeGPT.Application.DTOs.Responses.Media;
using PerfumeGPT.Application.DTOs.Responses.ProductAttributes;
using PerfumeGPT.Application.DTOs.Responses.Products;
using PerfumeGPT.Application.DTOs.Responses.Variants;
using PerfumeGPT.Application.Interfaces.Repositories;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Domain.Enums;
using PerfumeGPT.Persistence.Contexts;
using PerfumeGPT.Persistence.Repositories.Commons;

namespace PerfumeGPT.Persistence.Repositories
{
	public class ProductRepository : GenericRepository<Product>, IProductRepository
	{
		private readonly Kernel _kernel;

		public ProductRepository(PerfumeDbContext context, Kernel kernel) : base(context)
		{
			_kernel = kernel;
		}

		public async Task<Product?> GetProductByIdWithAttributesAsync(Guid productId)
			=> await _context.Products
			  .Where(p => !p.IsDeleted)
				.Include(p => p.ProductAttributes)
				.FirstOrDefaultAsync(p => p.Id == productId);

		public async Task<Product?> GetProductAggregateForUpdateAsync(Guid productId)
		=> await _context.Products
			.Where(p => !p.IsDeleted && p.Id == productId)
			.Include(p => p.ProductFamilyMaps)
			.Include(p => p.ProductScentMaps)
			.Include(p => p.ProductAttributes)
			.AsSplitQuery()
			.FirstOrDefaultAsync();

		public async Task<bool> HasActiveVariantsAsync(Guid productId)
			=> await _context.Products
			 .AnyAsync(p => !p.IsDeleted && p.Id == productId && p.Variants.Any(v => !v.IsDeleted));

		public async Task<List<ProductLookupItem>> GetProductLookupListAsync()
			=> await _context.Products
			  .Where(p => !p.IsDeleted)
				.Select(p => new ProductLookupItem
				{
					Id = p.Id,
					Name = p.Name,
					BrandName = p.Brand.Name,
					PrimaryImageUrl = p.Media
						.Where(m => m.IsPrimary && !m.IsDeleted)
						.Select(m => m.Url)
						.FirstOrDefault()
				})
				.AsNoTracking()
				.ToListAsync();

		public async Task<ProductResponse?> GetProductResponseAsync(Guid productId)
		{
			var now = DateTime.UtcNow;

			var raw = await _context.Products
				.AsNoTracking()
			  .Where(p => !p.IsDeleted && p.Id == productId)
				.Select(p => new
				{
					p.Id,
					p.Name,
					p.Gender,
					p.Origin,
					p.ReleaseYear,
					p.BrandId,
					BrandName = p.Brand.Name,
					p.CategoryId,
					CategoryName = p.Category.Name,
					p.Description,
					NumberOfVariants = p.Variants.Count(v => !v.IsDeleted),
					Media = p.Media
						.Where(m => !m.IsDeleted)
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
					Attributes = p.ProductAttributes
						.Select(pa => new ProductAttributeResponse
						{
							Id = pa.Id,
							AttributeId = pa.AttributeId,
							ValueId = pa.ValueId,
							Attribute = pa.Attribute.Name,
							Description = pa.Attribute.Description ?? string.Empty,
							Value = pa.Value.Value
						})
						.ToList(),
					Variants = p.Variants
						.Where(v => !v.IsDeleted)
						.Select(v => new
						{
							Variant = new ProductVariantResponse
							{
								Id = v.Id,
								ProductId = v.ProductId,
								ProductName = p.Name,
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
								StockQuantity = v.Stock != null
									? v.Stock.TotalQuantity - v.Stock.ReservedQuantity
									: 0,
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
										Id = pa.Id,
										AttributeId = pa.AttributeId,
										ValueId = pa.ValueId,
										Attribute = pa.Attribute.Name,
										Description = pa.Attribute.Description ?? string.Empty,
										Value = pa.Value.Value
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
									(!pi.MaxUsage.HasValue || pi.CurrentUsage < pi.MaxUsage.Value))
								.Select(pi => new
								{
									CampaignName = pi.Campaign.Name,
									DiscountType = pi.DiscountType,
									DiscountValue = pi.DiscountValue,
									CalculatedDiscountAmount = pi.DiscountType == DiscountType.Percentage
										? (v.BasePrice * pi.DiscountValue / 100m)
										: pi.DiscountValue
								})
								.OrderByDescending(x => x.CalculatedDiscountAmount)
								.FirstOrDefault(),
							AvailableVouchers = v.PromotionItems
								.Where(pi =>
									!pi.IsDeleted &&
									pi.IsActive &&
									!pi.Campaign.IsDeleted &&
									pi.Campaign.Status == CampaignStatus.Active &&
									pi.Campaign.StartDate <= now &&
									pi.Campaign.EndDate >= now)
								.OrderByDescending(pi => pi.CreatedAt)
								.SelectMany(pi => pi.Campaign.Vouchers)
								.Where(voucher =>
									!voucher.IsDeleted &&
									voucher.ExpiryDate >= now &&
									(voucher.RemainingQuantity == null || voucher.RemainingQuantity > 0))
								.Select(voucher => voucher.Code)
								.Distinct()
							   .ToList()
						})
						.ToList(),
					OlfactoryFamilies = p.ProductFamilyMaps
						.Select(pfm => new ProductOlfactoryFamilyResponse
						{
							OlfactoryFamilyId = pfm.OlfactoryFamilyId,
							Name = pfm.OlfactoryFamily.Name
						})
						.ToList(),
					ScentNotes = p.ProductScentMaps
						.Select(psm => new ProductScentNoteResponse
						{
							NoteId = psm.ScentNoteId,
							Name = psm.ScentNote.Name,
							Type = psm.NoteType
						})
						.ToList()
				})
				.AsSplitQuery()
				.FirstOrDefaultAsync();

			if (raw == null)
				return null;

			var variants = raw.Variants
				.Select(x =>
				{
					var variant = x.Variant;

					if (x.BestPromotion != null)
					{
						var safeDiscount = Math.Min(x.BestPromotion.CalculatedDiscountAmount, variant.BasePrice);
						var discountedPrice = Math.Round(variant.BasePrice - safeDiscount, 2, MidpointRounding.AwayFromZero);

						variant = variant with
						{
							DiscountedPrice = discountedPrice,
							CampaignName = x.BestPromotion.CampaignName,
						};
					}

					else
					{
						variant = variant with
						{
							DiscountedPrice = variant.BasePrice,
						};
					}

					return variant;
				})
				.ToList();

			return new ProductResponse
			{
				Id = raw.Id,
				Name = raw.Name,
				Gender = raw.Gender,
				Origin = raw.Origin,
				ReleaseYear = raw.ReleaseYear,
				BrandId = raw.BrandId,
				BrandName = raw.BrandName,
				CategoryId = raw.CategoryId,
				CategoryName = raw.CategoryName,
				Description = raw.Description,
				NumberOfVariants = raw.NumberOfVariants,
				Media = raw.Media,
				Variants = variants,
				Attributes = raw.Attributes,
				OlfactoryFamilies = raw.OlfactoryFamilies,
				ScentNotes = raw.ScentNotes
			};
		}

		public async Task<PublicProductResponse?> GetPublicProductResponseAsync(Guid productId)
		{
			var now = DateTime.UtcNow;

			var raw = await _context.Products
				.AsNoTracking()
				.Where(p => !p.IsDeleted && p.Id == productId)
				.Select(p => new
				{
					p.Id,
					p.Name,
					p.Gender,
					p.Origin,
					p.ReleaseYear,
					BrandName = p.Brand.Name,
					CategoryName = p.Category.Name,
					p.Description,
					Media = p.Media
						.Where(m => !m.IsDeleted)
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
					Variants = p.Variants
						.Where(v => !v.IsDeleted)
						.Select(v => new
						{
							Variant = new PublicProductVariantResponse
							{
								Id = v.Id,
								Sku = v.Sku,
								VolumeMl = v.VolumeMl,
								ConcentrationName = v.Concentration.Name,
								Type = v.Type,
								BasePrice = v.BasePrice,
								RetailPrice = v.RetailPrice,
								StockQuantity = v.Stock != null
									? v.Stock.TotalQuantity - v.Stock.ReservedQuantity
									: 0,
								ProductName = p.Name,
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
									(!pi.MaxUsage.HasValue || pi.CurrentUsage < pi.MaxUsage.Value))
								.Select(pi => new
								{
									CampaignName = pi.Campaign.Name,
									CalculatedDiscountAmount = pi.DiscountType == DiscountType.Percentage
										? (v.BasePrice * pi.DiscountValue / 100m)
										: pi.DiscountValue,

									Quota = pi.MaxUsage.HasValue
										? (pi.MaxUsage.Value - pi.CurrentUsage)
										: (int?)null
								})
								.OrderByDescending(x => x.CalculatedDiscountAmount)
								.FirstOrDefault(),
							AvailableVouchers = v.PromotionItems
								.Where(pi =>
									!pi.IsDeleted &&
									pi.IsActive &&
									!pi.Campaign.IsDeleted &&
									pi.Campaign.Status == CampaignStatus.Active &&
									pi.Campaign.StartDate <= now &&
									pi.Campaign.EndDate >= now)
								.OrderByDescending(pi => pi.CreatedAt)
								.SelectMany(pi => pi.Campaign.Vouchers)
								.Where(voucher =>
									!voucher.IsDeleted &&
									voucher.ExpiryDate >= now &&
									(voucher.RemainingQuantity == null || voucher.RemainingQuantity > 0))
								.Select(voucher => voucher.Code)
								.Distinct()
								.ToList()
						})
						.ToList()
				})
				.AsSplitQuery()
				.FirstOrDefaultAsync();

			if (raw == null)
				return null;

			var variants = raw.Variants
				.Select(x =>
				{
					var variant = x.Variant;

					if (x.BestPromotion != null)
					{
						var safeDiscount = Math.Min(x.BestPromotion.CalculatedDiscountAmount, variant.BasePrice);
						var discountedPrice = Math.Round(variant.BasePrice - safeDiscount, 2, MidpointRounding.AwayFromZero);

						variant = variant with
						{
							DiscountedPrice = discountedPrice,
							CampaignName = x.BestPromotion.CampaignName,

							// BỔ SUNG Ở ĐÂY: Map giá trị Quota từ BestPromotion
							CampaignQuota = x.BestPromotion.Quota,

							VoucherCode = x.AvailableVouchers.FirstOrDefault()
						};
					}
					else
					{
						variant = variant with
						{
							DiscountedPrice = variant.BasePrice,
							VoucherCode = x.AvailableVouchers.FirstOrDefault(),
							CampaignQuota = null
						};
					}

					return variant;
				})
				.ToList();

			return new PublicProductResponse
			{
				Id = raw.Id,
				Name = raw.Name,
				Gender = raw.Gender,
				Origin = raw.Origin,
				ReleaseYear = raw.ReleaseYear,
				BrandName = raw.BrandName,
				CategoryName = raw.CategoryName,
				Description = raw.Description,
				Media = raw.Media,
				Variants = variants
			};
		}

		public async Task<(List<ProductListItem> Items, int TotalCount)> GetPagedProductListItemsAsync(GetPagedProductRequest request)
		{
			var now = DateTime.UtcNow;

			var query = _context.Products
				.Where(p => !p.IsDeleted)
				.AsQueryable();

			if (request.Gender.HasValue)
				query = query.Where(p => p.Gender == request.Gender.Value);

			if (request.Volume.HasValue)
				query = query.Where(p =>
					p.Variants.Any(v => !v.IsDeleted && v.VolumeMl == request.Volume.Value));

			if (request.CategoryId.HasValue)
				query = query.Where(p => p.CategoryId == request.CategoryId.Value);

			if (request.BrandId.HasValue)
				query = query.Where(p => p.BrandId == request.BrandId.Value);

			if (request.FromPrice.HasValue)
				query = query.Where(p =>
					p.Variants.Any(v => !v.IsDeleted && v.BasePrice >= request.FromPrice.Value));

			if (request.ToPrice.HasValue)
				query = query.Where(p =>
					p.Variants.Any(v => !v.IsDeleted && v.BasePrice <= request.ToPrice.Value));

			if (request.IsAvailable == true)
				query = query.Where(p =>
					p.Variants.Any(v =>
						!v.IsDeleted &&
						v.Stock.TotalQuantity - v.Stock.ReservedQuantity > 0));

			var totalCount = await query.CountAsync();

			var itemsWithTags = await query
				   .OrderByDescending(p => p.CreatedAt)
				   .Skip((request.PageNumber - 1) * request.PageSize)
				   .Take(request.PageSize)
				   .Select(p => new
				   {
					   Item = new ProductListItem
					   {
						   Id = p.Id,
						   Name = p.Name,
						   BrandId = p.BrandId,
						   BrandName = p.Brand.Name,
						   CategoryId = p.CategoryId,
						   CategoryName = p.Category.Name,
						   Description = p.Description,
						   NumberOfVariants = p.Variants.Count(v => !v.IsDeleted),
						   VariantPrices = p.Variants
							   .Where(v => !v.IsDeleted)
							   .Select(v => v.BasePrice)
							   .ToList(),
						   PrimaryImage = p.Media
							   .Where(m => m.IsPrimary && !m.IsDeleted)
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
							   .FirstOrDefault()
					   },
					   HasSaleTag = p.Variants.Any(v =>
						   !v.IsDeleted &&
						   v.PromotionItems.Any(pi =>
							   !pi.IsDeleted &&
							   !pi.Campaign.IsDeleted &&
							   pi.Campaign.Status == CampaignStatus.Active &&
							   pi.Campaign.StartDate <= now &&
							   pi.Campaign.EndDate >= now &&
							   pi.IsActive)),
					   HasNewTag = p.Variants.Any(v =>
						   !v.IsDeleted &&
						   v.PromotionItems.Any(pi =>
							   !pi.IsDeleted &&
							   !pi.Campaign.IsDeleted &&
							   pi.ItemType == PromotionType.NewArrival &&
							   pi.Campaign.Status == CampaignStatus.Active &&
							   pi.Campaign.StartDate <= now &&
							   pi.Campaign.EndDate >= now &&
							   pi.IsActive))
				   })
				   .AsSplitQuery()
				   .AsNoTracking()
				   .ToListAsync();

			var items = itemsWithTags
				.Select(x =>
				{
					var tags = new List<string>();
					if (x.HasSaleTag) tags.Add("sale");
					if (x.HasNewTag) tags.Add("new");

					return x.Item with
					{
						Tags = tags.Count != 0 ? tags : null
					};
				})
				.ToList();

			return (items, totalCount);
		}

		public async Task<(List<ProductListItem> Items, int TotalCount)> GetBestSellerProductsAsync(GetPagedProductRequest request)
		{
			var now = DateTime.UtcNow;

			var limitDate = DateTime.UtcNow.AddDays(-30);

			var query = _context.Products
			 .Where(p => !p.IsDeleted && p.Variants.Any(v =>
					!v.IsDeleted &&
					v.Stock.TotalQuantity - v.Stock.ReservedQuantity > 0))
				.Select(p => new
				{
					Product = p,
					OrderCount = p.Variants
						.Where(v => !v.IsDeleted)
						.SelectMany(v => v.OrderDetails)
						.Count(od => od.Order != null && od.Order.CreatedAt >= limitDate)
				})
				.OrderByDescending(x => x.OrderCount)
				.Select(x => x.Product);

			var totalCount = await query.CountAsync();

			var itemsWithTags = await query
				   .Skip((request.PageNumber - 1) * request.PageSize)
				   .Take(request.PageSize)
				   .Select(p => new
				   {
					   Item = new ProductListItem
					   {
						   Id = p.Id,
						   Name = p.Name,
						   BrandId = p.BrandId,
						   BrandName = p.Brand.Name,
						   CategoryId = p.CategoryId,
						   CategoryName = p.Category.Name,
						   Description = p.Description,
						   NumberOfVariants = p.Variants.Count(v => !v.IsDeleted),
						   VariantPrices = p.Variants
							   .Where(v => !v.IsDeleted)
							   .Select(v => v.BasePrice)
							   .ToList(),
						   PrimaryImage = p.Media
							   .Where(m => m.IsPrimary && !m.IsDeleted)
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
							   .FirstOrDefault()
					   },
					   HasSaleTag = p.Variants.Any(v =>
						   !v.IsDeleted &&
						   v.PromotionItems.Any(pi =>
							   !pi.IsDeleted &&
							   !pi.Campaign.IsDeleted &&
							   pi.Campaign.Status == CampaignStatus.Active &&
							   pi.Campaign.StartDate <= now &&
							   pi.Campaign.EndDate >= now &&
							   pi.IsActive)),
					   HasNewTag = p.Variants.Any(v =>
						   !v.IsDeleted &&
						   v.PromotionItems.Any(pi =>
							   !pi.IsDeleted &&
							   !pi.Campaign.IsDeleted &&
							   pi.ItemType == PromotionType.NewArrival &&
							   pi.Campaign.Status == CampaignStatus.Active &&
							   pi.Campaign.StartDate <= now &&
							   pi.Campaign.EndDate >= now &&
							   pi.IsActive))
				   })
				   .AsSplitQuery()
				   .AsNoTracking()
				   .ToListAsync();

			var items = itemsWithTags
				.Select(x =>
				{
					var tags = new List<string>();
					if (x.HasSaleTag) tags.Add("sale");
					if (x.HasNewTag) tags.Add("new");

					return x.Item with
					{
						Tags = tags.Count != 0 ? tags : null
					};
				})
				.ToList();

			return (items, totalCount);
		}

		public async Task<(List<ProductListItem> Items, int TotalCount)> GetNewArrivalProductsAsync(GetPagedProductRequest request)
		{
			var now = DateTime.UtcNow;

			var query = _context.Products
			 .Where(p => !p.IsDeleted && p.Variants.Any(v =>
					!v.IsDeleted &&
					v.Stock.TotalQuantity - v.Stock.ReservedQuantity > 0))
				.OrderByDescending(p => p.CreatedAt);

			var totalCount = await query.CountAsync();

			var itemsWithTags = await query
				   .Skip((request.PageNumber - 1) * request.PageSize)
				   .Take(request.PageSize)
				   .Select(p => new
				   {
					   Item = new ProductListItem
					   {
						   Id = p.Id,
						   Name = p.Name,
						   BrandId = p.BrandId,
						   BrandName = p.Brand.Name,
						   CategoryId = p.CategoryId,
						   CategoryName = p.Category.Name,
						   Description = p.Description,
						   NumberOfVariants = p.Variants.Count(v => !v.IsDeleted),
						   VariantPrices = p.Variants
							   .Where(v => !v.IsDeleted)
							   .Select(v => v.BasePrice)
							   .ToList(),
						   PrimaryImage = p.Media
							   .Where(m => m.IsPrimary && !m.IsDeleted)
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
							   .FirstOrDefault()
					   },
					   HasSaleTag = p.Variants.Any(v =>
						   !v.IsDeleted &&
						   v.PromotionItems.Any(pi =>
							   !pi.IsDeleted &&
							   !pi.Campaign.IsDeleted &&
							   pi.Campaign.Status == CampaignStatus.Active &&
							   pi.Campaign.StartDate <= now &&
							   pi.Campaign.EndDate >= now &&
							   pi.IsActive))
				   })
				   .AsSplitQuery()
				   .AsNoTracking()
				   .ToListAsync();

			var items = itemsWithTags
				.Select(x =>
				{
					var finalTags = new List<string>
					{
						"new"
					};

					if (x.HasSaleTag)
						finalTags.Add("sale");

					return x.Item with { Tags = finalTags };
				})
				.ToList();

			return (items, totalCount);
		}

		public async Task<(List<ProductListItem> Items, int TotalCount)> GetCampaignProductsAsync(Guid campaignId, GetPagedProductRequest request)
		{
			var now = DateTime.UtcNow;

			var query = _context.Products
			 .Where(p => !p.IsDeleted && p.Variants.Any(v =>
					!v.IsDeleted &&
					v.PromotionItems.Any(pi =>
						!pi.IsDeleted &&
						pi.IsActive &&
						pi.CampaignId == campaignId &&
						!pi.Campaign.IsDeleted &&
						pi.Campaign.Status == CampaignStatus.Active &&
						pi.Campaign.StartDate <= now &&
						pi.Campaign.EndDate >= now)));

			if (request.Gender.HasValue)
				query = query.Where(p => p.Gender == request.Gender.Value);

			if (request.Volume.HasValue)
				query = query.Where(p =>
					p.Variants.Any(v => !v.IsDeleted && v.VolumeMl == request.Volume.Value));

			if (request.CategoryId.HasValue)
				query = query.Where(p => p.CategoryId == request.CategoryId.Value);

			if (request.BrandId.HasValue)
				query = query.Where(p => p.BrandId == request.BrandId.Value);

			if (request.FromPrice.HasValue)
				query = query.Where(p =>
					p.Variants.Any(v => !v.IsDeleted && v.BasePrice >= request.FromPrice.Value));

			if (request.ToPrice.HasValue)
				query = query.Where(p =>
					p.Variants.Any(v => !v.IsDeleted && v.BasePrice <= request.ToPrice.Value));

			if (request.IsAvailable == true)
				query = query.Where(p =>
					p.Variants.Any(v =>
						!v.IsDeleted &&
						v.Stock.TotalQuantity - v.Stock.ReservedQuantity > 0));

			var totalCount = await query.CountAsync();

			var itemsWithTags = await query
				.OrderByDescending(p => p.CreatedAt)
				.Skip((request.PageNumber - 1) * request.PageSize)
				.Take(request.PageSize)
				.Select(p => new
				{
					Item = new ProductListItem
					{
						Id = p.Id,
						Name = p.Name,
						BrandId = p.BrandId,
						BrandName = p.Brand.Name,
						CategoryId = p.CategoryId,
						CategoryName = p.Category.Name,
						Description = p.Description,
						NumberOfVariants = p.Variants.Count(v => !v.IsDeleted),
						VariantPrices = p.Variants
							.Where(v => !v.IsDeleted)
							.Select(v => v.BasePrice)
							.ToList(),
						PrimaryImage = p.Media
							.Where(m => m.IsPrimary && !m.IsDeleted)
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
							.FirstOrDefault()
					},
					HasSaleTag = p.Variants.Any(v =>
						!v.IsDeleted &&
						v.PromotionItems.Any(pi =>
							!pi.IsDeleted &&
							pi.IsActive &&
							pi.CampaignId == campaignId &&
							!pi.Campaign.IsDeleted &&
							pi.Campaign.Status == CampaignStatus.Active &&
							pi.Campaign.StartDate <= now &&
							pi.Campaign.EndDate >= now)),
					HasNewTag = p.Variants.Any(v =>
						!v.IsDeleted &&
						v.PromotionItems.Any(pi =>
							!pi.IsDeleted &&
							pi.IsActive &&
							pi.CampaignId == campaignId &&
							!pi.Campaign.IsDeleted &&
							pi.ItemType == PromotionType.NewArrival &&
							pi.Campaign.Status == CampaignStatus.Active &&
							pi.Campaign.StartDate <= now &&
							pi.Campaign.EndDate >= now))
				})
				.AsSplitQuery()
				.AsNoTracking()
				.ToListAsync();

			var items = itemsWithTags
				.Select(x =>
				{
					var tags = new List<string>();
					if (x.HasSaleTag) tags.Add("sale");
					if (x.HasNewTag) tags.Add("new");

					return x.Item with
					{
						Tags = tags.Count != 0 ? tags : null
					};
				})
				.ToList();

			return (items, totalCount);
		}

		public async Task<ProductInforResponse?> GetProductInfoAsync(Guid productId)
			=> await _context.Products
			  .Where(p => !p.IsDeleted && p.Id == productId)
			  .Select(p => new ProductInforResponse
			  {
				  ProductCode = p.Id.ToString(),
				  BrandName = p.Brand.Name,
				  Origin = p.Origin,
				  ReleaseYear = p.ReleaseYear,
				  Gender = p.Gender,
				  ScentGroup = string.Join(", ", p.ProductFamilyMaps.Select(pfm => pfm.OlfactoryFamily.Name)),
				  Style = string.Join(", ", p.ProductAttributes
						.Where(pa => pa.Attribute.InternalCode == "STYLE")
						.Select(pa => pa.Value.Value)),
				  TopNotes = string.Join(", ", p.ProductScentMaps
						.Where(psm => psm.NoteType == NoteType.Top)
						.Select(psm => psm.ScentNote.Name)),
				  HeartNotes = string.Join(", ", p.ProductScentMaps
						.Where(psm => psm.NoteType == NoteType.Heart)
						.Select(psm => psm.ScentNote.Name)),
				  BaseNotes = string.Join(", ", p.ProductScentMaps
						.Where(psm => psm.NoteType == NoteType.Base)
						.Select(psm => psm.ScentNote.Name)),
				  Description = p.Description ?? string.Empty
			  })
				.AsSplitQuery()
				.AsNoTracking()
				.FirstOrDefaultAsync();

		public async Task<ProductFastLookResponse?> GetProductFastLookAsync(Guid productId)
			=> await _context.Products
			  .Where(p => !p.IsDeleted && p.Id == productId)
				.Select(p => new ProductFastLookResponse
				{
					Id = p.Id,
					Name = p.Name,
					Description = p.Description != null
						? p.Description.Substring(0, Math.Min(p.Description.Length, 100))
						  + (p.Description.Length > 100 ? "..." : "")
						: string.Empty,
					BrandName = p.Brand.Name,
					Gender = p.Gender,

					Rating = (int)Math.Round(
						p.Variants
							.Where(v => !v.IsDeleted)
							.SelectMany(v => v.OrderDetails)
							.Where(od => od.Review != null)
							.Select(od => (double?)od.Review!.Rating)
							.Average() ?? 0),
					ReviewCount = p.Variants
						.Where(v => !v.IsDeleted)
						.SelectMany(v => v.OrderDetails)
						.Count(od => od.Review != null),
					Variants = p.Variants
						.Where(v => !v.IsDeleted)
						.Select(v => new VariantFastLookResponse
						{
							Id = v.Id,
							Sku = v.Sku,
							DisplayName = $"{v.Concentration.Name} - {v.VolumeMl}ml",
							Price = v.BasePrice,
							RetailPrice = v.RetailPrice,
							StockQuantity = v.Stock.TotalQuantity - v.Stock.ReservedQuantity,
							Media = v.Media
								.Where(m => m.IsPrimary && !m.IsDeleted)
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
								.FirstOrDefault()
						})
						.ToList()
				})
				.AsSplitQuery()
				.AsNoTracking()
				.FirstOrDefaultAsync();

		public async Task<List<ProductDailySaleFigureResponse>> GetProductDailySaleFiguresAsync(DateOnly date)
		{
			var startDate = date.ToDateTime(TimeOnly.MinValue);
			var endDate = date.ToDateTime(TimeOnly.MaxValue);
			var dailyFigures = await _context.Products
				.Where(p => !p.IsDeleted)
				.Select(p => new ProductDailySaleFigureResponse
				{
					ProductId = p.Id,
					ProductName = p.Name,
					DailySaleFigures = p.Variants
						.Where(v => !v.IsDeleted)
						.Select(v => new VariantDailySaleFigure
						{
							VariantId = v.Id,
							VariantName = $"{v.Concentration.Name} - {v.VolumeMl}ml",
							QuantitySold = v.OrderDetails
								.Where(od => od.Order != null && od.Order.CreatedAt >= startDate && od.Order.CreatedAt <= endDate)
								.Sum(od => od.Quantity),
							Date = date
						})
						.ToList()
				})
				.AsNoTracking()
				.ToListAsync();

			return dailyFigures.Where(df => df.DailySaleFigures.Any(v => v.QuantitySold > 0)).ToList();
		}
	}
}

