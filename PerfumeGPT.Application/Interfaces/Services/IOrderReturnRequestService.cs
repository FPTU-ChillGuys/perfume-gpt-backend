using PerfumeGPT.Application.DTOs.Requests.OrderReturnRequests;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.OrderReturnRequests;

namespace PerfumeGPT.Application.Interfaces.Services
{
	public interface IOrderReturnRequestService
	{
		Task<BaseResponse<PagedResult<OrderReturnRequestResponse>>> GetPagedReturnRequestsAsync(GetPagedReturnRequestsRequest request);
		Task<BaseResponse<PagedResult<OrderReturnRequestResponse>>> GetPagedUserReturnRequestsAsync(Guid userId, GetPagedUserReturnRequestsRequest request);
		Task<BaseResponse<OrderReturnRequestResponse>> GetReturnRequestByIdAsync(Guid requestId, Guid requesterId, bool isPrivilegedUser);
		Task<BaseResponse<string>> CreateReturnRequestAsync(Guid customerId, CreateReturnRequestDto request);
		Task<BaseResponse<string>> ProcessInitialRequestAsync(Guid processedById, Guid requestId, ProcessInitialReturnDto request);
		Task<BaseResponse<string>> StartInspectionAsync(Guid inspectedById, Guid requestId, StartInspectionDto request);
		Task<BaseResponse<string>> RecordInspectionResultAsync(Guid inspectedById, Guid requestId, RecordInspectionDto request);
		Task<BaseResponse<string>> RejectAfterInspectionAsync(Guid inspectedById, Guid requestId, RejectInspectionDto request);
		Task<BaseResponse<string>> ProcessRefundAsync(Guid financeAdminId, Guid requestId, ProcessRefundRequest request);
	}
}
