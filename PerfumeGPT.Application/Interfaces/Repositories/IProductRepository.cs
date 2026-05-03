using PerfumeGPT.Application.DTOs.Commons;
using PerfumeGPT.Application.DTOs.Requests.Products;
using PerfumeGPT.Application.DTOs.Responses.Products;
using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Domain.Entities;

namespace PerfumeGPT.Application.Interfaces.Repositories
{
	public interface IProductRepository : IGenericRepository<Product>
	{
		Task<List<ProductLookupItem>> GetProductLookupListAsync();
		Task<PublicProductResponse?> GetPublicProductResponseAsync(Guid productId, SellableStockQueryContext sellable);
		Task<ProductResponse?> GetProductResponseAsync(Guid productId, SellableStockQueryContext sellable);
		Task<(List<ProductListItem> Items, int TotalCount)> GetPagedProductListItemsAsync(GetPagedProductRequest request, SellableStockQueryContext sellable);
		Task<Product?> GetProductByIdWithAttributesAsync(Guid productId);
		Task<Product?> GetProductAggregateForUpdateAsync(Guid productId);
		Task<bool> HasActiveVariantsAsync(Guid productId);
		Task<(List<ProductListItem> Items, int TotalCount)> GetBestSellerProductsAsync(GetPagedProductRequest request, SellableStockQueryContext sellable);
		Task<(List<ProductListItem> Items, int TotalCount)> GetNewArrivalProductsAsync(GetPagedProductRequest request, SellableStockQueryContext sellable);
		Task<(List<ProductListItem> Items, int TotalCount)> GetCampaignProductsAsync(Guid campaignId, GetPagedProductRequest request, SellableStockQueryContext sellable);
		Task<ProductInforResponse?> GetProductInfoAsync(Guid productId);
		Task<ProductFastLookResponse?> GetProductFastLookAsync(Guid productId, SellableStockQueryContext sellable);


		Task<List<ProductDailySaleFigureResponse>> GetProductDailySaleFiguresAsync(DateOnly date);
	}
}

