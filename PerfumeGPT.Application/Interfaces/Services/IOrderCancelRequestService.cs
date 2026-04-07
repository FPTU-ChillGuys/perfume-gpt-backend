using PerfumeGPT.Application.DTOs.Requests.OrderCancelRequests;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.OrderCancelRequests;

namespace PerfumeGPT.Application.Interfaces.Services
{
	public interface IOrderCancelRequestService
	{
		Task<BaseResponse<PagedResult<OrderCancelRequestResponse>>> GetPagedRequestsAsync(GetPagedCancelRequestsRequest request);
        Task<BaseResponse<PagedResult<OrderCancelRequestResponse>>> GetPagedUserRequestsAsync(Guid userId, GetPagedCancelRequestsRequest request);
		Task<BaseResponse<OrderCancelRequestResponse>> GetRequestByIdAsync(Guid requestId, Guid requesterId, bool isPrivilegedUser);
		Task<BaseResponse<string>> ProcessRequestAsync(Guid requestId, Guid processedBy, string userRole, ProcessCancelRequest request);
	}
}
