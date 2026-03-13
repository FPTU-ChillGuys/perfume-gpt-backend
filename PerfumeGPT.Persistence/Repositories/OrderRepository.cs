using Mapster;
using Microsoft.EntityFrameworkCore;
using PerfumeGPT.Application.DTOs.Requests.Orders;
using PerfumeGPT.Application.DTOs.Responses.Orders;
using PerfumeGPT.Application.Interfaces.Repositories;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Persistence.Contexts;
using PerfumeGPT.Persistence.Extensions;
using PerfumeGPT.Persistence.Repositories.Commons;

namespace PerfumeGPT.Persistence.Repositories
{
	public class OrderRepository : GenericRepository<Order>, IOrderRepository
	{
		public OrderRepository(PerfumeDbContext context) : base(context)
		{
		}

		public async Task<(List<OrderListItem> Orders, int TotalCount)> GetPagedOrdersAsync(
			GetPagedOrdersRequest request,
			Guid? userId = null,
			Guid? staffId = null)
		{
			IQueryable<Order> query = _context.Orders.AsQueryable();

			if (userId.HasValue)
			{
				query = query.Where(o => o.CustomerId == userId.Value);
			}

			if (staffId.HasValue)
			{
				query = query.Where(o => o.StaffId == staffId.Value);
			}

			if (request.Status.HasValue)
			{
				query = query.Where(o => o.Status == request.Status.Value);
			}

			if (request.Type.HasValue)
			{
				query = query.Where(o => o.Type == request.Type.Value);
			}

			if (request.PaymentStatus.HasValue)
			{
				query = query.Where(o => o.PaymentStatus == request.PaymentStatus.Value);
			}

			if (request.FromDate.HasValue)
			{
				query = query.Where(o => o.CreatedAt >= request.FromDate.Value);
			}

			if (request.ToDate.HasValue)
			{
				query = query.Where(o => o.CreatedAt <= request.ToDate.Value);
			}

			if (!string.IsNullOrWhiteSpace(request.SearchTerm))
			{
				var searchTerm = request.SearchTerm.Trim();

				var orderIdFilter = EfCollationExtensions.CollateContains<Order>(
					o => o.Id.ToString(),
					searchTerm);

				var customerNameFilter = EfCollationExtensions.CollateContains<Order>(
					o => o.Customer != null ? o.Customer.FullName : null,
					searchTerm);

				query = query.Where(orderIdFilter.OrElse(customerNameFilter));
			}

			var totalCount = await query.CountAsync();

			var orders = await query
				.ApplySorting(request.SortBy ?? "CreatedAt", request.IsDescending)
				.Skip((request.PageNumber - 1) * request.PageSize)
				.Take(request.PageSize)
				.ProjectToType<OrderListItem>()
				.ToListAsync();

			return (orders, totalCount);
		}

		public async Task<OrderResponse?> GetOrderWithFullDetailsAsync(Guid orderId)
		{
			return await _context.Orders
				.Where(o => o.Id == orderId)
				.ProjectToType<OrderResponse>()
				.FirstOrDefaultAsync();
		}

		public async Task<UserOrderResponse?> GetUserOrderWithFullDetailsAsync(Guid orderId, Guid userId)
		{
			return await _context.Orders
				.Where(o => o.Id == orderId && o.CustomerId == userId)
				.ProjectToType<UserOrderResponse>()
				.FirstOrDefaultAsync();
		}

		public async Task<Order?> GetOrderForStatusUpdateAsync(Guid orderId)
		{
			return await _context.Orders
				.Include(o => o.ShippingInfo)
				.Include(o => o.OrderDetails)
				.FirstOrDefaultAsync(o => o.Id == orderId);
		}

		public async Task<Order?> GetOrderForCancellationAsync(Guid orderId)
		{
			return await _context.Orders
				.Include(o => o.ShippingInfo)
				.FirstOrDefaultAsync(o => o.Id == orderId);
		}

		public async Task<Order?> GetOrderForMarkUsedVoucherAsync(Guid orderId)
		{
			return await _context.Orders
				.Include(o => o.UserVoucher)
				.FirstOrDefaultAsync(o => o.Id == orderId);
		}
	}
}
