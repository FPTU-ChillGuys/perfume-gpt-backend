using PerfumeGPT.Application.DTOs.Requests.Products;
using PerfumeGPT.Application.DTOs.Responses.Products;
using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Domain.Entities;

namespace PerfumeGPT.Application.Interfaces.Repositories
{
	public interface IProductRepository : IGenericRepository<Product>
	{
		Task<List<ProductLookupItem>> GetProductLookupListAsync();
		Task<ProductResponse?> GetProductResponseAsync(Guid productId);
		Task<(List<ProductListItem> Items, int TotalCount)> GetPagedProductListItemsAsync(GetPagedProductRequest request);
		Task<Product?> GetProductByIdWithAttributesAsync(Guid productId);


		/// <summary>
		///	Get paged products based on semantic search of the provided text.
		///	</summary>
		Task<(List<ProductListItem> Items, int TotalCount)> GetPagedProductsWithSemanticSearch(string searchText, GetPagedProductRequest request);

		/// <summary>
		///	Add embeddings for all products in the database.
		/// </summary>
		Task AddAllProductEmbeddingsAsync();

		/// <summary>
		/// Add embedding for a specific product by its ID.
		/// </summary>
		Task AddProductEmbeddingsByIdAsync(Guid productId);

        /// <summary>
        /// Add embedding for a specific product by passing the entire product entity. This can be useful when you already have the product data loaded and want to avoid an additional database query.
        /// </summary>
        Task<Product> AddProductEmbeddingsByProductAsync(Product product);

    }
}

