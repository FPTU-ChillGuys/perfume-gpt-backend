using PerfumeGPT.Application.DTOs.Requests.SourcingCatalogs;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.SourcingCatalogs;

namespace PerfumeGPT.Application.Interfaces.Services
{
	public interface ISourcingCatalogService
	{
		Task<BaseResponse<IEnumerable<CatalogItemResponse>>> GetCatalogsAsync(int? supplierId, Guid? variantId);
		Task<BaseResponse<string>> CreateItemAsync(CreateCatalogItemRequest request);
		Task<BaseResponse<string>> UpdateItemAsync(Guid id, UpdateCatalogItemRequest request);
		Task<BaseResponse<string>> SetAsPrimaryAsync(Guid id);
		Task<BaseResponse<string>> DeleteItemAsync(Guid id);
	}
}
