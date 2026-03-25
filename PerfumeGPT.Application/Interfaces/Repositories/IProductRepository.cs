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
		Task<bool> HasActiveVariantsAsync(Guid productId);
		Task<(List<ProductListItem> Items, int TotalCount)> GetBestSellerProductsAsync(GetPagedProductRequest request);
		Task<(List<ProductListItem> Items, int TotalCount)> GetNewArrivalProductsAsync(GetPagedProductRequest request);
		Task<(List<ProductListItem> Items, int TotalCount)> GetCampaignProductsAsync(Guid campaignId, GetPagedProductRequest request);
		Task<ProductInforResponse?> GetProductInfoAsync(Guid productId);
		Task<ProductFastLookResponse?> GetProductFastLookAsync(Guid productId);

		/// <summary>
		///	Get paged products based on full-text search via Elasticsearch.
		///	</summary>
		Task<(List<ProductListItemWithVariants> Items, int TotalCount)> GetPagedProductsWithSemanticSearch(string searchText, GetPagedProductRequest request);

		/// <summary>
		/// Index all products into Elasticsearch.
		/// </summary>
		Task IndexAllProductsToElasticsearchAsync();

		/// <summary>
		/// Index a specific product into Elasticsearch by its ID.
		/// </summary>
		Task IndexProductToElasticsearchAsync(Guid productId);

		Task<List<ProductDailySaleFigureResponse>> GetProductDailySaleFiguresAsync(DateOnly date);
	}
}

