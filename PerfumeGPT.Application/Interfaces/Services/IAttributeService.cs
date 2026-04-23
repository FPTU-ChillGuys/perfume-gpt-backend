using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.ProductAttributes.Attributes;
using PerfumeGPT.Application.DTOs.Requests.ProductAttributes;

namespace PerfumeGPT.Application.Interfaces.Services
{
	public interface IAttributeService
	{
		Task<BaseResponse<List<AttributeLookupItem>>> GetLookupListAsync(bool isVariantLevel);
		Task<BaseResponse<string>> CreateAttributeAsync(CreateAttributeRequest request);
		Task<BaseResponse<string>> UpdateAttributeAsync(int attributeId, UpdateAttributeRequest request);
		Task<BaseResponse<string>> DeleteAttributeAsync(int attributeId);
	}
}
