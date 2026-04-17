using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Application.DTOs.Requests.Shippings;
using PerfumeGPT.Application.DTOs.Responses.Shippings;
using PerfumeGPT.Domain.Entities;

namespace PerfumeGPT.Application.Interfaces.Repositories
{
	public interface IShippingInfoRepository : IGenericRepository<ShippingInfo>
	{
		Task<(List<ShippingInfoListItem> Items, int TotalCount)> GetPagedByUserIdAsync(Guid userId, GetPagedShippingsRequest request);
		Task<ShippingInfo?> GetByOrderIdAsync(Guid orderId);
		Task<List<ShippingInfo>> GetSyncCandidatesForGhnAsync();
		Task<List<ShippingInfo>> GetSyncCandidatesForGhnByUserIdAsync(Guid userId);
	}
}
