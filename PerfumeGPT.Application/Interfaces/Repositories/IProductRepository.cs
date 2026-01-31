using PerfumeGPT.Application.DTOs.Requests.Products;
using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Domain.Entities;

namespace PerfumeGPT.Application.Interfaces.Repositories
{
	public interface IProductRepository : IGenericRepository<Product>
	{
		/// <summary>
		/// Gets a product with all related data (Brand, Category, Family, Variants with Concentrations).
		/// </summary>
		Task<Product?> GetProductWithDetailsAsync(Guid productId);

		/// <summary>
		/// Gets paged products with related data (Brand, Category, Family).
		/// </summary>
		Task<(List<Product> Items, int TotalCount)> GetPagedProductsWithDetailsAsync(GetPagedProductRequest request);

        /// <summary>
        ///	Get paged products based on semantic search of the provided text.
        ///	</summary>
        Task<(List<Product> Items, int TotalCount)> GetPagedProductsWithSemanticSearch(string searchText, GetPagedProductRequest request);

        /// <summary>
        ///	Add embeddings for all products in the database.
        /// </summary>
        Task AddAllProductEmbeddingsAsync();

        /// <summary>
		/// Add embedding for a specific product by its ID.
		/// </summary>
        Task AddProductEmbeddingsAsync(Guid productId);
    }
}

