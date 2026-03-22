using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.ProductAttributes.Values;
using PerfumeGPT.Application.DTOs.Requests.ProductAttributes;

namespace PerfumeGPT.Application.Interfaces.Services
{
	public interface IAttributeValueService
	{
		Task<BaseResponse<List<AttributeValueLookupItem>>> GetLookupListByAttributeIdAsync(int attributeId);
		Task<BaseResponse<string>> CreateAttributeValueAsync(int attributeId, CreateAttributeValueRequest request);
		Task<BaseResponse<string>> UpdateAttributeValueAsync(int valueId, UpdateAttributeValueRequest request);
		Task<BaseResponse<string>> DeleteAttributeValueAsync(int valueId);
	}
}
