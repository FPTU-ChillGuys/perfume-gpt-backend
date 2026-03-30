using Microsoft.EntityFrameworkCore;
using PerfumeGPT.Application.DTOs.Requests.OrderReturnRequests;
using PerfumeGPT.Application.DTOs.Responses.Media;
using PerfumeGPT.Application.DTOs.Responses.OrderReturnRequests;
using PerfumeGPT.Application.Interfaces.Repositories;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Persistence.Contexts;
using PerfumeGPT.Persistence.Repositories.Commons;

namespace PerfumeGPT.Persistence.Repositories
{
	public class OrderReturnRequestRepository : GenericRepository<OrderReturnRequest>, IOrderReturnRequestRepository
	{
		public OrderReturnRequestRepository(PerfumeDbContext context) : base(context) { }

		public async Task<(List<OrderReturnRequestResponse> Items, int TotalCount)> GetPagedResponsesAsync(GetPagedReturnRequestsRequest request)
		{
			var query = _context.OrderReturnRequests
				.Include(r => r.Customer)
				.AsNoTracking()
				.AsQueryable();

			if (request.Status.HasValue)
				query = query.Where(r => r.Status == request.Status.Value);

			if (request.CustomerId.HasValue)
				query = query.Where(r => r.CustomerId == request.CustomerId.Value);

			if (request.IsRefunded.HasValue)
				query = query.Where(r => r.IsRefunded == request.IsRefunded.Value);

			var totalCount = await query.CountAsync();

			query = request.IsDescending
				? query.OrderByDescending(r => r.CreatedAt)
				: query.OrderBy(r => r.CreatedAt);

			var pagedData = await query
				.Skip((request.PageNumber - 1) * request.PageSize)
				.Take(request.PageSize)
				.Select(r => new OrderReturnRequestResponse
				{
					Id = r.Id,
					OrderId = r.OrderId,
					CustomerId = r.CustomerId,
					CustomerEmail = r.Customer != null ? r.Customer.Email : null,
					ProcessedById = r.ProcessedById,
					InspectedById = r.InspectedById,
					Reason = r.Reason,
					CustomerNote = r.CustomerNote,
					StaffNote = r.StaffNote,
					InspectionNote = r.InspectionNote,
					Status = r.Status,
					RequestedRefundAmount = r.RequestedRefundAmount,
					ApprovedRefundAmount = r.ApprovedRefundAmount,
					IsRefunded = r.IsRefunded,
					VnpTransactionNo = r.VnpTransactionNo,
					IsRestocked = r.IsRestocked,
					CreatedAt = r.CreatedAt,
					UpdatedAt = r.UpdatedAt
				})
				.ToListAsync();

			return (pagedData, totalCount);
		}

		public async Task<OrderReturnRequestResponse?> GetResponseByIdAsync(Guid requestId)
			=> await _context.OrderReturnRequests
				.AsNoTracking()
				.Where(r => r.Id == requestId)
				.Select(r => new OrderReturnRequestResponse
				{
					Id = r.Id,
					OrderId = r.OrderId,
					CustomerId = r.CustomerId,
					CustomerEmail = r.Customer != null ? r.Customer.Email : null,
					ProcessedById = r.ProcessedById,
					InspectedById = r.InspectedById,
					Reason = r.Reason,
					CustomerNote = r.CustomerNote,
					StaffNote = r.StaffNote,
					InspectionNote = r.InspectionNote,
					Status = r.Status,
					RequestedRefundAmount = r.RequestedRefundAmount,
					ApprovedRefundAmount = r.ApprovedRefundAmount,
					IsRefunded = r.IsRefunded,
					VnpTransactionNo = r.VnpTransactionNo,
					IsRestocked = r.IsRestocked,
					ReturnDetails = r.ReturnDetails
						.Select(d => new OrderReturnRequestDetailResponse
						{
							Id = d.Id,
							OrderDetailId = d.OrderDetailId,
							ReturnedQuantity = d.ReturnedQuantity,
							IsRestocked = d.IsRestocked,
							InspectionNote = d.InspectionNote
						})
						.ToList(),
					ProofImages = r.ProofImages
						.OrderBy(m => m.DisplayOrder)
						.Select(m => new MediaResponse
						{
							Id = m.Id,
							Url = m.Url,
							AltText = m.AltText,
							DisplayOrder = m.DisplayOrder,
							IsPrimary = m.IsPrimary,
							FileSize = m.FileSize,
							MimeType = m.MimeType
						})
						.ToList(),
					CreatedAt = r.CreatedAt,
					UpdatedAt = r.UpdatedAt
				})
				.FirstOrDefaultAsync();

		public async Task<OrderReturnRequest?> GetByIdWithOrderAsync(Guid requestId)
			=> await _context.OrderReturnRequests
				.Include(r => r.Order)
				.Include(r => r.ProofImages)
				.Include(r => r.ReturnDetails)
					.ThenInclude(d => d.OrderDetail)
				.AsSplitQuery()
				.FirstOrDefaultAsync(r => r.Id == requestId);

		public async Task<OrderReturnRequest?> GetByIdWithOrderDetailsAsync(Guid requestId)
			=> await _context.OrderReturnRequests
				.Include(r => r.Order)
					.ThenInclude(o => o.OrderDetails)
				.Include(r => r.ProofImages)
				.Include(r => r.ReturnDetails)
					.ThenInclude(d => d.OrderDetail)
				.AsSplitQuery()
				.FirstOrDefaultAsync(r => r.Id == requestId);
	}
}
