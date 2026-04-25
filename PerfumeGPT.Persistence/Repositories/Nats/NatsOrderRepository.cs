using Microsoft.EntityFrameworkCore;
using PerfumeGPT.Application.DTOs.Responses.Nats;
using PerfumeGPT.Application.Interfaces.Repositories.Nats;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Domain.Enums;
using PerfumeGPT.Persistence.Contexts;
using PerfumeGPT.Persistence.Repositories.Commons;

namespace PerfumeGPT.Persistence.Repositories.Nats;

/// <summary>
/// NATS-specific repository implementation for Order operations
/// Returns NATS-optimized DTOs that match AI backend expectations
/// </summary>
public sealed class NatsOrderRepository : GenericRepository<Order>, INatsOrderRepository
{
	public NatsOrderRepository(PerfumeDbContext context) : base(context) { }

	public async Task<(List<NatsOrderListItemResponse> Items, int TotalCount)> GetPagedOrdersForNatsAsync(
		int pageNumber,
		int pageSize,
		Guid? userId = null,
		string? status = null,
		string? paymentStatus = null,
		int? shippingStatus = null,
		string? sortBy = null,
		bool isDescending = false)
	{
		var query = _context.Orders.AsQueryable();

		if (userId.HasValue)
		{
			query = query.Where(o => o.CustomerId == userId.Value);
		}

		if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<OrderStatus>(status, true, out var orderStatus))
		{
			query = query.Where(o => o.Status == orderStatus);
		}

		if (!string.IsNullOrWhiteSpace(paymentStatus) && Enum.TryParse<PaymentStatus>(paymentStatus, true, out var payStatus))
		{
			query = query.Where(o => o.PaymentStatus == payStatus);
		}

		if (shippingStatus.HasValue)
		{
			query = query.Where(o => o.ForwardShipping != null && o.ForwardShipping.Status == (ShippingStatus)shippingStatus.Value);
		}

		var totalCount = await query.CountAsync();

		// Apply sorting
		query = string.IsNullOrWhiteSpace(sortBy) switch
		{
			false when sortBy.Equals("createdAt", StringComparison.OrdinalIgnoreCase) =>
				isDescending ? query.OrderByDescending(o => o.CreatedAt) : query.OrderBy(o => o.CreatedAt),
			false when sortBy.Equals("totalAmount", StringComparison.OrdinalIgnoreCase) =>
				isDescending ? query.OrderByDescending(o => o.TotalAmount) : query.OrderBy(o => o.TotalAmount),
			_ => query.OrderByDescending(o => o.CreatedAt)
		};

		var items = await query
			.Skip((pageNumber - 1) * pageSize)
			.Take(pageSize)
			.Select(o => new NatsOrderListItemResponse
			{
				CreatedAt = o.CreatedAt.ToString("O"),
				CustomerId = o.CustomerId.ToString(),
				CustomerName = o.Customer != null ? o.Customer.FullName : null,
				Id = o.Id.ToString(),
				Code = o.Code,
				ItemCount = o.OrderDetails.Count,
				PaymentStatus = o.PaymentStatus.ToString(),
				ShippingStatus = o.ForwardShipping != null ? (int?)o.ForwardShipping.Status : null,
				StaffId = o.StaffId.ToString(),
				StaffName = o.Staff != null ? o.Staff.FullName : null,
				Status = o.Status.ToString(),
				TotalAmount = o.TotalAmount,
				Type = o.Type.ToString(),
				UpdatedAt = o.UpdatedAt.HasValue ? o.UpdatedAt.Value.ToString("O") : null
			})
			.AsNoTracking()
			.ToListAsync();

		return (items, totalCount);
	}

	public async Task<NatsOrderListItemResponse?> GetOrderByIdForNatsAsync(Guid orderId)
	{
		return await _context.Orders
			.Where(o => o.Id == orderId)
			.Select(o => new NatsOrderListItemResponse
			{
				CreatedAt = o.CreatedAt.ToString("O"),
				CustomerId = o.CustomerId.ToString(),
				CustomerName = o.Customer != null ? o.Customer.FullName : null,
				Id = o.Id.ToString(),
				Code = o.Code,
				ItemCount = o.OrderDetails.Count,
				PaymentStatus = o.PaymentStatus.ToString(),
				ShippingStatus = o.ForwardShipping != null ? (int?)o.ForwardShipping.Status : null,
				StaffId = o.StaffId.ToString(),
				StaffName = o.Staff != null ? o.Staff.FullName : null,
				Status = o.Status.ToString(),
				TotalAmount = o.TotalAmount,
				Type = o.Type.ToString(),
				UpdatedAt = o.UpdatedAt.HasValue ? o.UpdatedAt.Value.ToString("O") : null
			})
			.AsNoTracking()
			.FirstOrDefaultAsync();
	}
}
