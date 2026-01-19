using PerfumeGPT.Application.DTOs.Requests.Base;
using PerfumeGPT.Application.DTOs.Requests.Variants;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.Variants;

namespace PerfumeGPT.Application.Interfaces.Services
{
	public interface IVariantService
	{
		Task<BaseResponse<PagedResult<VariantPagedItem>>> GetPagedVariantsAsync(GetPagedVariantsRequest request);
		Task<BaseResponse<ProductVariantResponse>> GetVariantByIdAsync(Guid variantId);
		Task<BaseResponse<string>> CreateVariantAsync(CreateVariantRequest request, FileUpload? imageFile);
		Task<BaseResponse<string>> UpdateVariantAsync(Guid variantId, UpdateVariantRequest request, FileUpload? imageFile);
		Task<BaseResponse<string>> DeleteVariantAsync(Guid variantId);
	}
}
