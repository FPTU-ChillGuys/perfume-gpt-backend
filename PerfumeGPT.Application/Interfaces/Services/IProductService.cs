using PerfumeGPT.Application.DTOs.Requests.Products;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.Media;
using PerfumeGPT.Application.DTOs.Responses.Products;

namespace PerfumeGPT.Application.Interfaces.Services
{
	public interface IProductService
	{
		Task<BaseResponse<PagedResult<ProductListItem>>> GetProductsAsync(GetPagedProductRequest request);
		Task<BaseResponse<ProductResponse>> GetProductAsync(Guid productId);
		Task<BaseResponse<string>> CreateProductAsync(CreateProductRequest request);
		Task<BaseResponse<string>> UpdateProductAsync(Guid productId, UpdateProductRequest request);
		Task<BaseResponse<string>> DeleteProductAsync(Guid productId);

		// Get product images (for viewing/editing)
		Task<BaseResponse<List<MediaResponse>>> GetProductImagesAsync(Guid productId);

		// Semantic search
		Task<BaseResponse> UpdateAllProductsEmbeddingAsync();
		Task<BaseResponse> UpdateProductEmbeddingAsync(Guid productId);
		Task<BaseResponse<PagedResult<ProductListItem>>> GetSemanticSearchProductAsync(string searchText, GetPagedProductRequest request);
	}
}

