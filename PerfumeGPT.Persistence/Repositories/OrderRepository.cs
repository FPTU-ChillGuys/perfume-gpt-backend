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

		public async Task<(List<OrderListItem> Orders, int TotalCount)> GetPagedOrdersAsync(GetPagedOrdersRequest request)
		{
			IQueryable<Order> query = _context.Orders.AsQueryable();

			if (request.UserId.HasValue)
			{
				query = query.Where(o => o.CustomerId == request.UserId.Value);
			}

			if (!string.IsNullOrWhiteSpace(request.OrderCode))
			{
				var orderCodeFilter = EfCollationExtensions.CollateContains<Order>(
					o => o.Code,
					request.OrderCode.Trim());
				query = query.Where(orderCodeFilter);
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
				.Select(o => new OrderListItem
				{
					Id = o.Id,
					Code = o.Code,
					CustomerId = o.CustomerId,
					CustomerName = o.Customer != null ? o.Customer.FullName : null,
					StaffId = o.StaffId,
					StaffName = o.Staff != null ? o.Staff.FullName : null,
					Type = o.Type,
					Status = o.Status,
					PaymentStatus = o.PaymentStatus,
					TotalAmount = o.TotalAmount,
					RequiredDepositAmount = o.RequiredDepositAmount,
					PaidAmount = o.PaidAmount,
					RemainingAmount = o.TotalAmount - o.PaidAmount,
					ItemCount = o.OrderDetails.Count,
					IsReturnalbe = o.Status == OrderStatus.Delivered
						   && o.ForwardShipping != null
							&& o.ForwardShipping.ShippedDate.HasValue
							&& o.ForwardShipping.ShippedDate.Value >= DateTime.UtcNow.AddDays(-7),
					ShippingStatus = o.ForwardShipping != null ? o.ForwardShipping.Status : null,
					CreatedAt = o.CreatedAt,
					PaymentExpiresAt = o.PaymentExpiresAt,
					UpdatedAt = o.UpdatedAt,
					OrderDetails = o.OrderDetails.Select(od => new OrderDetailListItem
					{
						Id = od.Id,
						VariantId = od.VariantId,
						VariantName = od.ProductVariant != null ? $"{od.ProductVariant.Sku} - {od.ProductVariant.VolumeMl}ml" : string.Empty,
						ImageUrl = od.ProductVariant != null && od.ProductVariant.Media.Count > 0
							? (od.ProductVariant.Media.Where(m => m.IsPrimary).Select(m => m.Url).FirstOrDefault()
								?? od.ProductVariant.Media.Select(m => m.Url).FirstOrDefault())
							: null,
						Quantity = od.Quantity,
						UnitPrice = od.UnitPrice,
						// Lấy Tổng tiền gốc - Tiền giảm Flash Sale - Tiền gánh Voucher
						Total = (od.UnitPrice * od.Quantity) - od.PromotionDiscountAmount - od.ApportionedDiscount,

						// Lấy cái Total vừa tính ở trên chia đều cho số lượng
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
				})
				.ToListAsync();

			return (orders, totalCount);
		}

		public async Task<OrderResponse?> GetOrderWithFullDetailsAsync(Guid orderId)
		=> await _context.Orders
			.Where(o => o.Id == orderId)
			.Select(o => new OrderResponse
			{
				Id = o.Id,
				Code = o.Code,
				CustomerId = o.CustomerId,
				CustomerName = o.Customer != null ? o.Customer.FullName : null,
				CustomerEmail = o.Customer != null ? o.Customer.Email : null,
				CustomerPhoneNumber = o.Customer != null ? o.Customer.PhoneNumber : o.GuestEmailOrPhone,
				StaffId = o.StaffId,
				StaffName = o.Staff != null ? o.Staff.FullName : null,
				Type = o.Type,
				Status = o.Status,
				PaymentStatus = o.PaymentStatus,
				TotalAmount = o.TotalAmount,
				RequiredDepositAmount = o.RequiredDepositAmount,
				PaidAmount = o.PaidAmount,
				RemainingAmount = o.TotalAmount - o.PaidAmount,
				SubTotal = o.OrderDetails.Sum(od => (od.UnitPrice * od.Quantity) - od.PromotionDiscountAmount),
				ShippingFee = o.ForwardShipping != null ? o.ForwardShipping.ShippingFee : 0m,
				VoucherId = o.UserVoucherId,
				VoucherCode = o.UserVoucher != null ? o.UserVoucher.Voucher.Code : null,
				VoucherType = o.UserVoucher != null ? o.UserVoucher.Voucher.ApplyType : null,
				VoucherDiscountTotal = o.OrderDetails.Sum(od => od.ApportionedDiscount),
				PaymentExpiresAt = o.PaymentExpiresAt,
				PaidAt = o.PaidAt,
				CreatedAt = o.CreatedAt,
				UpdatedAt = o.UpdatedAt,
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
				ShippingInfo = o.ForwardShipping == null ? null : new ShippingInfoResponse
				{
					Id = o.ForwardShipping.Id,
					CarrierName = o.ForwardShipping.CarrierName,
					TrackingNumber = o.ForwardShipping.TrackingNumber,
					ShippingFee = o.ForwardShipping.ShippingFee,
					Status = o.ForwardShipping.Status,
					EstimatedDeliveryDate = o.ForwardShipping.EstimatedDeliveryDate,
					ShippedDate = o.ForwardShipping.ShippedDate
				},
				RecipientInfo = o.ContactAddress == null ? null : new RecipientInfoResponse
				{
					Id = o.ContactAddress.Id,
					RecipientName = o.ContactAddress.ContactName,
					RecipientPhoneNumber = o.ContactAddress.ContactPhoneNumber,
					DistrictName = o.ContactAddress.DistrictName,
					WardName = o.ContactAddress.WardName,
					ProvinceName = o.ContactAddress.ProvinceName,
					FullAddress = o.ContactAddress.FullAddress
				},
				OrderDetails = o.OrderDetails.Select(od => new OrderDetailResponse
				{
					Id = od.Id,
					VariantId = od.VariantId,
					VariantName = od.ProductVariant != null ? $"{od.ProductVariant.Sku} - {od.ProductVariant.VolumeMl}ml" : string.Empty,
					ImageUrl = od.ProductVariant != null && od.ProductVariant.Media.Count > 0
						? (od.ProductVariant.Media.Where(m => m.IsPrimary).Select(m => m.Url).FirstOrDefault()
							?? od.ProductVariant.Media.Select(m => m.Url).FirstOrDefault())
						: null,
					Quantity = od.Quantity,
					UnitPrice = od.UnitPrice,
					CampaignDiscount = od.PromotionDiscountAmount,
					CampaignPrice = od.Quantity > 0
					   ? od.UnitPrice - (od.PromotionDiscountAmount / od.Quantity)
					   : od.UnitPrice,
					VoucherDiscount = od.ApportionedDiscount,
					ItemTotal = (od.UnitPrice * od.Quantity) - od.PromotionDiscountAmount,
					RefunablePrice = (od.UnitPrice * od.Quantity) - od.PromotionDiscountAmount - od.ApportionedDiscount,
					Total = (od.UnitPrice * od.Quantity) - od.PromotionDiscountAmount - od.ApportionedDiscount,
					ReservedBatches = o.StockReservations
						.Where(sr => sr.VariantId == od.VariantId)
						.Select(sr => new ReservedBatchResponse
						{
							BatchId = sr.BatchId,
							BatchCode = sr.Batch.BatchCode,
							ReservedQuantity = sr.ReservedQuantity,
							ExpiryDate = sr.Batch.ExpiryDate
						}).ToList()
				}).ToList()
			})
			.AsSplitQuery()
			.FirstOrDefaultAsync();

		public async Task<OrderResponse?> GetOrderWithFullDetailsByCodeAsync(string orderCode)
		=> await _context.Orders
			.Where(o => o.Code == orderCode)
			.Select(o => new OrderResponse
			{
				Id = o.Id,
				Code = o.Code,
				CustomerId = o.CustomerId,
				CustomerName = o.Customer != null ? o.Customer.FullName : null,
				CustomerEmail = o.Customer != null ? o.Customer.Email : null,
				CustomerPhoneNumber = o.Customer != null ? o.Customer.PhoneNumber : o.GuestEmailOrPhone,
				StaffId = o.StaffId,
				StaffName = o.Staff != null ? o.Staff.FullName : null,
				Type = o.Type,
				Status = o.Status,
				PaymentStatus = o.PaymentStatus,
				TotalAmount = o.TotalAmount,
				RequiredDepositAmount = o.RequiredDepositAmount,
				PaidAmount = o.PaidAmount,
				RemainingAmount = o.TotalAmount - o.PaidAmount,
				SubTotal = o.OrderDetails.Sum(od => (od.UnitPrice * od.Quantity) - od.PromotionDiscountAmount),
				ShippingFee = o.ForwardShipping != null ? o.ForwardShipping.ShippingFee : 0m,
				VoucherId = o.UserVoucherId,
				VoucherCode = o.UserVoucher != null ? o.UserVoucher.Voucher.Code : null,
				VoucherType = o.UserVoucher != null ? o.UserVoucher.Voucher.ApplyType : null,
				VoucherDiscountTotal = o.OrderDetails.Sum(od => od.ApportionedDiscount),
				PaymentExpiresAt = o.PaymentExpiresAt,
				PaidAt = o.PaidAt,
				CreatedAt = o.CreatedAt,
				UpdatedAt = o.UpdatedAt,
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
				ShippingInfo = o.ForwardShipping == null ? null : new ShippingInfoResponse
				{
					Id = o.ForwardShipping.Id,
					CarrierName = o.ForwardShipping.CarrierName,
					TrackingNumber = o.ForwardShipping.TrackingNumber,
					ShippingFee = o.ForwardShipping.ShippingFee,
					Status = o.ForwardShipping.Status,
					EstimatedDeliveryDate = o.ForwardShipping.EstimatedDeliveryDate,
					ShippedDate = o.ForwardShipping.ShippedDate
				},
				RecipientInfo = o.ContactAddress == null ? null : new RecipientInfoResponse
				{
					Id = o.ContactAddress.Id,
					RecipientName = o.ContactAddress.ContactName,
					RecipientPhoneNumber = o.ContactAddress.ContactPhoneNumber,
					DistrictName = o.ContactAddress.DistrictName,
					WardName = o.ContactAddress.WardName,
					ProvinceName = o.ContactAddress.ProvinceName,
					FullAddress = o.ContactAddress.FullAddress
				},
				OrderDetails = o.OrderDetails.Select(od => new OrderDetailResponse
				{
					Id = od.Id,
					VariantId = od.VariantId,
					VariantName = od.ProductVariant != null ? $"{od.ProductVariant.Sku} - {od.ProductVariant.VolumeMl}ml" : string.Empty,
					ImageUrl = od.ProductVariant != null && od.ProductVariant.Media.Count > 0
						? (od.ProductVariant.Media.Where(m => m.IsPrimary).Select(m => m.Url).FirstOrDefault()
							?? od.ProductVariant.Media.Select(m => m.Url).FirstOrDefault())
						: null,
					Quantity = od.Quantity,
					UnitPrice = od.UnitPrice,
					CampaignDiscount = od.PromotionDiscountAmount,
					CampaignPrice = od.Quantity > 0
					   ? od.UnitPrice - (od.PromotionDiscountAmount / od.Quantity)
					   : od.UnitPrice,
					VoucherDiscount = od.ApportionedDiscount,
					ItemTotal = (od.UnitPrice * od.Quantity) - od.PromotionDiscountAmount,
					RefunablePrice = (od.UnitPrice * od.Quantity) - od.PromotionDiscountAmount - od.ApportionedDiscount,
					Total = (od.UnitPrice * od.Quantity) - od.PromotionDiscountAmount - od.ApportionedDiscount,
					ReservedBatches = o.StockReservations
						.Where(sr => sr.VariantId == od.VariantId)
						.Select(sr => new ReservedBatchResponse
						{
							BatchId = sr.BatchId,
							BatchCode = sr.Batch.BatchCode,
							ReservedQuantity = sr.ReservedQuantity,
							ExpiryDate = sr.Batch.ExpiryDate
						}).ToList()
				}).ToList()
			})
			.AsSplitQuery()
			.FirstOrDefaultAsync();

		public async Task<UserOrderResponse?> GetUserOrderWithFullDetailsAsync(Guid orderId, Guid userId)
		=> await _context.Orders
			.Where(o => o.Id == orderId && o.CustomerId == userId)
			.Select(o => new UserOrderResponse
			{
				Id = o.Id,
				Code = o.Code,
				Type = o.Type,
				Status = o.Status,
				IsReturnable = o.Status == OrderStatus.Delivered
					   && o.ForwardShipping != null
						&& o.ForwardShipping.ShippedDate.HasValue
						&& o.ForwardShipping.ShippedDate.Value >= DateTime.UtcNow.AddDays(-7),
				PaymentStatus = o.PaymentStatus,
				TotalAmount = o.TotalAmount,
				RequiredDepositAmount = o.RequiredDepositAmount,
				DepositAmount = o.PolicyDepositAmount,
				PaidAmount = o.PaidAmount,
				RemainingAmount = o.TotalAmount - o.PaidAmount,
				SubTotal = o.OrderDetails.Sum(od => (od.UnitPrice * od.Quantity) - od.PromotionDiscountAmount),
				ShippingFee = o.ForwardShipping != null ? o.ForwardShipping.ShippingFee : 0m,
				VoucherCode = o.UserVoucher != null ? o.UserVoucher.Voucher.Code : null,
				VoucherType = o.UserVoucher != null ? o.UserVoucher.Voucher.ApplyType : null,
				VoucherDiscountTotal = o.OrderDetails.Sum(od => od.ApportionedDiscount),
				PaymentExpiresAt = o.PaymentExpiresAt,
				PaidAt = o.PaidAt,
				CreatedAt = o.CreatedAt,
				UpdatedAt = o.UpdatedAt,
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
				ShippingInfo = o.ForwardShipping == null ? null : new ShippingInfoResponse
				{
					Id = o.ForwardShipping.Id,
					CarrierName = o.ForwardShipping.CarrierName,
					TrackingNumber = o.ForwardShipping.TrackingNumber,
					ShippingFee = o.ForwardShipping.ShippingFee,
					Status = o.ForwardShipping.Status,
					EstimatedDeliveryDate = o.ForwardShipping.EstimatedDeliveryDate,
					ShippedDate = o.ForwardShipping.ShippedDate
				},
				RecipientInfo = o.ContactAddress == null ? null : new RecipientInfoResponse
				{
					Id = o.ContactAddress.Id,
					RecipientName = o.ContactAddress.ContactName,
					RecipientPhoneNumber = o.ContactAddress.ContactPhoneNumber,
					DistrictName = o.ContactAddress.DistrictName,
					WardName = o.ContactAddress.WardName,
					ProvinceName = o.ContactAddress.ProvinceName,
					FullAddress = o.ContactAddress.FullAddress
				},
				OrderDetails = o.OrderDetails.Select(od => new OrderDetailResponse
				{
					Id = od.Id,
					VariantId = od.VariantId,
					VariantName = od.ProductVariant != null ? $"{od.ProductVariant.Sku} - {od.ProductVariant.VolumeMl}ml" : string.Empty,
					ImageUrl = od.ProductVariant != null && od.ProductVariant.Media.Count > 0
						? (od.ProductVariant.Media.Where(m => m.IsPrimary).Select(m => m.Url).FirstOrDefault()
							?? od.ProductVariant.Media.Select(m => m.Url).FirstOrDefault())
						: null,
					Quantity = od.Quantity,
					UnitPrice = od.UnitPrice,
					CampaignDiscount = od.PromotionDiscountAmount,
					CampaignPrice = od.Quantity > 0
					   ? od.UnitPrice - (od.PromotionDiscountAmount / od.Quantity)
					   : od.UnitPrice,
					VoucherDiscount = od.ApportionedDiscount,
					ItemTotal = (od.UnitPrice * od.Quantity) - od.PromotionDiscountAmount,
					RefunablePrice = (od.UnitPrice * od.Quantity) - od.PromotionDiscountAmount - od.ApportionedDiscount,
					Total = (od.UnitPrice * od.Quantity) - od.PromotionDiscountAmount - od.ApportionedDiscount,
					ReservedBatches = o.StockReservations
						.Where(sr => sr.VariantId == od.VariantId)
						.Select(sr => new ReservedBatchResponse
						{
							BatchId = sr.BatchId,
							BatchCode = sr.Batch.BatchCode,
							ReservedQuantity = sr.ReservedQuantity,
							ExpiryDate = sr.Batch.ExpiryDate
						}).ToList(),
				}).ToList()
			})
			.AsSplitQuery()
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

		public async Task<(string CustomerEmail, ReceiptResponse Invoice, string OrderCode)?> GetOnlineOrderInvoiceEmailPayloadAsync(Guid orderId)
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
			return (order.Customer.Email!, invoice, order.Code);
		}

		public async Task<Order?> GetOrderForStatusUpdateAsync(Guid orderId)
			=> await _context.Orders
				.Include(o => o.ForwardShipping)
				.Include(o => o.ContactAddress)
				.Include(o => o.OrderDetails)
				.Include(o => o.PaymentTransactions)
				.AsSplitQuery()
				.FirstOrDefaultAsync(o => o.Id == orderId);

		public async Task<Order?> GetOrderForCancellationAsync(Guid orderId)
			=> await _context.Orders
				.Include(o => o.ForwardShipping)
				.FirstOrDefaultAsync(o => o.Id == orderId);

		public async Task<Order?> GetOrderForMarkUsedVoucherAsync(Guid orderId)
		=> await _context.Orders
			.Include(o => o.UserVoucher)
			.FirstOrDefaultAsync(o => o.Id == orderId);

		public async Task<Order?> GetOrderForPaymentSuccessLogAsync(Guid orderId)
		=> await _context.Orders
			.Include(o => o.Customer)
			.Include(o => o.PaymentTransactions)
			.AsSplitQuery()
			.FirstOrDefaultAsync(o => o.Id == orderId);

		public async Task<Order?> GetOrderForPickListAsync(Guid orderId)
		=> await _context.Orders
			.Include(o => o.OrderDetails)
			.FirstOrDefaultAsync(o => o.Id == orderId && o.Status == OrderStatus.Preparing);

		public async Task<Order?> GetOrderForFulfillmentAsync(Guid orderId)
		=> await _context.Orders
			.Include(o => o.ContactAddress)
			.Include(o => o.ForwardShipping)
			.Include(o => o.OrderDetails)
			.FirstOrDefaultAsync(o => o.Id == orderId);

		public async Task<Order?> GetOrderForSwapDamagedStockAsync(Guid orderId)
		=> await _context.Orders
			.Include(o => o.OrderDetails)
			.FirstOrDefaultAsync(o => o.Id == orderId);

		public async Task<Order?> GetOrderWithDetailsForShippingAsync(Guid orderId)
		=> await _context.Orders
			.Include(o => o.OrderDetails)
			.ThenInclude(od => od.ProductVariant)
			.FirstOrDefaultAsync(o => o.Id == orderId);

		private Task<Order?> GetOrderForInvoiceAsync(Guid orderId, Guid? userId = null)
		{
			var query = _context.Orders.AsNoTracking()
				.Include(o => o.Customer)
				.Include(o => o.Staff)
				.Include(o => o.ContactAddress)
				.Include(o => o.ForwardShipping)
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
			var shippingFee = order.ForwardShipping?.ShippingFee ?? 0m;
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

			var recipientAddress = order.ContactAddress == null
				  ? "N/A"
				  : string.Join(", ",
					  new[]
					  {
						order.ContactAddress.FullAddress,
						order.ContactAddress.WardName,
						order.ContactAddress.DistrictName,
						order.ContactAddress.ProvinceName
					  }.Where(x => !string.IsNullOrWhiteSpace(x)));

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
				Items = [.. order.OrderDetails.Select(MapToReceiptItem)],
				Subtotal = subtotal,
				DepositeAmount = order.PolicyDepositAmount,
				RemainingAmount = order.TotalAmount - order.PaidAmount,
				ShippingFee = shippingFee,
				Discount = discount,
				Tax = 0,
				Total = order.TotalAmount,
				PaymentMethod = latestPayment?.Method.ToString() ?? "N/A",
				Note = order.Type == OrderType.Offline
					? "Hóa đơn mua hàng tại cửa hàng."
					: "Hóa đơn mua hàng trực tuyến. Cảm ơn quý khách đã mua sắm tại PerfumeGPT!"
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
				Subtotal = detail.UnitPrice * detail.Quantity - detail.ApportionedDiscount
			};
		}
	}
}
