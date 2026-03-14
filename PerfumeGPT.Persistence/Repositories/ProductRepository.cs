using Mapster;
using Microsoft.Data.SqlTypes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel;
using PerfumeGPT.Application.DTOs.Requests.Products;
using PerfumeGPT.Application.DTOs.Responses.Media;
using PerfumeGPT.Application.DTOs.Responses.ProductAttributes;
using PerfumeGPT.Application.DTOs.Responses.Products;
using PerfumeGPT.Application.DTOs.Responses.Variants;
using PerfumeGPT.Application.Interfaces.Repositories;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Persistence.Contexts;
using PerfumeGPT.Persistence.Repositories.Commons;
using System.Text;

namespace PerfumeGPT.Persistence.Repositories
{
	public class ProductRepository : GenericRepository<Product>, IProductRepository
	{
		private readonly Kernel kernel;

		public ProductRepository(PerfumeDbContext context, Kernel kernel) : base(context)
		{
			this.kernel = kernel;
		}

		public async Task<Product?> GetProductByIdWithAttributesAsync(Guid productId)
		{
			return await _context.Products.Include(p => p.ProductAttributes)
				.FirstOrDefaultAsync(p => p.Id == productId && !p.IsDeleted);
		}

		public async Task<List<ProductLookupItem>> GetProductLookupListAsync()
		{
			return await _context.Products
				.Where(p => !p.IsDeleted)
				.ProjectToType<ProductLookupItem>()
				.AsNoTracking()
				.ToListAsync();
		}

		public async Task<ProductResponse?> GetProductResponseAsync(Guid productId)
		{
			return await _context.Products
				.Where(p => p.Id == productId && !p.IsDeleted)
				.ProjectToType<ProductResponse>()
				.AsNoTracking()
				.FirstOrDefaultAsync();
		}

		public async Task<(List<ProductListItem> Items, int TotalCount)> GetPagedProductListItemsAsync(GetPagedProductRequest request)
		{
			var query = _context.Products
				.Where(p => !p.IsDeleted);

			if (request.GenderValueId.HasValue)
			{
				query = query.Where(p => p.ProductAttributes.Any(pa =>
					pa.Attribute != null &&
					pa.Attribute.InternalCode == "GENDER" &&
					pa.ValueId == request.GenderValueId.Value
				));
			}

			if (request.IsAvailable.HasValue)
			{
				if (request.IsAvailable.Value)
				{
					query = query.Where(p => p.Variants.Any(v => !v.IsDeleted && (v.Stock.TotalQuantity - v.Stock.ReservedQuantity) > 0));
				}
			}

			var totalCount = await query.CountAsync();

			var items = await query
				.OrderByDescending(p => p.CreatedAt)
				.Skip((request.PageNumber - 1) * request.PageSize)
				.Take(request.PageSize)
				.ProjectToType<ProductListItem>()
				.AsNoTracking()
				.ToListAsync();

			return (items, totalCount);
		}

		public async Task<(List<ProductListItem> Items, int TotalCount)> GetBestSellerProductsAsync(GetPagedProductRequest request)
		{
			// Best sellers: rank by total order count across all variants in the last 30 days (using Order.CreatedAt)
			var limitDate = DateTime.UtcNow.AddDays(-30);
			var query = _context.Products
				.Where(p => !p.IsDeleted)
				.Select(p => new
				{
					Product = p,
					OrderCount = p.Variants
						.Where(v => !v.IsDeleted && p.Variants.Any(v => !v.IsDeleted && (v.Stock.TotalQuantity - v.Stock.ReservedQuantity) > 0))
						.SelectMany(v => v.OrderDetails)
						.Count(od => od.Order != null && od.Order.CreatedAt >= limitDate)
				})
				.OrderByDescending(x => x.OrderCount)
				.Select(x => x.Product);

			var totalCount = await query.CountAsync();
			var items = await query
				.Skip((request.PageNumber - 1) * request.PageSize)
				.Take(request.PageSize)
				.ProjectToType<ProductListItem>()
				.AsNoTracking()
				.ToListAsync();
			return (items, totalCount);
		}

		public async Task<(List<ProductListItem> Items, int TotalCount)> GetNewArrivalProductsAsync(GetPagedProductRequest request)
		{
			// New arrivals by most recent CreatedAt
			var query = _context.Products
				.Where(p => !p.IsDeleted && p.Variants.Any(v => !v.IsDeleted && (v.Stock.TotalQuantity - v.Stock.ReservedQuantity) > 0))
				.OrderByDescending(p => p.CreatedAt);
			var totalCount = await query.CountAsync();
			var items = await query
				.Skip((request.PageNumber - 1) * request.PageSize)
				.Take(request.PageSize)
				.ProjectToType<ProductListItem>()
				.AsNoTracking()
				.ToListAsync();
			return (items, totalCount);
		}

		public async Task<ProductInforResponse?> GetProductInfoAsync(Guid productId)
		{
			return await _context.Products
				.Where(p => p.Id == productId)
				.ProjectToType<ProductInforResponse>()
				.AsNoTracking()
				.FirstOrDefaultAsync();
		}

		public async Task<ProductFastLookResponse?> GetProductFastLookAsync(Guid productId)
		{
			var result = await _context.Products
				.Where(p => p.Id == productId && !p.IsDeleted)
				.Select(p => new ProductFastLookResponse
				{
					Id = p.Id,
					Name = p.Name,
					Description = p.Description,
					BrandName = p.Brand.Name,

					// Calculate rating and review count
					Rating = (int)Math.Round(
						p.Variants
							.Where(v => !v.IsDeleted)
							.SelectMany(v => v.OrderDetails)
							.Where(od => od.Review != null)
							.Average(od => (double?)od.Review!.Rating) ?? 0
					),

					ReviewCount = p.Variants
						.Where(v => !v.IsDeleted)
						.SelectMany(v => v.OrderDetails)
						.Count(od => od.Review != null),

					// Map variants with their own media
					Variants = p.Variants
						.Where(v => !v.IsDeleted)
						.Select(v => new VariantFastLookResponse
						{
							Id = v.Id,
							DisplayName = $"{v.Concentration.Name} - {v.VolumeMl}ml",
							Price = v.BasePrice,
							StockQuantity = v.Stock.TotalQuantity - v.Stock.ReservedQuantity,
							Media = v.Media
								.Where(m => m.IsPrimary)
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
						.ToList(),

					// Map product-level attributes
					Attribute = p.ProductAttributes
						.Where(pa => pa.Attribute != null && pa.Attribute.InternalCode == "GENDER")
						.Select(pa => new ProductAttributeResponse
						{
							Id = pa.Id,
							AttributeId = pa.AttributeId,
							Attribute = pa.Attribute.Name,
							ValueId = pa.ValueId,
							Value = pa.Value.Value,
							Description = pa.Attribute.Description
						})
						.FirstOrDefault(),
				})
				.AsNoTracking()
				.FirstOrDefaultAsync();
			return result;
		}

		#region Vector Search Methods

		public async Task<(List<ProductListItemWithVariants> Items, int TotalCount)> GetPagedProductsWithSemanticSearch(string searchText, GetPagedProductRequest request)
		{
			IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator = kernel.GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>();

			if (String.IsNullOrEmpty(searchText))
			{
				return (new List<ProductListItemWithVariants>(), 0);
			}

			var searchEmbedding = await embeddingGenerator.GenerateVectorAsync(searchText);

			var sqlVector = new SqlVector<float>(searchEmbedding);

			var productQuery = _context.Products
				.Where(p => !p.IsDeleted);


			var threshold = 0.5;
			var metricType = "cosine";
			var topSimilarProductsQuery = productQuery
				.Include(p => p.Brand)
				.Include(p => p.Category)
				.Include(p => p.Media)
				.Include(p => p.ProductAttributes)
				.Include(p => p.Variants.Where(v => !v.IsDeleted))
					.ThenInclude(v => v.Concentration)
				//.Where(p => EF.Functions.VectorDistance(metricType, p.Embedding!.Value, sqlVector) <= threshold)
				.OrderBy(p => EF.Functions.VectorDistance(metricType, p.Embedding!.Value, sqlVector))
				.AsNoTracking();

			// Get distance from the most similar product to use for total count calculation
			var productDistance = await productQuery
				.Select(p => new
				{
					ProductName = p.Name,
					Distance = EF.Functions.VectorDistance(metricType, p.Embedding!.Value, sqlVector)
				})
				.ToListAsync();

			foreach (var item in productDistance)
			{
				Console.WriteLine($"{item.ProductName}: {item.Distance} compare to threshold: {threshold}");
			}

			var totalCount = await _context.Products.CountAsync();

			var items = await topSimilarProductsQuery
				.Skip((request.PageNumber - 1) * request.PageSize)
				.Take(request.PageSize)
				.ProjectToType<ProductListItemWithVariants>()
				.ToListAsync();

			return (items, totalCount);
		}

		public async Task AddAllProductEmbeddingsAsync()
		{
			IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator = kernel.GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>();

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
			IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator = kernel.GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>();
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
			IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator = kernel.GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>();
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

