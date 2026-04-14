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
					OrderCode = r.Order.Code,
					CustomerId = r.CustomerId,
					CustomerEmail = r.Customer != null ? r.Customer.Email : null,
					ProcessedById = r.ProcessedById,
					InspectedById = r.InspectedById,
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
							VoucherDiscount = d.OrderDetail.Quantity > 0
								? (d.OrderDetail.ApportionedDiscount / d.OrderDetail.Quantity) * d.RequestedQuantity
								: 0m,
							RefundableAmount = d.OrderDetail.Quantity > 0
							 ? (d.OrderDetail.UnitPrice - (d.OrderDetail.PromotionDiscountAmount / d.OrderDetail.Quantity) - (d.OrderDetail.ApportionedDiscount / d.OrderDetail.Quantity)) * d.RequestedQuantity
								: 0m
						})
						.ToList(),
					CreatedAt = r.CreatedAt,
					UpdatedAt = r.UpdatedAt
				})
				.AsSingleQuery()
				.ToListAsync();

			return (pagedData, totalCount);
		}

		public async Task<(List<OrderReturnRequestResponse> Items, int TotalCount)> GetPagedUserResponsesAsync(Guid userId, GetPagedUserReturnRequestsRequest request)
		{
			var query = _context.OrderReturnRequests
				.Where(r => r.CustomerId == userId)
				.AsNoTracking()
				.AsQueryable();

			if (request.Status.HasValue)
				query = query.Where(r => r.Status == request.Status.Value);

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
					OrderCode = r.Order.Code,
					CustomerId = r.CustomerId,
					CustomerEmail = r.Customer != null ? r.Customer.Email : null,
					ProcessedById = r.ProcessedById,
					InspectedById = r.InspectedById,
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
							VoucherDiscount = d.OrderDetail.Quantity > 0
								? (d.OrderDetail.ApportionedDiscount / d.OrderDetail.Quantity) * d.RequestedQuantity
								: 0m,
							RefundableAmount = d.OrderDetail.Quantity > 0
							 ? (d.OrderDetail.UnitPrice - (d.OrderDetail.PromotionDiscountAmount / d.OrderDetail.Quantity) - (d.OrderDetail.ApportionedDiscount / d.OrderDetail.Quantity)) * d.RequestedQuantity
								: 0m
						})
						.ToList(),
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
	}
}
