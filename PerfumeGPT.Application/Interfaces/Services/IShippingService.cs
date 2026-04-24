using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Requests.Shippings;
using PerfumeGPT.Application.DTOs.Responses.Shippings;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.Interfaces.Services
{
	public interface IShippingService
	{
		Task<BaseResponse<PagedResult<ShippingInfoListItem>>> GetPagedShippingInfosByUserIdAsync(Guid userId, GetPagedShippingsRequest request);
		Task<BaseResponse<string>> SyncShippingStatusByUserIdAsync(Guid userId);
		Task<BaseResponse<string>> SyncShippingStatusByWebhookAsync(string orderCode, string ghnStatus);
		Task<bool> SyncSingleShippingInfoAsync(ShippingInfo shippingInfo);
		ShippingStatus? MapGhnStatusToDomainStatus(string ghnStatus);
		bool TryApplyShippingStatus(ShippingInfo shippingInfo, ShippingStatus targetStatus);
	}
}
