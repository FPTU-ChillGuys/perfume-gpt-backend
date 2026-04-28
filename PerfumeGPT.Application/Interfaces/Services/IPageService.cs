using PerfumeGPT.Application.DTOs.Requests.Pages;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.Pages;

namespace PerfumeGPT.Application.Interfaces.Services
{
	public interface IPageService
	{
		Task<BaseResponse<PagedResult<PageResponse>>> GetPagesAsync(GetPagedPageRequest request);
		Task<BaseResponse<PageResponse>> GetPageContentAsync(string slug);
		Task<BaseResponse<PageResponse>> CreatePageAsync(CreatePageRequest request);
		Task<BaseResponse<PageResponse>> UpdatePageAsync(string slug, UpdatePageRequest request);
		Task<BaseResponse> DeletePageAsync(string slug);
		Task<BaseResponse<string>> PublishPageAsync(string slug);
	}
}
