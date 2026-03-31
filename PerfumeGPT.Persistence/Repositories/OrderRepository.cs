using Mapster;
using Microsoft.EntityFrameworkCore;
using PerfumeGPT.Application.DTOs.Requests.Orders;
using PerfumeGPT.Application.DTOs.Responses.Orders;
using PerfumeGPT.Application.Interfaces.Repositories;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Domain.Enums;
using PerfumeGPT.Persistence.Contexts;
using PerfumeGPT.Persistence.Extensions;
using PerfumeGPT.Persistence.Repositories.Commons;

namespace PerfumeGPT.Persistence.Repositories
{
	public class OrderRepository : GenericRepository<Order>, IOrderRepository
	{
		public OrderRepository(PerfumeDbContext context) : base(context) { }

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
				.AsSplitQuery()
				.ProjectToType<OrderListItem>()
				.ToListAsync();

			return (orders, totalCount);
		}

		public async Task<OrderResponse?> GetOrderWithFullDetailsAsync(Guid orderId)
			=> await _context.Orders
				.Where(o => o.Id == orderId)
				.ProjectToType<OrderResponse>()
				.AsSplitQuery()
				.FirstOrDefaultAsync();

		public async Task<UserOrderResponse?> GetUserOrderWithFullDetailsAsync(Guid orderId, Guid userId)
			=> await _context.Orders
				.Where(o => o.Id == orderId && o.CustomerId == userId)
				.ProjectToType<UserOrderResponse>()
				.FirstOrDefaultAsync();

		public async Task<ReceiptResponse?> GetInvoiceAsync(Guid orderId)
		{
			var order = await GetOrderForInvoiceAsync(orderId);
			return order == null ? null : MapToReceiptResponse(order);
		}

		public async Task<ReceiptResponse?> GetUserInvoiceAsync(Guid orderId, Guid userId)
		{
			var order = await GetOrderForInvoiceAsync(orderId, userId);
			return order == null ? null : MapToReceiptResponse(order);
		}

		public async Task<(string CustomerEmail, ReceiptResponse Invoice)?> GetOnlineOrderInvoiceEmailPayloadAsync(Guid orderId)
		{
			var order = await GetOrderForInvoiceAsync(orderId);
			if (order == null || order.Type != OrderType.Online || order.PaymentStatus != PaymentStatus.Paid)
			{
				return null;
			}

			if (string.IsNullOrWhiteSpace(order.Customer?.Email))
			{
				return null;
			}

			var invoice = MapToReceiptResponse(order);
			return (order.Customer.Email!, invoice);
		}

		public async Task<Order?> GetOrderForStatusUpdateAsync(Guid orderId)
			=> await _context.Orders
				.Include(o => o.ShippingInfo)
				.Include(o => o.OrderDetails)
				.FirstOrDefaultAsync(o => o.Id == orderId);

		public async Task<Order?> GetOrderForCancellationAsync(Guid orderId)
			=> await _context.Orders
				.Include(o => o.ShippingInfo)
				.FirstOrDefaultAsync(o => o.Id == orderId);

		public async Task<Order?> GetOrderForMarkUsedVoucherAsync(Guid orderId)
			=> await _context.Orders
				.Include(o => o.UserVoucher)
				.FirstOrDefaultAsync(o => o.Id == orderId);

		public async Task<Order?> GetOrderForPickListAsync(Guid orderId)
			=> await _context.Orders
				.Include(o => o.OrderDetails)
				.FirstOrDefaultAsync(o => o.Id == orderId && o.Status == OrderStatus.Processing);

		public async Task<Order?> GetOrderForFulfillmentAsync(Guid orderId)
			=> await _context.Orders
				.Include(o => o.ShippingInfo)
				.Include(o => o.OrderDetails)
				.FirstOrDefaultAsync(o => o.Id == orderId);

		public async Task<Order?> GetOrderForSwapDamagedStockAsync(Guid orderId)
			=> await _context.Orders
				.Include(o => o.OrderDetails)
				.FirstOrDefaultAsync(o => o.Id == orderId);

		public async Task<Order?> GetOrderWithDetailsForShippingAsync(Guid orderId)
			=> await _context.Orders
				.Include(o => o.OrderDetails)
				.FirstOrDefaultAsync(o => o.Id == orderId);

		private Task<Order?> GetOrderForInvoiceAsync(Guid orderId, Guid? userId = null)
		{
			var query = _context.Orders
				.Include(o => o.Customer)
				.Include(o => o.Staff)
				.Include(o => o.RecipientInfo)
				.Include(o => o.ShippingInfo)
				.Include(o => o.PaymentTransactions)
				.Include(o => o.OrderDetails)
					.ThenInclude(od => od.ProductVariant)
						.ThenInclude(v => v.Product)
				.Include(o => o.OrderDetails)
					.ThenInclude(od => od.ProductVariant)
						.ThenInclude(v => v.Concentration)
				.AsSplitQuery();

			if (userId.HasValue)
			{
				query = query.Where(o => o.CustomerId == userId.Value);
			}

			return query.FirstOrDefaultAsync(o => o.Id == orderId);
		}

		private static ReceiptResponse MapToReceiptResponse(Order order)
		{
			var subtotal = order.OrderDetails.Sum(od => od.UnitPrice * od.Quantity);
			var shippingFee = order.ShippingInfo?.ShippingFee ?? 0m;
			var discount = subtotal + shippingFee > order.TotalAmount
				? subtotal + shippingFee - order.TotalAmount
				: 0m;

			var successfulPayment = order.PaymentTransactions
				.Where(pt => pt.TransactionStatus == TransactionStatus.Success)
				.OrderByDescending(pt => pt.UpdatedAt ?? pt.CreatedAt)
				.FirstOrDefault();

			var latestPayment = successfulPayment ?? order.PaymentTransactions
				.OrderByDescending(pt => pt.UpdatedAt ?? pt.CreatedAt)
				.FirstOrDefault();

			var recipientAddress = order.RecipientInfo == null
				? "N/A"
				: string.Join(", ",
					new[]
					{
						order.RecipientInfo.FullAddress,
						order.RecipientInfo.WardName,
						order.RecipientInfo.DistrictName,
						order.RecipientInfo.ProvinceName
					}.Where(x => !string.IsNullOrWhiteSpace(x)));

			return new ReceiptResponse
			{
				OrderId = order.Id,
				Code = order.Code,
				OrderDate = order.PaidAt ?? order.CreatedAt,
				OrderStatus = order.Status.ToString(),
				StaffName = order.Staff?.FullName ?? "N/A",
				CustomerName = order.Customer?.FullName ?? order.RecipientInfo?.RecipientName ?? "Guest customer",
				RecipientPhone = order.RecipientInfo?.RecipientPhoneNumber ?? order.Customer?.PhoneNumber ?? "N/A",
				RecipientAddress = recipientAddress,
				Items = order.OrderDetails.Select(MapToReceiptItem).ToList(),
				Subtotal = subtotal,
				Discount = discount,
				Tax = 0,
				Total = order.TotalAmount,
				PaymentMethod = latestPayment?.Method.ToString() ?? "N/A",
				Note = order.Type == OrderType.Offline
					? "Physical in-store invoice."
					: "Online order invoice."
			};
		}

		private static ReceiptItemDto MapToReceiptItem(OrderDetail detail)
		{
			var variant = detail.ProductVariant;
			var variantParts = new List<string>();

			if (variant != null)
			{
				variantParts.Add($"{variant.VolumeMl}ml");
				if (!string.IsNullOrWhiteSpace(variant.Concentration?.Name))
				{
					variantParts.Add(variant.Concentration.Name);
				}

				variantParts.Add(variant.Type.ToString());
			}

			return new ReceiptItemDto
			{
				ProductName = variant?.Product?.Name ?? "Unknown Product",
				VariantInfo = variantParts.Count > 0 ? string.Join(" ", variantParts) : "N/A",
				Quantity = detail.Quantity,
				UnitPrice = detail.UnitPrice,
				Subtotal = detail.UnitPrice * detail.Quantity
			};
		}
	}
}
