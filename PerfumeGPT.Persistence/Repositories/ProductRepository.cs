using Microsoft.Data.SqlTypes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Embeddings;
using PerfumeGPT.Application.DTOs.Requests.Products;
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

        public async Task<Product?> GetProductWithDetailsAsync(Guid productId)
        {
            return await _context.Products
                .Include(p => p.Brand)
                .Include(p => p.Category)
                .Include(p => p.FragranceFamily)
                .Include(p => p.Variants)
                    .ThenInclude(v => v.Concentration)
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == productId && !p.IsDeleted);
        }

        public async Task<(List<Product> Items, int TotalCount)> GetPagedProductsWithDetailsAsync(GetPagedProductRequest request)
        {
            var query = _context.Products
                .Include(p => p.Brand)
                .Include(p => p.Category)
                .Include(p => p.FragranceFamily)
                .Where(p => !p.IsDeleted)
                .AsNoTracking();

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(p => p.CreatedAt)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<(List<Product> Items, int TotalCount)> GetPagedProductsWithSemanticSearch(string searchText, GetPagedProductRequest request)
        {
            IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator = kernel.GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>();

            var searchEmbedding = await embeddingGenerator.GenerateVectorAsync(searchText);

            var sqlVector = new SqlVector<float>(searchEmbedding);

            var topSimilarProductsQuery = _context.Products.OrderBy(p => EF.Functions.VectorDistance("cosine", p.Embedding!.Value, sqlVector)).AsNoTracking();

            var totalCount = await _context.Products.CountAsync();

            var items = await topSimilarProductsQuery
                .OrderByDescending(p => p.CreatedAt)
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
                .Include(p => p.FragranceFamily)
                .Where(p => !p.IsDeleted)
                .ToListAsync();

            foreach (var product in products)
            {
                // Combine all relevant text fields for embedding
                var textToEmbed = $"Name:{product?.Name} Description:{product?.Description} FragranceFamily:{product?.FragranceFamily?.Name} Cateogory:{product?.Category?.Name} Brand:{product?.Brand?.Name}";

                var embedding = await embeddingGenerator.GenerateVectorAsync(textToEmbed ?? string.Empty);

                product?.Embedding = new SqlVector<float>(embedding);

                _context.Update(product!);

            }
            await _context.SaveChangesAsync();
        }

        public async Task AddProductEmbeddingsAsync(Guid productId) {             
            IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator = kernel.GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>();
            var product = await _context.Products
                .Include(p => p.Brand)
                .Include(p => p.Category)
                .Include(p => p.FragranceFamily)
                .FirstOrDefaultAsync(p => p.Id == productId && !p.IsDeleted);
            if (product != null)
            {
                // Combine all relevant text fields for embedding
                var textToEmbed = $"Name:{product?.Name} Description:{product?.Description} FragranceFamily:{product?.FragranceFamily?.Name} Cateogory:{product?.Category?.Name} Brand:{product?.Brand?.Name}";
                var embedding = await embeddingGenerator.GenerateVectorAsync(textToEmbed ?? string.Empty);
                product!.Embedding = new SqlVector<float>(embedding);
                _context.Update(product);
                await _context.SaveChangesAsync();
            }
        }
    }
}

