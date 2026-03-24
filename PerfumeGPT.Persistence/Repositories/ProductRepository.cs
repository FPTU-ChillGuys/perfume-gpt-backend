using Mapster;
using Microsoft.Data.SqlTypes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel;
using PerfumeGPT.Application.DTOs.Requests.Products;
using PerfumeGPT.Application.DTOs.Responses.Media;
using PerfumeGPT.Application.DTOs.Responses.Products;
using PerfumeGPT.Application.DTOs.Responses.Variants;
using PerfumeGPT.Application.Interfaces.Repositories;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Domain.Enums;
using PerfumeGPT.Persistence.Contexts;
using PerfumeGPT.Persistence.Repositories.Commons;
using System.Text;

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
				.Include(p => p.ProductAttributes)
				.FirstOrDefaultAsync(p => p.Id == productId);

		public async Task<bool> HasActiveVariantsAsync(Guid productId)
			=> await _context.Products
				.AnyAsync(p => p.Id == productId && p.Variants.Any(v => !v.IsDeleted));

		public async Task<List<ProductLookupItem>> GetProductLookupListAsync()
			=> await _context.Products
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
			=> await _context.Products
				.Where(p => p.Id == productId)
				.ProjectToType<ProductResponse>()
				.AsSplitQuery()
				.AsNoTracking()
				.FirstOrDefaultAsync();

		public async Task<(List<ProductListItem> Items, int TotalCount)> GetPagedProductListItemsAsync(
		GetPagedProductRequest request)
		{
			var now = DateTime.UtcNow;

			var query = _context.Products.AsQueryable();

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
				   .AsNoTracking()
				   .ToListAsync();

			var items = itemsWithTags
				.Select(x =>
				{
					if (x.HasSaleTag)
						x.Item.Tags.Add("sale");

					if (x.HasNewTag)
						x.Item.Tags.Add("new");

					return x.Item;
				})
				.ToList();

			return (items, totalCount);
		}

		public async Task<(List<ProductListItem> Items, int TotalCount)> GetBestSellerProductsAsync(
		GetPagedProductRequest request)
		{
			var now = DateTime.UtcNow;

			var limitDate = DateTime.UtcNow.AddDays(-30);

			var query = _context.Products
				.Where(p => p.Variants.Any(v =>
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
				   .AsNoTracking()
				   .ToListAsync();

			var items = itemsWithTags
				.Select(x =>
				{
					if (x.HasSaleTag)
						x.Item.Tags.Add("sale");

					if (x.HasNewTag)
						x.Item.Tags.Add("new");

					return x.Item;
				})
				.ToList();

			return (items, totalCount);
		}


		public async Task<(List<ProductListItem> Items, int TotalCount)> GetNewArrivalProductsAsync(
		GetPagedProductRequest request)
		{
			var now = DateTime.UtcNow;

			var query = _context.Products
				.Where(p => p.Variants.Any(v =>
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
				   .AsNoTracking()
				   .ToListAsync();

			var items = itemsWithTags
				.Select(x =>
				{
					if (x.HasSaleTag)
						x.Item.Tags.Add("sale");

					x.Item.Tags.Add("new");

					return x.Item;
				})
				.ToList();

			return (items, totalCount);
		}

		public async Task<(List<ProductListItem> Items, int TotalCount)> GetCampaignProductsAsync(
		Guid campaignId,
		GetPagedProductRequest request)
		{
			var now = DateTime.UtcNow;

			var query = _context.Products
				.Where(p => p.Variants.Any(v =>
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
				.AsNoTracking()
				.ToListAsync();

			var items = itemsWithTags
				.Select(x =>
				{
					if (x.HasSaleTag)
						x.Item.Tags.Add("sale");

					if (x.HasNewTag)
						x.Item.Tags.Add("new");

					return x.Item;
				})
				.ToList();

			return (items, totalCount);
		}

		public async Task<ProductInforResponse?> GetProductInfoAsync(Guid productId)
			=> await _context.Products
				.Where(p => p.Id == productId)
				.ProjectToType<ProductInforResponse>()
				.AsNoTracking()
				.FirstOrDefaultAsync();

		public async Task<ProductFastLookResponse?> GetProductFastLookAsync(Guid productId)
			=> await _context.Products
				.Where(p => p.Id == productId)
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

		#region Vector Search Methods
		public async Task<(List<ProductListItemWithVariants> Items, int TotalCount)>
		GetPagedProductsWithSemanticSearch(string searchText, GetPagedProductRequest request)
		{
			if (string.IsNullOrEmpty(searchText))
				return ([], 0);

			var embeddingGenerator = _kernel
				.GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>();

			var searchEmbedding = await embeddingGenerator.GenerateVectorAsync(searchText);
			var sqlVector = new SqlVector<float>(searchEmbedding);
			const string metricType = "cosine";

			var query = _context.Products
				.OrderBy(p => EF.Functions.VectorDistance(metricType, p.Embedding!.Value, sqlVector))
				.AsNoTracking();

			var totalCount = await _context.Products.CountAsync();

			var items = await query
				.Skip((request.PageNumber - 1) * request.PageSize)
				.Take(request.PageSize)
				.ProjectToType<ProductListItemWithVariants>()
				.ToListAsync();

			return (items, totalCount);
		}

		public async Task AddAllProductEmbeddingsAsync()
		{
			IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator = _kernel.GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>();

			var products = await _context.Products
				.Include(p => p.Brand)
				.Include(p => p.Category)
				.Include(p => p.ProductAttributes)
					.ThenInclude(pa => pa.Attribute)
				.Include(p => p.ProductAttributes)
					.ThenInclude(pa => pa.Value)
				.Include(p => p.Variants.Where(v => !v.IsDeleted))
					.ThenInclude(v => v.Concentration)
				.Include(p => p.Variants.Where(v => !v.IsDeleted))
					.ThenInclude(v => v.ProductAttributes)
						.ThenInclude(pa => pa.Attribute)
				.Include(p => p.Variants.Where(v => !v.IsDeleted))
					.ThenInclude(v => v.ProductAttributes)
						.ThenInclude(pa => pa.Value)
				.Where(p => !p.IsDeleted)
				.ToListAsync();

			foreach (var product in products)
			{
				var textToEmbed = BuildEmbeddingText(product);

				var embedding = await embeddingGenerator.GenerateVectorAsync(textToEmbed);

				product.Embedding = new SqlVector<float>(embedding);

				_context.Update(product);

			}
			await _context.SaveChangesAsync();
		}

		public async Task AddProductEmbeddingsByIdAsync(Guid productId)
		{
			IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator = _kernel.GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>();
			var product = await _context.Products
				.Include(p => p.Brand)
				.Include(p => p.Category)
				.Include(p => p.ProductAttributes)
					.ThenInclude(pa => pa.Attribute)
				.Include(p => p.ProductAttributes)
					.ThenInclude(pa => pa.Value)
				.Include(p => p.Variants.Where(v => !v.IsDeleted))
					.ThenInclude(v => v.Concentration)
				.Include(p => p.Variants.Where(v => !v.IsDeleted))
					.ThenInclude(v => v.ProductAttributes)
						.ThenInclude(pa => pa.Attribute)
				.Include(p => p.Variants.Where(v => !v.IsDeleted))
					.ThenInclude(v => v.ProductAttributes)
						.ThenInclude(pa => pa.Value)
				.FirstOrDefaultAsync(p => p.Id == productId && !p.IsDeleted);
			if (product != null)
			{
				var textToEmbed = BuildEmbeddingText(product);
				var embedding = await embeddingGenerator.GenerateVectorAsync(textToEmbed);
				product.Embedding = new SqlVector<float>(embedding);
				_context.Update(product);
				await _context.SaveChangesAsync();
			}
		}

		public async Task<Product> AddProductEmbeddingsByProductAsync(Product product)
		{
			IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator = _kernel.GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>();
			if (product != null)
			{
				// Reload with all related data for comprehensive embedding
				var fullProduct = await _context.Products
					.Include(p => p.Brand)
					.Include(p => p.Category)
					.Include(p => p.ProductAttributes)
						.ThenInclude(pa => pa.Attribute)
					.Include(p => p.ProductAttributes)
						.ThenInclude(pa => pa.Value)
					.Include(p => p.Variants.Where(v => !v.IsDeleted))
						.ThenInclude(v => v.Concentration)
					.Include(p => p.Variants.Where(v => !v.IsDeleted))
						.ThenInclude(v => v.ProductAttributes)
							.ThenInclude(pa => pa.Attribute)
					.Include(p => p.Variants.Where(v => !v.IsDeleted))
						.ThenInclude(v => v.ProductAttributes)
							.ThenInclude(pa => pa.Value)
					.FirstOrDefaultAsync(p => p.Id == product.Id && !p.IsDeleted);

				if (fullProduct != null)
				{
					var textToEmbed = BuildEmbeddingText(fullProduct);
					var embedding = await embeddingGenerator.GenerateVectorAsync(textToEmbed);
					fullProduct.Embedding = new SqlVector<float>(embedding);
					_context.Update(fullProduct);
					await _context.SaveChangesAsync();
					return fullProduct;
				}
			}
			return product!;
		}

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

		#endregion Vector Search Methods

		#region Private Methods

		private static string BuildEmbeddingText(Product product)
		{
			var sb = new StringBuilder();

			sb.Append($"Name: {product.Name}.");
			sb.Append($" Brand: {product.Brand?.Name}.");
			sb.Append($" Category: {product.Category?.Name}.");

			if (!string.IsNullOrWhiteSpace(product.Description))
				sb.Append($" Description: {product.Description}.");

			// Product-level attributes (e.g., Top Notes: Bergamot, Scent Family: Floral)
			if (product.ProductAttributes?.Count > 0)
			{
				var grouped = product.ProductAttributes
					.Where(pa => pa.Attribute != null && pa.Value != null)
					.GroupBy(pa => pa.Attribute.Name);

				foreach (var group in grouped)
				{
					var values = string.Join(", ", group.Select(pa => pa.Value.Value));
					sb.Append($" {group.Key}: {values}.");
				}
			}

			// Variants (e.g., concentration, volume, type, price, variant-level attributes)
			if (product.Variants?.Count > 0)
			{
				var variantTexts = new List<string>();
				foreach (var variant in product.Variants)
				{
					var parts = new List<string>();

					if (variant.Concentration != null)
						parts.Add(variant.Concentration.Name);

					parts.Add($"{variant.VolumeMl}ml");
					parts.Add(variant.Type.ToString());
					parts.Add($"{variant.BasePrice:N0} VND");

					// Variant-level attributes
					if (variant.ProductAttributes?.Count > 0)
					{
						var variantAttrs = variant.ProductAttributes
							.Where(pa => pa.Attribute != null && pa.Value != null)
							.Select(pa => $"{pa.Attribute.Name}: {pa.Value.Value}");
						parts.AddRange(variantAttrs);
					}

					variantTexts.Add(string.Join(", ", parts));
				}

				sb.Append($" Variants: [{string.Join("; ", variantTexts)}].");
			}

			return sb.ToString();
		}

		#endregion Private Methods
	}
}

