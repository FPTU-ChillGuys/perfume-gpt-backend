using Microsoft.EntityFrameworkCore;
using PerfumeGPT.Application.DTOs.Requests.OrderReturnRequests;
using PerfumeGPT.Application.DTOs.Responses.Media;
using PerfumeGPT.Application.DTOs.Responses.OrderReturnRequests;
using PerfumeGPT.Application.Interfaces.Repositories;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Domain.Enums;
using PerfumeGPT.Persistence.Contexts;
using PerfumeGPT.Persistence.Extensions;
using PerfumeGPT.Persistence.Repositories.Commons;
using System.Linq.Expressions;

namespace PerfumeGPT.Persistence.Repositories
{
	public class OrderReturnRequestRepository : GenericRepository<OrderReturnRequest>, IOrderReturnRequestRepository
	{
		public OrderReturnRequestRepository(PerfumeDbContext context) : base(context) { }

		public async Task<(List<OrderReturnRequestResponse> Items, int TotalCount)> GetPagedResponsesAsync(GetPagedReturnRequestsRequest request)
		{
			Expression<Func<OrderReturnRequest, bool>> filter = r => true;

			if (request.Status.HasValue)
			{
				var status = request.Status.Value;
				Expression<Func<OrderReturnRequest, bool>> statusFilter = r => r.Status == status;
				filter = filter.AndAlso(statusFilter);
			}

			if (request.CustomerId.HasValue)
			{
				var customerId = request.CustomerId.Value;
				Expression<Func<OrderReturnRequest, bool>> customerFilter = r => r.CustomerId == customerId;
				filter = filter.AndAlso(customerFilter);
			}

			if (request.IsRefunded.HasValue)
			{
				var isRefunded = request.IsRefunded.Value;
				Expression<Func<OrderReturnRequest, bool>> refundedFilter = r => r.IsRefunded == isRefunded;
				filter = filter.AndAlso(refundedFilter);
			}

			var query = _context.OrderReturnRequests
				.AsNoTracking()
				.Where(filter)
				.AsQueryable();

			var totalCount = await query.CountAsync();

			var allowedSortColumns = new HashSet<string>(StringComparer.Ordinal)
			{
				nameof(OrderReturnRequest.CreatedAt),
				nameof(OrderReturnRequest.UpdatedAt),
				nameof(OrderReturnRequest.Status),
				nameof(OrderReturnRequest.RequestedRefundAmount),
				nameof(OrderReturnRequest.ApprovedRefundAmount),
				nameof(OrderReturnRequest.IsRefunded)
			};

			string? sortBy = null;
			if (!string.IsNullOrWhiteSpace(request.SortBy))
			{
				var trimmedSortBy = request.SortBy.Trim();
				sortBy = trimmedSortBy.Length == 1
					? char.ToUpper(trimmedSortBy[0]).ToString()
					: char.ToUpper(trimmedSortBy[0]) + trimmedSortBy.Substring(1);
			}

			query = !string.IsNullOrWhiteSpace(sortBy) && allowedSortColumns.Contains(sortBy)
				? query.ApplySorting(sortBy, request.IsDescending)
				: query.OrderByDescending(r => r.CreatedAt);

			// 1. Thêm Include để EF Core kéo đủ dữ liệu các bảng liên kết cho RawRequest
			var pagedData = await query
				.Include(r => r.Order)
				.Include(r => r.Customer)
				.Include(r => r.ReturnShipping)
				.Skip((request.PageNumber - 1) * request.PageSize)
				.Take(request.PageSize)
				.Select(r => new
				{
					RawRequest = r,

					// 2. Tính SUM 1 lần duy nhất dưới DB
					TotalRefundableDetailAmount = r.ReturnDetails.Sum(d => d.OrderDetail.Quantity > 0
						? (d.OrderDetail.UnitPrice - (d.OrderDetail.PromotionDiscountAmount / d.OrderDetail.Quantity) - (d.OrderDetail.ApportionedDiscount / d.OrderDetail.Quantity)) * d.RequestedQuantity
						: 0m),

					// Tên biến là ReturnDetails
					ReturnDetails = r.ReturnDetails
						.Select(d => new OrderReturnRequestDetailResponse
						{
							Id = d.Id,
							OrderDetailId = d.OrderDetailId,
							VariantId = d.OrderDetail.VariantId,
							RequestedQuantity = d.RequestedQuantity,
							UnitPrice = d.OrderDetail.UnitPrice,
							CampaignDiscount = d.OrderDetail.Quantity > 0
								? (d.OrderDetail.PromotionDiscountAmount / d.OrderDetail.Quantity) * d.RequestedQuantity
								: 0m,
							CampaignPrice = d.OrderDetail.Quantity > 0
								? (d.OrderDetail.UnitPrice - (d.OrderDetail.PromotionDiscountAmount / d.OrderDetail.Quantity)) * d.RequestedQuantity
								: 0m,
							VoucherDiscount = d.OrderDetail.Quantity > 0
								? (d.OrderDetail.ApportionedDiscount / d.OrderDetail.Quantity) * d.RequestedQuantity
								: 0m,
							RefundableAmount = d.OrderDetail.Quantity > 0
							 ? (d.OrderDetail.UnitPrice - (d.OrderDetail.PromotionDiscountAmount / d.OrderDetail.Quantity) - (d.OrderDetail.ApportionedDiscount / d.OrderDetail.Quantity)) * d.RequestedQuantity
								: 0m
						})
						.ToList(),
				})
				.AsSplitQuery()
				.ToListAsync();

			// 3. Xử lý logic trên RAM (Đã sửa x.Details thành x.ReturnDetails)
			var finalItems = pagedData.Select(x => new OrderReturnRequestResponse
			{
				Id = x.RawRequest.Id,
				OrderId = x.RawRequest.OrderId,
				OrderCode = x.RawRequest.Order.Code,
				CustomerId = x.RawRequest.CustomerId,
				CustomerEmail = x.RawRequest.Customer?.Email,
				ProcessedById = x.RawRequest.ProcessedById,
				InspectedById = x.RawRequest.InspectedById,
				Reason = x.RawRequest.Reason.ToString(),
				CustomerNote = x.RawRequest.CustomerNote,
				StaffNote = x.RawRequest.StaffNote,
				InspectionNote = x.RawRequest.InspectionNote,
				Status = x.RawRequest.Status,
				RequestedRefundAmount = x.RawRequest.RequestedRefundAmount,
				ApprovedRefundAmount = x.RawRequest.ApprovedRefundAmount,
				IsRefunded = x.RawRequest.IsRefunded,
				IsRefundOnly = x.RawRequest.IsRefundOnly,
				RefundBankName = x.RawRequest.RefundBankName,
				RefundAccountName = x.RawRequest.RefundAccountName,
				RefundAccountNumber = x.RawRequest.RefundAccountNumber,
				VnpTransactionNo = x.RawRequest.RefundTransactionReference,
				IsRestocked = x.RawRequest.IsRestocked,
				ReturnShippingInfo = x.RawRequest.ReturnShipping == null
					? null
					: new ReturnShippingInfoResponse
					{
						Id = x.RawRequest.ReturnShipping.Id,
						CarrierName = x.RawRequest.ReturnShipping.CarrierName,
						TrackingNumber = x.RawRequest.ReturnShipping.TrackingNumber,
						Type = x.RawRequest.ReturnShipping.Type,
						ShippingFee = x.RawRequest.ReturnShipping.ShippingFee,
						Status = x.RawRequest.ReturnShipping.Status,
						EstimatedDeliveryDate = x.RawRequest.ReturnShipping.EstimatedDeliveryDate,
						ShippedDate = x.RawRequest.ReturnShipping.ShippedDate
					},
				CreatedAt = x.RawRequest.CreatedAt,
				UpdatedAt = x.RawRequest.UpdatedAt,

				// Tính toán nhẹ nhàng không cần chọc DB nữa
				RefundedShippingFee = x.RawRequest.RequestedRefundAmount > x.TotalRefundableDetailAmount
					? x.RawRequest.RequestedRefundAmount - x.TotalRefundableDetailAmount
					: 0m,

				ReturnDetails = x.ReturnDetails
			}).ToList();

			return (finalItems, totalCount);
		}

		public async Task<(List<OrderReturnRequestResponse> Items, int TotalCount)> GetPagedUserResponsesAsync(Guid userId, GetPagedUserReturnRequestsRequest request)
		{
			Expression<Func<OrderReturnRequest, bool>> filter = r => r.CustomerId == userId;

			if (request.Status.HasValue)
			{
				var status = request.Status.Value;
				Expression<Func<OrderReturnRequest, bool>> statusFilter = r => r.Status == status;
				filter = filter.AndAlso(statusFilter);
			}

			if (request.IsRefunded.HasValue)
			{
				var isRefunded = request.IsRefunded.Value;
				Expression<Func<OrderReturnRequest, bool>> refundedFilter = r => r.IsRefunded == isRefunded;
				filter = filter.AndAlso(refundedFilter);
			}

			var query = _context.OrderReturnRequests
				.AsNoTracking()
				.Where(filter)
				.AsQueryable();

			var totalCount = await query.CountAsync();

			var allowedSortColumns = new HashSet<string>(StringComparer.Ordinal)
			{
				nameof(OrderReturnRequest.CreatedAt),
				nameof(OrderReturnRequest.UpdatedAt),
				nameof(OrderReturnRequest.Status),
				nameof(OrderReturnRequest.RequestedRefundAmount),
				nameof(OrderReturnRequest.ApprovedRefundAmount),
				nameof(OrderReturnRequest.IsRefunded)
			};

			string? sortBy = null;
			if (!string.IsNullOrWhiteSpace(request.SortBy))
			{
				var trimmedSortBy = request.SortBy.Trim();
				sortBy = trimmedSortBy.Length == 1
					? char.ToUpper(trimmedSortBy[0]).ToString()
					: char.ToUpper(trimmedSortBy[0]) + trimmedSortBy.Substring(1);
			}

			query = !string.IsNullOrWhiteSpace(sortBy) && allowedSortColumns.Contains(sortBy)
				? query.ApplySorting(sortBy, request.IsDescending)
				: query.OrderByDescending(r => r.CreatedAt);

			// 1. Thêm Include để EF Core kéo đủ dữ liệu các bảng liên kết cho RawRequest
			var pagedData = await query
				.Include(r => r.Order)
				.Include(r => r.Customer)
				.Include(r => r.ReturnShipping)
				.Skip((request.PageNumber - 1) * request.PageSize)
				.Take(request.PageSize)
				.Select(r => new
				{
					RawRequest = r,

					// 2. Tính SUM 1 lần duy nhất dưới DB
					TotalRefundableDetailAmount = r.ReturnDetails.Sum(d => d.OrderDetail.Quantity > 0
						? (d.OrderDetail.UnitPrice - (d.OrderDetail.PromotionDiscountAmount / d.OrderDetail.Quantity) - (d.OrderDetail.ApportionedDiscount / d.OrderDetail.Quantity)) * d.RequestedQuantity
						: 0m),

					// Tên biến là ReturnDetails
					ReturnDetails = r.ReturnDetails
						.Select(d => new OrderReturnRequestDetailResponse
						{
							Id = d.Id,
							OrderDetailId = d.OrderDetailId,
							VariantId = d.OrderDetail.VariantId,
							RequestedQuantity = d.RequestedQuantity,
							UnitPrice = d.OrderDetail.UnitPrice,
							CampaignDiscount = d.OrderDetail.Quantity > 0
								? (d.OrderDetail.PromotionDiscountAmount / d.OrderDetail.Quantity) * d.RequestedQuantity
								: 0m,
							CampaignPrice = d.OrderDetail.Quantity > 0
								? (d.OrderDetail.UnitPrice - (d.OrderDetail.PromotionDiscountAmount / d.OrderDetail.Quantity)) * d.RequestedQuantity
								: 0m,
							VoucherDiscount = d.OrderDetail.Quantity > 0
								? (d.OrderDetail.ApportionedDiscount / d.OrderDetail.Quantity) * d.RequestedQuantity
								: 0m,
							RefundableAmount = d.OrderDetail.Quantity > 0
							 ? (d.OrderDetail.UnitPrice - (d.OrderDetail.PromotionDiscountAmount / d.OrderDetail.Quantity) - (d.OrderDetail.ApportionedDiscount / d.OrderDetail.Quantity)) * d.RequestedQuantity
								: 0m
						})
						.ToList(),
				})
				.AsSplitQuery() // Tránh Cartesian Explosion
				.ToListAsync();

			// 3. Xử lý logic trên RAM (Đã sửa x.Details thành x.ReturnDetails)
			var finalItems = pagedData.Select(x => new OrderReturnRequestResponse
			{
				Id = x.RawRequest.Id,
				OrderId = x.RawRequest.OrderId,
				OrderCode = x.RawRequest.Order.Code,
				CustomerId = x.RawRequest.CustomerId,
				CustomerEmail = x.RawRequest.Customer?.Email,
				ProcessedById = x.RawRequest.ProcessedById,
				InspectedById = x.RawRequest.InspectedById,
				Reason = x.RawRequest.Reason.ToString(),
				CustomerNote = x.RawRequest.CustomerNote,
				StaffNote = x.RawRequest.StaffNote,
				InspectionNote = x.RawRequest.InspectionNote,
				Status = x.RawRequest.Status,
				RequestedRefundAmount = x.RawRequest.RequestedRefundAmount,
				ApprovedRefundAmount = x.RawRequest.ApprovedRefundAmount,
				IsRefunded = x.RawRequest.IsRefunded,
				IsRefundOnly = x.RawRequest.IsRefundOnly,
				RefundBankName = x.RawRequest.RefundBankName,
				RefundAccountName = x.RawRequest.RefundAccountName,
				RefundAccountNumber = x.RawRequest.RefundAccountNumber,
				VnpTransactionNo = x.RawRequest.RefundTransactionReference,
				IsRestocked = x.RawRequest.IsRestocked,
				ReturnShippingInfo = x.RawRequest.ReturnShipping == null
					? null
					: new ReturnShippingInfoResponse
					{
						Id = x.RawRequest.ReturnShipping.Id,
						CarrierName = x.RawRequest.ReturnShipping.CarrierName,
						TrackingNumber = x.RawRequest.ReturnShipping.TrackingNumber,
						Type = x.RawRequest.ReturnShipping.Type,
						ShippingFee = x.RawRequest.ReturnShipping.ShippingFee,
						Status = x.RawRequest.ReturnShipping.Status,
						EstimatedDeliveryDate = x.RawRequest.ReturnShipping.EstimatedDeliveryDate,
						ShippedDate = x.RawRequest.ReturnShipping.ShippedDate
					},
				CreatedAt = x.RawRequest.CreatedAt,
				UpdatedAt = x.RawRequest.UpdatedAt,

				// Tính toán nhẹ nhàng không cần chọc DB nữa
				RefundedShippingFee = x.RawRequest.RequestedRefundAmount > x.TotalRefundableDetailAmount
					? x.RawRequest.RequestedRefundAmount - x.TotalRefundableDetailAmount
					: 0m,

				ReturnDetails = x.ReturnDetails
			}).ToList();

			return (finalItems, totalCount);
		}

		public async Task<OrderReturnRequestResponse?> GetResponseByIdAsync(Guid requestId)
			=> await _context.OrderReturnRequests
				.AsNoTracking()
				.Where(r => r.Id == requestId)
				.Select(r => new OrderReturnRequestResponse
				{
					Id = r.Id,
					OrderId = r.OrderId,
					OrderCode = r.Order.Code,
					CustomerId = r.CustomerId,
					CustomerEmail = r.Customer != null ? r.Customer.Email : null,
					ProcessedById = r.ProcessedById,
					ProcessedByName = r.ProcessedBy != null ? (r.ProcessedBy.FullName ?? r.ProcessedBy.UserName) : null,
					InspectedById = r.InspectedById,
					InspectedByName = r.InspectedBy != null ? (r.InspectedBy.FullName ?? r.InspectedBy.UserName) : null,
					Reason = r.Reason.ToString(),
					CustomerNote = r.CustomerNote,
					StaffNote = r.StaffNote,
					InspectionNote = r.InspectionNote,
					Status = r.Status,
					RequestedRefundAmount = r.RequestedRefundAmount,
					RefundedShippingFee = r.RequestedRefundAmount > r.ReturnDetails.Sum(d => d.OrderDetail.Quantity > 0
						  ? (d.OrderDetail.UnitPrice - (d.OrderDetail.PromotionDiscountAmount / d.OrderDetail.Quantity) - (d.OrderDetail.ApportionedDiscount / d.OrderDetail.Quantity)) * d.RequestedQuantity
						  : 0m)
						? r.RequestedRefundAmount - r.ReturnDetails.Sum(d => d.OrderDetail.Quantity > 0
							? (d.OrderDetail.UnitPrice - (d.OrderDetail.PromotionDiscountAmount / d.OrderDetail.Quantity) - (d.OrderDetail.ApportionedDiscount / d.OrderDetail.Quantity)) * d.RequestedQuantity
							: 0m)
						: 0m,
					ApprovedRefundAmount = r.ApprovedRefundAmount,
					IsRefunded = r.IsRefunded,
					IsRefundOnly = r.IsRefundOnly,
					RefundBankName = r.RefundBankName,
					RefundAccountName = r.RefundAccountName,
					RefundAccountNumber = r.RefundAccountNumber,
					VnpTransactionNo = r.RefundTransactionReference,
					IsRestocked = r.IsRestocked,
					ReturnShippingInfo = r.ReturnShipping == null
						? null
						: new ReturnShippingInfoResponse
						{
							Id = r.ReturnShipping.Id,
							CarrierName = r.ReturnShipping.CarrierName,
							TrackingNumber = r.ReturnShipping.TrackingNumber,
							Type = r.ReturnShipping.Type,
							ShippingFee = r.ReturnShipping.ShippingFee,
							Status = r.ReturnShipping.Status,
							EstimatedDeliveryDate = r.ReturnShipping.EstimatedDeliveryDate,
							ShippedDate = r.ReturnShipping.ShippedDate
						},
					ReturnDetails = r.ReturnDetails
						.Select(d => new OrderReturnRequestDetailResponse
						{
							Id = d.Id,
							OrderDetailId = d.OrderDetailId,
							VariantId = d.OrderDetail.VariantId,
							RequestedQuantity = d.RequestedQuantity,
							UnitPrice = d.OrderDetail.UnitPrice,
							CampaignDiscount = d.OrderDetail.Quantity > 0
								? (d.OrderDetail.PromotionDiscountAmount / d.OrderDetail.Quantity) * d.RequestedQuantity
								: 0m,
							// Giá thực bán sau khi trừ Campaign
							CampaignPrice = d.OrderDetail.Quantity > 0
								? (d.OrderDetail.UnitPrice - (d.OrderDetail.PromotionDiscountAmount / d.OrderDetail.Quantity)) * d.RequestedQuantity
								: 0m,
							VoucherDiscount = d.OrderDetail.Quantity > 0
								? (d.OrderDetail.ApportionedDiscount / d.OrderDetail.Quantity) * d.RequestedQuantity
								: 0m,
							RefundableAmount = d.OrderDetail.Quantity > 0
								? (d.OrderDetail.UnitPrice - (d.OrderDetail.PromotionDiscountAmount / d.OrderDetail.Quantity) - (d.OrderDetail.ApportionedDiscount / d.OrderDetail.Quantity)) * d.RequestedQuantity
								: 0m
						})
						.ToList(),
					ProofImages = r.ProofImages
						.OrderBy(m => m.DisplayOrder)
						.Where(m => !m.IsDeleted)
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
				.AsSplitQuery()
				.FirstOrDefaultAsync();

		public async Task<OrderReturnRequest?> GetByIdWithOrderAsync(Guid requestId)
		=> await _context.OrderReturnRequests
			.Include(r => r.Order)
			.Include(r => r.ProofImages)
			.Include(r => r.ReturnShipping)
			.AsSplitQuery()
			.FirstOrDefaultAsync(r => r.Id == requestId);

		public async Task<OrderReturnRequest?> GetByIdWithPickAddressAsync(Guid requestId)
			=> await _context.OrderReturnRequests
			.Include(r => r.PickupAddress)
			.FirstOrDefaultAsync(r => r.Id == requestId);

		public async Task<OrderReturnRequest?> GetByIdWithOrderDetailsAsync(Guid requestId)
		=> await _context.OrderReturnRequests
			.Include(r => r.Order)
				.ThenInclude(o => o.OrderDetails)
			.Include(r => r.ReturnDetails)
				.ThenInclude(d => d.OrderDetail)
			.Include(r => r.ProofImages)
			.AsSplitQuery()
			.FirstOrDefaultAsync(r => r.Id == requestId);

		public async Task<List<ReturnRequestStatus>> GetStatusesByOrderIdAsync(Guid orderId)
		{
			return await _context.OrderReturnRequests
				.AsNoTracking()
				.Where(r => r.OrderId == orderId)
				.Select(r => r.Status)
				.ToListAsync();
		}
	}
}
