using PerfumeGPT.Application.DTOs.Requests.Banners;
using PerfumeGPT.Application.DTOs.Responses.Banners;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.Interfaces.Services
{
	public interface IBannerService
	{
		Task<BaseResponse<List<BannerResponse>>> GetVisibleBannersAsync(BannerPosition? position = null);
		Task<BaseResponse<PagedResult<BannerResponse>>> GetPagedBannersAsync(GetPagedBannersRequest request);
		Task<BaseResponse<BannerResponse>> GetBannerByIdAsync(Guid bannerId);
		Task<BaseResponse<string>> CreateBannerAsync(CreateBannerRequest request);
		Task<BaseResponse<string>> UpdateBannerAsync(Guid bannerId, UpdateBannerRequest request);
		Task<BaseResponse<string>> DeleteBannerAsync(Guid bannerId);
	}
}
