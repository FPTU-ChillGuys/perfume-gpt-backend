using PerfumeGPT.Application.DTOs.Requests.Variants;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.Media;
using PerfumeGPT.Application.DTOs.Responses.Variants;
using PerfumeGPT.Domain.Entities;

namespace PerfumeGPT.Application.Interfaces.Services
{
	public interface IVariantService
	{
	Task<BaseResponse<PagedResult<VariantPagedItem>>> GetPagedVariantsAsync(GetPagedVariantsRequest request);
	Task<BaseResponse<ProductVariantResponse>> GetVariantByIdAsync(Guid variantId);
	Task<BaseResponse<ProductVariantResponse>> GetVariantByBarcodeAsync(string barcode);
	Task<BaseResponse<List<VariantLookupItem>>> GetVariantLookupListAsync(Guid? productId = null);
	Task<BaseResponse<BulkActionResult<string>>> CreateVariantAsync(CreateVariantRequest request);
	Task<BaseResponse<BulkActionResult<string>>> UpdateVariantAsync(Guid variantId, UpdateVariantRequest request);
	Task<BaseResponse<string>> DeleteVariantAsync(Guid variantId);
	Task<BaseResponse<List<MediaResponse>>> GetVariantImagesAsync(Guid variantId);

		(bool IsValid, string? ErrorMessage) ValidateVariantForCart(ProductVariant variant);
	}
}
