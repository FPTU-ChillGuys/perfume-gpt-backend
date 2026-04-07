using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Application.DTOs.Requests.OrderCancelRequests;
using PerfumeGPT.Application.DTOs.Responses.OrderCancelRequests;
using PerfumeGPT.Domain.Entities;

namespace PerfumeGPT.Application.Interfaces.Repositories
{
	public interface IOrderCancelRequestRepository : IGenericRepository<OrderCancelRequest>
	{
		Task<(List<OrderCancelRequestResponse> Items, int TotalCount)> GetPagedResponsesAsync(GetPagedCancelRequestsRequest request);
		Task<(List<OrderCancelRequestResponse> Items, int TotalCount)> GetPagedUserResponsesAsync(Guid userId, GetPagedCancelRequestsRequest request);
		Task<OrderCancelRequestResponse?> GetResponseByIdAsync(Guid requestId);
	}
}