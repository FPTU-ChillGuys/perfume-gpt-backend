using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Application.DTOs.Requests.OrderReturnRequests;
using PerfumeGPT.Application.DTOs.Responses.OrderReturnRequests;
using PerfumeGPT.Domain.Entities;

namespace PerfumeGPT.Application.Interfaces.Repositories
{
	public interface IOrderReturnRequestRepository : IGenericRepository<OrderReturnRequest>
	{
		Task<(List<OrderReturnRequestResponse> Items, int TotalCount)> GetPagedResponsesAsync(GetPagedReturnRequestsRequest request);
		Task<(List<OrderReturnRequestResponse> Items, int TotalCount)> GetPagedUserResponsesAsync(Guid userId, GetPagedUserReturnRequestsRequest request);
		Task<OrderReturnRequestResponse?> GetResponseByIdAsync(Guid requestId);
		Task<OrderReturnRequest?> GetByIdWithOrderAsync(Guid requestId);
		Task<OrderReturnRequest?> GetByIdWithOrderDetailsAsync(Guid requestId);
	}
}
