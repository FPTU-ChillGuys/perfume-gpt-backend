using Mapster;
using Microsoft.Data.SqlTypes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel;
using PerfumeGPT.Application.DTOs.Requests.Products;
using PerfumeGPT.Application.DTOs.Responses.Products;
using PerfumeGPT.Application.Interfaces.Repositories;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Persistence.Contexts;
using PerfumeGPT.Persistence.Repositories.Commons;

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

		#region Vector Search Methods

		public async Task<(List<Product> Items, int TotalCount)> GetPagedProductsWithSemanticSearch(string searchText, GetPagedProductRequest request)
		{
			IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator = kernel.GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>();

			if (String.IsNullOrEmpty(searchText))
			{
				return (new List<Product>(), 0);
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
				.ToListAsync();

			return (items, totalCount);
		}

		public async Task AddAllProductEmbeddingsAsync()
		{
			IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator = kernel.GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>();

			var products = await _context.Products
				.Include(p => p.Brand)
				.Include(p => p.Category)
				.Where(p => !p.IsDeleted)
				.ToListAsync();

			foreach (var product in products)
			{
				// Combine all relevant text fields for embedding
				var textToEmbed = $"Name:{product?.Name} Description:{product?.Description} Cateogory:{product?.Category?.Name} Brand:{product?.Brand?.Name}";

				var embedding = await embeddingGenerator.GenerateVectorAsync(textToEmbed ?? string.Empty);

				product?.Embedding = new SqlVector<float>(embedding);

				_context.Update(product!);

			}
			await _context.SaveChangesAsync();
		}

		public async Task AddProductEmbeddingsAsync(Guid productId)
		{
			IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator = kernel.GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>();
			var product = await _context.Products
				.Include(p => p.Brand)
				.Include(p => p.Category)
				.FirstOrDefaultAsync(p => p.Id == productId && !p.IsDeleted);
			if (product != null)
			{
				// Combine all relevant text fields for embedding
				var textToEmbed = $"Name:{product?.Name} Description:{product?.Description} Cateogory:{product?.Category?.Name} Brand:{product?.Brand?.Name}";
				var embedding = await embeddingGenerator.GenerateVectorAsync(textToEmbed ?? string.Empty);
				product!.Embedding = new SqlVector<float>(embedding);
				_context.Update(product);
				await _context.SaveChangesAsync();
			}
		}

		#endregion Vector Search Methods
	}
}

