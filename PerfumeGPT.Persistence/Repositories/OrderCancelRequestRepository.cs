using Microsoft.EntityFrameworkCore;
using PerfumeGPT.Application.DTOs.Requests.OrderCancelRequests;
using PerfumeGPT.Application.DTOs.Responses.OrderCancelRequests;
using PerfumeGPT.Application.Interfaces.Repositories;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Persistence.Contexts;
using PerfumeGPT.Persistence.Repositories.Commons;

namespace PerfumeGPT.Persistence.Repositories
{
	public class OrderCancelRequestRepository : GenericRepository<OrderCancelRequest>, IOrderCancelRequestRepository
	{
		public OrderCancelRequestRepository(PerfumeDbContext context) : base(context) { }

		public async Task<(List<OrderCancelRequestResponse> Items, int TotalCount)> GetPagedResponsesAsync(GetPagedCancelRequestsRequest request)
		{
			var query = _context.OrderCancelRequests
				.Include(r => r.RequestedBy)
				.AsNoTracking()
				.AsQueryable();

			if (request.Status.HasValue)
				query = query.Where(r => r.Status == request.Status.Value);

			if (request.IsRefundRequired.HasValue)
				query = query.Where(r => r.IsRefundRequired == request.IsRefundRequired.Value);

			var totalCount = await query.CountAsync();

			var pagedData = await query
				.OrderByDescending(r => r.CreatedAt)
				.Skip((request.PageNumber - 1) * request.PageSize)
				.Take(request.PageSize)
				.Select(r => new OrderCancelRequestResponse
				{
					Id = r.Id,
					OrderId = r.OrderId,
					RequestedById = r.RequestedById,
					RequestedByEmail = r.RequestedBy != null ? r.RequestedBy.Email : null,
					ProcessedById = r.ProcessedById,
					Reason = r.Reason.ToString(),
					StaffNote = r.StaffNote,
					Status = r.Status,
					IsRefundRequired = r.IsRefundRequired,
					RefundAmount = r.RefundAmount,
					IsRefunded = r.IsRefunded,
					VnpTransactionNo = r.VnpTransactionNo,
					CreatedAt = r.CreatedAt,
					UpdatedAt = r.UpdatedAt
				})
				.ToListAsync();

			return (pagedData, totalCount);
		}
	}
}