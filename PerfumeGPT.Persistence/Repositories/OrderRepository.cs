using Microsoft.EntityFrameworkCore;
using PerfumeGPT.Application.DTOs.Requests.Orders;
using PerfumeGPT.Application.DTOs.Responses.Orders;
using PerfumeGPT.Application.Interfaces.Repositories;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Domain.Enums;
using PerfumeGPT.Persistence.Contexts;
using PerfumeGPT.Persistence.Extensions;
using PerfumeGPT.Persistence.Repositories.Commons;
using System.Linq.Expressions;
using System.Text.Json;

namespace PerfumeGPT.Persistence.Repositories
{
	public class OrderRepository : GenericRepository<Order>, IOrderRepository
	{
		public OrderRepository(PerfumeDbContext context) : base(context) { }

		public async Task<(List<OrderListItem> Orders, int TotalCount)> GetPagedOrdersAsync(GetPagedOrdersRequest request, int returnOrderAllowanceInDays)
		{
			Expression<Func<Order, bool>> filter = o => true;

			if (request.UserId.HasValue)
			{
				var userId = request.UserId.Value;
				Expression<Func<Order, bool>> userFilter = o => o.CustomerId == userId;
				filter = filter.AndAlso(userFilter);
			}

			if (!string.IsNullOrWhiteSpace(request.OrderCode))
			{
				var orderCodeFilter = EfCollationExtensions.CollateContains<Order>(
					o => o.Code,
					request.OrderCode.Trim());
				filter = filter.AndAlso(orderCodeFilter);
			}

			if (request.Status.HasValue)
			{
				var status = request.Status.Value;
				Expression<Func<Order, bool>> statusFilter = o => o.Status == status;
				filter = filter.AndAlso(statusFilter);
			}

			if (request.Type.HasValue)
			{
				var type = request.Type.Value;
				Expression<Func<Order, bool>> typeFilter = o => o.Type == type;
				filter = filter.AndAlso(typeFilter);
			}

			if (request.PaymentStatus.HasValue)
			{
				var paymentStatus = request.PaymentStatus.Value;
				Expression<Func<Order, bool>> paymentStatusFilter = o => o.PaymentStatus == paymentStatus;
				filter = filter.AndAlso(paymentStatusFilter);
			}

			if (request.FromDate.HasValue)
			{
				var fromDate = request.FromDate.Value;
				Expression<Func<Order, bool>> fromDateFilter = o => o.CreatedAt >= fromDate;
				filter = filter.AndAlso(fromDateFilter);
			}

			if (request.ToDate.HasValue)
			{
				var toDate = request.ToDate.Value;
				Expression<Func<Order, bool>> toDateFilter = o => o.CreatedAt <= toDate;
				filter = filter.AndAlso(toDateFilter);
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

				var searchFilter = orderIdFilter.OrElse(customerNameFilter);
				filter = filter.AndAlso(searchFilter);
			}

			IQueryable<Order> query = _context.Orders.Where(filter);
			var totalCount = await query.CountAsync();

			var allowedSortColumns = new HashSet<string>(StringComparer.Ordinal)
			{
				nameof(Order.Id), nameof(Order.Code), nameof(Order.Type), nameof(Order.Status),
				nameof(Order.PaymentStatus), nameof(Order.TotalAmount), nameof(Order.CreatedAt),
				nameof(Order.UpdatedAt), nameof(Order.PaymentExpiresAt)
			};

			string? sortBy = null;
			if (!string.IsNullOrWhiteSpace(request.SortBy))
			{
				var trimmedSortBy = request.SortBy.Trim();
				sortBy = trimmedSortBy.Length == 1
					? char.ToUpper(trimmedSortBy[0]).ToString()
					: char.ToUpper(trimmedSortBy[0]) + trimmedSortBy.Substring(1);
			}

			var sortedQuery = !string.IsNullOrWhiteSpace(sortBy) && allowedSortColumns.Contains(sortBy)
				? query.ApplySorting(sortBy, request.IsDescending)
				: query.OrderByDescending(o => o.CreatedAt);

			// Kéo dữ liệu lên RAM trước để có thể parse JSON Snapshot an toàn
			var dbOrders = await sortedQuery
				.Skip((request.PageNumber - 1) * request.PageSize)
				.Take(request.PageSize)
				.Include(o => o.Customer)
				.Include(o => o.Staff)
				.Include(o => o.ForwardShipping)
				.Include(o => o.PaymentTransactions)
				.Include(o => o.OrderDetails)
					.ThenInclude(od => od.ProductVariant).ThenInclude(v => v.Media)
				.AsSplitQuery()
				.ToListAsync();

			return (dbOrders.Select(o => new OrderListItem
			{
				Id = o.Id,
				Code = o.Code,
				CustomerId = o.CustomerId,
				CustomerName = o.Customer?.FullName,
				StaffId = o.StaffId,
				StaffName = o.Staff?.FullName,
				Type = o.Type,
				Status = o.Status,
				PaymentStatus = o.PaymentStatus,
				TotalAmount = o.TotalAmount,
				RequiredDepositAmount = o.RequiredDepositAmount,
				PaidAmount = o.PaidAmount,
				RemainingAmount = o.TotalAmount - o.PaidAmount,
				ItemCount = o.OrderDetails.Count,
				IsReturnalbe = o.Status == OrderStatus.Delivered
						&& o.ForwardShipping?.ShippedDate >= DateTime.UtcNow.AddDays(-returnOrderAllowanceInDays),
				ShippingStatus = o.ForwardShipping?.Status,
				CreatedAt = o.CreatedAt,
				PaymentExpiresAt = o.PaymentExpiresAt,
				UpdatedAt = o.UpdatedAt,
				OrderDetails = o.OrderDetails.Select(od => new OrderDetailListItem
				{
					Id = od.Id,
					VariantId = od.VariantId,
					VariantName = ParseVariantNameForDisplay(od.Snapshot, od.ProductVariant),
					ImageUrl = od.ProductVariant?.Media.FirstOrDefault(m => m.IsPrimary)?.Url
							?? od.ProductVariant?.Media.FirstOrDefault()?.Url,
					Quantity = od.Quantity,
					UnitPrice = od.UnitPrice,
					Total = (od.UnitPrice * od.Quantity) - od.PromotionDiscountAmount - od.ApportionedDiscount,
					RefunablePrice = od.Quantity > 0
						? ((od.UnitPrice * od.Quantity) - od.PromotionDiscountAmount - od.ApportionedDiscount) / od.Quantity
						: 0m,
				}).ToList(),
				PaymentTransactions = o.PaymentTransactions
					.Select(pt => new PaymentInfoResponse
					{
						Id = pt.Id,
						TransactionType = pt.TransactionType,
						Status = pt.TransactionStatus,
						PaymentMethod = pt.Method,
						FailureReason = pt.FailureReason,
						TotalAmount = pt.Amount
					}).ToList(),
			}).ToList(), totalCount);
		}

		public async Task<OrderResponse?> GetOrderWithFullDetailsAsync(Guid orderId, int returnOrderAllowanceInDays)
		{
			var orderFromDb = await GetOrderForDetailViewAsync(o => o.Id == orderId);
			return orderFromDb == null ? null : MapToOrderResponse(orderFromDb, returnOrderAllowanceInDays);
		}

		public async Task<OrderResponse?> GetOrderWithFullDetailsByCodeAsync(string orderCode)
		{
			var orderFromDb = await GetOrderForDetailViewAsync(o => o.Code == orderCode);
			return orderFromDb == null ? null : MapToOrderResponse(orderFromDb, 0);
		}

		public async Task<UserOrderResponse?> GetUserOrderWithFullDetailsAsync(Guid orderId, Guid userId, int returnOrderAllowanceInDays)
		{
			var orderFromDb = await GetOrderForDetailViewAsync(o => o.Id == orderId && o.CustomerId == userId);
			if (orderFromDb == null) return null;

			var response = MapToOrderResponse(orderFromDb, returnOrderAllowanceInDays);

			return new UserOrderResponse
			{
				Id = response.Id,
				Code = response.Code,
				Type = response.Type,
				Status = response.Status,
				IsReturnable = response.IsReturnable,
				PaymentStatus = response.PaymentStatus,
				TotalAmount = response.TotalAmount,
				RequiredDepositAmount = response.RequiredDepositAmount,
				DepositAmount = orderFromDb.PolicyDepositAmount,
				PaidAmount = response.PaidAmount,
				RemainingAmount = response.RemainingAmount,
				SubTotal = response.SubTotal,
				ShippingFee = response.ShippingFee,
				VoucherCode = response.VoucherCode,
				VoucherType = response.VoucherType,
				VoucherDiscountTotal = response.VoucherDiscountTotal,
				PaymentExpiresAt = response.PaymentExpiresAt,
				PaidAt = response.PaidAt,
				CreatedAt = response.CreatedAt,
				UpdatedAt = response.UpdatedAt,
				PaymentTransactions = response.PaymentTransactions,
				ShippingInfo = response.ShippingInfo,
				RecipientInfo = response.RecipientInfo,
				OrderDetails = response.OrderDetails
			};
		}

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

		public async Task<(string CustomerEmail, ReceiptResponse Invoice, string OrderCode)?> GetOnlineOrderInvoiceEmailPayloadAsync(Guid orderId)
		{
			var order = await GetOrderForInvoiceAsync(orderId);
			if (order == null || order.Type != OrderType.Online || order.PaymentStatus != PaymentStatus.Paid)
				return null;

			if (string.IsNullOrWhiteSpace(order.Customer?.Email))
				return null;

			var invoice = MapToReceiptResponse(order);
			return (order.Customer.Email!, invoice, order.Code);
		}

		// Mọi truy vấn Order liên quan nghiệp vụ nội bộ đều được gộp về đây
		#region Standard Internal Queries
		public async Task<Order?> GetOrderForStatusUpdateAsync(Guid orderId)
			=> await _context.Orders.Include(o => o.ForwardShipping).Include(o => o.ContactAddress)
				.Include(o => o.OrderDetails).Include(o => o.PaymentTransactions).AsSplitQuery()
				.FirstOrDefaultAsync(o => o.Id == orderId);

		public async Task<Order?> GetOrderForReturnRequestAsync(Guid orderId)
			=> await _context.Orders.Include(o => o.ForwardShipping).Include(o => o.OrderDetails).AsSplitQuery()
				.FirstOrDefaultAsync(o => o.Id == orderId);

		public async Task<Order?> GetOrderForCancellationAsync(Guid orderId)
			=> await _context.Orders.Include(o => o.ForwardShipping).FirstOrDefaultAsync(o => o.Id == orderId);

		public async Task<Order?> GetOrderForMarkUsedVoucherAsync(Guid orderId)
			=> await _context.Orders.Include(o => o.UserVoucher).FirstOrDefaultAsync(o => o.Id == orderId);

		public async Task<Order?> GetOrderForPaymentSuccessLogAsync(Guid orderId)
			=> await _context.Orders.Include(o => o.Customer).Include(o => o.PaymentTransactions).AsSplitQuery()
				.FirstOrDefaultAsync(o => o.Id == orderId);

		public async Task<Order?> GetOrderForPickListAsync(Guid orderId)
			=> await _context.Orders.Include(o => o.OrderDetails)
				.FirstOrDefaultAsync(o => o.Id == orderId && o.Status == OrderStatus.Preparing);

		public async Task<Order?> GetOrderForFulfillmentAsync(Guid orderId)
			=> await _context.Orders.Include(o => o.ContactAddress).Include(o => o.ForwardShipping).Include(o => o.OrderDetails)
				.FirstOrDefaultAsync(o => o.Id == orderId);

		public async Task<Order?> GetOrderForSwapDamagedStockAsync(Guid orderId)
			=> await _context.Orders.Include(o => o.OrderDetails).FirstOrDefaultAsync(o => o.Id == orderId);

		public async Task<Order?> GetOrderWithDetailsForShippingAsync(Guid orderId)
			=> await _context.Orders.Include(o => o.OrderDetails).ThenInclude(od => od.ProductVariant)
				.FirstOrDefaultAsync(o => o.Id == orderId);

		public async Task<List<Order>> GetExpiringUnpaidOrdersAsync(int limit)
		{
			var now = DateTime.UtcNow;
			return await _context.Orders
				.Where(o => o.Status == OrderStatus.Pending && o.PaymentStatus == PaymentStatus.Unpaid
						&& o.PaymentExpiresAt.HasValue && o.PaymentExpiresAt.Value < now)
				.Include(o => o.OrderDetails).OrderBy(o => o.PaymentExpiresAt).Take(limit).ToListAsync();
		}

		public async Task<string?> GetOrderCodeAsync(Guid orderId)
			=> await _context.Orders.Where(o => o.Id == orderId).Select(o => o.Code).FirstOrDefaultAsync();
		#endregion

		#region Data Mapping Helpers
		private async Task<Order?> GetOrderForDetailViewAsync(Expression<Func<Order, bool>> predicate)
		{
			return await _context.Orders
				.AsNoTracking()
				.Where(predicate)
				.Include(o => o.Customer)
				.Include(o => o.Staff)
				.Include(o => o.ForwardShipping)
				.Include(o => o.ContactAddress)
				.Include(o => o.UserVoucher).ThenInclude(uv => uv!.Voucher)
				.Include(o => o.PaymentTransactions)
				.Include(o => o.OrderDetails)
					.ThenInclude(od => od.ProductVariant).ThenInclude(v => v.Media)
				.Include(o => o.OrderDetails)
					.ThenInclude(od => od.StockReservations)
						.ThenInclude(sr => sr.Batch)
				.AsSplitQuery()
				.FirstOrDefaultAsync();
		}

		private static OrderResponse MapToOrderResponse(Order order, int returnOrderAllowanceInDays)
		{
			return new OrderResponse
			{
				Id = order.Id,
				Code = order.Code,
				CustomerId = order.CustomerId,
				CustomerName = order.Customer?.FullName,
				CustomerEmail = order.Customer?.Email,
				CustomerPhoneNumber = order.Customer?.PhoneNumber ?? order.GuestEmailOrPhone,
				StaffId = order.StaffId,
				StaffName = order.Staff?.FullName,
				Type = order.Type,
				Status = order.Status,
				IsReturnable = order.Status == OrderStatus.Delivered
					&& order.ForwardShipping?.ShippedDate >= DateTime.UtcNow.AddDays(-returnOrderAllowanceInDays),
				PaymentStatus = order.PaymentStatus,
				TotalAmount = order.TotalAmount,
				RequiredDepositAmount = order.RequiredDepositAmount,
				PaidAmount = order.PaidAmount,
				RemainingAmount = order.TotalAmount - order.PaidAmount,
				SubTotal = order.OrderDetails.Sum(od => (od.UnitPrice * od.Quantity) - od.PromotionDiscountAmount),
				ShippingFee = order.ForwardShipping?.ShippingFee ?? 0m,
				VoucherId = order.UserVoucherId,
				VoucherCode = order.UserVoucher?.Voucher.Code,
				VoucherType = order.UserVoucher?.Voucher.ApplyType,
				VoucherDiscountTotal = order.OrderDetails.Sum(od => od.ApportionedDiscount),
				PaymentExpiresAt = order.PaymentExpiresAt,
				PaidAt = order.PaidAt,
				CreatedAt = order.CreatedAt,
				UpdatedAt = order.UpdatedAt,
				PaymentTransactions = order.PaymentTransactions.Select(pt => new PaymentInfoResponse
				{
					Id = pt.Id,
					TransactionType = pt.TransactionType,
					Status = pt.TransactionStatus,
					PaymentMethod = pt.Method,
					FailureReason = pt.FailureReason,
					TotalAmount = pt.Amount
				}).ToList(),
				ShippingInfo = order.ForwardShipping == null ? null : new ShippingInfoResponse
				{
					Id = order.ForwardShipping.Id,
					CarrierName = order.ForwardShipping.CarrierName,
					TrackingNumber = order.ForwardShipping.TrackingNumber,
					ShippingFee = order.ForwardShipping.ShippingFee,
					Status = order.ForwardShipping.Status,
					EstimatedDeliveryDate = order.ForwardShipping.EstimatedDeliveryDate,
					ShippedDate = order.ForwardShipping.ShippedDate
				},
				RecipientInfo = order.ContactAddress == null ? null : new RecipientInfoResponse
				{
					Id = order.ContactAddress.Id,
					RecipientName = order.ContactAddress.ContactName,
					RecipientPhoneNumber = order.ContactAddress.ContactPhoneNumber,
					DistrictName = order.ContactAddress.DistrictName,
					WardName = order.ContactAddress.WardName,
					ProvinceName = order.ContactAddress.ProvinceName,
					FullAddress = order.ContactAddress.FullAddress
				},
				OrderDetails = order.OrderDetails.Select(od => new OrderDetailResponse
				{
					Id = od.Id,
					VariantId = od.VariantId,
					VariantName = ParseVariantNameForDisplay(od.Snapshot, od.ProductVariant),
					ImageUrl = od.ProductVariant?.Media.FirstOrDefault(m => m.IsPrimary)?.Url ?? od.ProductVariant?.Media.FirstOrDefault()?.Url,
					Quantity = od.Quantity,
					UnitPrice = od.UnitPrice,
					CampaignDiscount = od.PromotionDiscountAmount,
					CampaignPrice = od.Quantity > 0 ? od.UnitPrice - (od.PromotionDiscountAmount / od.Quantity) : od.UnitPrice,
					VoucherDiscount = od.ApportionedDiscount,
					ItemTotal = (od.UnitPrice * od.Quantity) - od.PromotionDiscountAmount,
					RefunablePrice = (od.UnitPrice * od.Quantity) - od.PromotionDiscountAmount - od.ApportionedDiscount,
					Total = (od.UnitPrice * od.Quantity) - od.PromotionDiscountAmount - od.ApportionedDiscount,
					ReservedBatches = od.StockReservations
						.Where(sr => sr.Status == ReservationStatus.Reserved || sr.Status == ReservationStatus.Committed)
						.Select(sr => new ReservedBatchResponse
						{
							BatchId = sr.BatchId,
							BatchCode = sr.Batch.BatchCode,
							ReservedQuantity = sr.ReservedQuantity,
							ExpiryDate = sr.Batch.ExpiryDate
						}).ToList()
				}).ToList()
			};
		}

		private Task<Order?> GetOrderForInvoiceAsync(Guid orderId, Guid? userId = null)
		{
			var query = _context.Orders.AsNoTracking()
				.Include(o => o.Customer).Include(o => o.Staff).Include(o => o.ContactAddress)
				.Include(o => o.ForwardShipping).Include(o => o.PaymentTransactions)
				.Include(o => o.OrderDetails).ThenInclude(od => od.ProductVariant).ThenInclude(v => v.Product)
				.Include(o => o.OrderDetails).ThenInclude(od => od.ProductVariant).ThenInclude(v => v.Concentration)
				.AsSplitQuery();

			if (userId.HasValue) query = query.Where(o => o.CustomerId == userId.Value);

			return query.FirstOrDefaultAsync(o => o.Id == orderId);
		}

		private static ReceiptResponse MapToReceiptResponse(Order order)
		{
			var subtotal = order.OrderDetails.Sum(od => od.UnitPrice * od.Quantity);
			var shippingFee = order.ForwardShipping?.ShippingFee ?? 0m;
			var discount = subtotal + shippingFee > order.TotalAmount ? subtotal + shippingFee - order.TotalAmount : 0m;

			var successfulPayment = order.PaymentTransactions.Where(pt => pt.TransactionStatus == TransactionStatus.Success)
				.OrderByDescending(pt => pt.UpdatedAt ?? pt.CreatedAt).FirstOrDefault();

			var latestPayment = successfulPayment ?? order.PaymentTransactions.OrderByDescending(pt => pt.UpdatedAt ?? pt.CreatedAt).FirstOrDefault();

			var recipientAddress = order.ContactAddress == null ? "N/A"
				  : string.Join(", ", new[] { order.ContactAddress.FullAddress, order.ContactAddress.WardName, order.ContactAddress.DistrictName, order.ContactAddress.ProvinceName }.Where(x => !string.IsNullOrWhiteSpace(x)));

			return new ReceiptResponse
			{
				OrderId = order.Id,
				Code = order.Code,
				OrderDate = order.PaidAt ?? order.CreatedAt,
				OrderStatus = order.Status.ToString(),
				StaffName = order.Staff?.FullName ?? "N/A",
				CustomerName = order.Customer?.FullName ?? order.ContactAddress?.ContactName ?? "Guest customer",
				RecipientPhone = order.ContactAddress?.ContactPhoneNumber ?? order.Customer?.PhoneNumber ?? "N/A",
				RecipientAddress = recipientAddress,
				Items = order.OrderDetails.Select(MapToReceiptItem).ToList(),
				Subtotal = subtotal,
				DepositeAmount = order.PolicyDepositAmount,
				RemainingAmount = order.TotalAmount - order.PaidAmount,
				ShippingFee = shippingFee,
				Discount = discount,
				Tax = 0,
				Total = order.TotalAmount,
				PaymentMethod = latestPayment?.Method.ToString() ?? "N/A",
				Note = order.Type == OrderType.Offline ? "Hóa đơn mua hàng tại cửa hàng." : "Hóa đơn mua hàng trực tuyến. Cảm ơn quý khách đã mua sắm tại PerfumeGPT!"
			};
		}

		// 💡 NÂNG CẤP: Dùng JSON Snapshot để xuất biên lai, bất di bất dịch dù Product có bị xóa!
		private static ReceiptItemDto MapToReceiptItem(OrderDetail detail)
		{
			string productName = "Unknown Product";
			string variantInfo = "N/A";

			try
			{
				using var doc = JsonDocument.Parse(detail.Snapshot);
				var root = doc.RootElement;
				productName = root.TryGetProperty("ProductName", out var n) ? n.GetString() ?? productName : productName;

				var volume = root.TryGetProperty("VolumeMl", out var v) ? v.GetInt32() : 0;
				var concentration = root.TryGetProperty("Concentration", out var c) ? c.GetString() : null;
				var type = root.TryGetProperty("VariantType", out var t) ? t.GetString() : null;

				var parts = new List<string>();
				if (volume > 0) parts.Add($"{volume}ml");
				if (!string.IsNullOrWhiteSpace(concentration)) parts.Add(concentration);
				if (!string.IsNullOrWhiteSpace(type)) parts.Add(type);

				if (parts.Count > 0) variantInfo = string.Join(" ", parts);
			}
			catch
			{
				if (detail.ProductVariant != null)
				{
					productName = detail.ProductVariant.Product?.Name ?? "Unknown Product";
					var parts = new List<string> { $"{detail.ProductVariant.VolumeMl}ml" };
					if (!string.IsNullOrWhiteSpace(detail.ProductVariant.Concentration?.Name)) parts.Add(detail.ProductVariant.Concentration.Name);
					parts.Add(detail.ProductVariant.Type.ToString());
					variantInfo = string.Join(" ", parts);
				}
			}

			return new ReceiptItemDto
			{
				ProductName = productName,
				VariantInfo = variantInfo,
				Quantity = detail.Quantity,
				UnitPrice = detail.UnitPrice,
				Subtotal = detail.UnitPrice * detail.Quantity - detail.ApportionedDiscount
			};
		}

		private static string ParseVariantNameForDisplay(string snapshot, ProductVariant? fallback)
		{
			try
			{
				using var doc = JsonDocument.Parse(snapshot);
				var root = doc.RootElement;
				var sku = root.TryGetProperty("Sku", out var s) ? s.GetString() : "Unknown";
				var volume = root.TryGetProperty("VolumeMl", out var v) ? v.GetInt32().ToString() : "0";
				return $"{sku} - {volume}ml";
			}
			catch
			{
				return fallback != null ? $"{fallback.Sku} - {fallback.VolumeMl}ml" : string.Empty;
			}
		}
		#endregion
	}
}