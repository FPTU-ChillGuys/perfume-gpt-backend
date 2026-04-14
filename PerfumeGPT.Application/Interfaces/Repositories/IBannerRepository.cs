using PerfumeGPT.Application.DTOs.Requests.Banners;
using PerfumeGPT.Application.DTOs.Responses.Banners;
using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.Interfaces.Repositories
{
	public interface IBannerRepository : IGenericRepository<Banner>
	{
		Task<List<BannerResponse>> GetVisibleBannersAsync(BannerPosition? position = null);
		Task<(List<BannerResponse> Items, int TotalCount)> GetPagedBannersAsync(GetPagedBannersRequest request);
		Task<BannerResponse?> GetBannerByIdDtoAsync(Guid bannerId);
	}
}
