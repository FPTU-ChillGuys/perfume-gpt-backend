using Microsoft.EntityFrameworkCore;
using PerfumeGPT.Application.DTOs.Requests.Shippings;
using PerfumeGPT.Application.DTOs.Responses.Shippings;
using PerfumeGPT.Application.Interfaces.Repositories;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Domain.Enums;
using PerfumeGPT.Persistence.Contexts;
using PerfumeGPT.Persistence.Extensions;
using PerfumeGPT.Persistence.Repositories.Commons;

namespace PerfumeGPT.Persistence.Repositories
{
	public class ShippingInfoRepository : GenericRepository<ShippingInfo>, IShippingInfoRepository
	{
		public ShippingInfoRepository(PerfumeDbContext context) : base(context) { }

		public async Task<(List<ShippingInfoListItem> Items, int TotalCount)> GetPagedByUserIdAsync(Guid userId, GetPagedShippingsRequest request)
		{
			var forwardShippingQuery = _context.Orders
					 .Where(o => o.CustomerId == userId && o.ForwardShipping != null)
					 .Select(o => new ShippingInfoListItem
					 {
						 Id = o.ForwardShipping!.Id,
						 OrderId = o.Id,
						 CarrierName = o.ForwardShipping.CarrierName,
						 TrackingNumber = o.ForwardShipping.TrackingNumber,
						 Type = o.ForwardShipping.Type,
						 ShippingFee = o.ForwardShipping.ShippingFee,
						 Status = o.ForwardShipping.Status,
						 LeadTime = o.ForwardShipping.EstimatedDeliveryDate,
						 ShippedDate = o.ForwardShipping.ShippedDate
					 });

			var returnShippingQuery = _context.OrderReturnRequests
				.Where(r => r.CustomerId == userId && r.ReturnShipping != null)
				.Select(r => new ShippingInfoListItem
				{
					Id = r.ReturnShipping!.Id,
					OrderId = r.OrderId,
					CarrierName = r.ReturnShipping.CarrierName,
					TrackingNumber = r.ReturnShipping.TrackingNumber,
					Type = r.ReturnShipping.Type,
					ShippingFee = r.ReturnShipping.ShippingFee,
					Status = r.ReturnShipping.Status,
					LeadTime = r.ReturnShipping.EstimatedDeliveryDate,
					ShippedDate = r.ReturnShipping.ShippedDate
				});

			var query = forwardShippingQuery.Concat(returnShippingQuery).AsQueryable();

			if (request.Status.HasValue)
			{
				query = query.Where(si => si.Status == request.Status.Value);
			}

			if (request.CarrierName.HasValue)
			{
				query = query.Where(si => si.CarrierName == request.CarrierName.Value);
			}

			if (request.OrderId.HasValue)
			{
				query = query.Where(si => si.OrderId == request.OrderId.Value);
			}

			if (request.ShippingType.HasValue)
			{
				query = query.Where(si => si.Type == request.ShippingType.Value);
			}

			if (!string.IsNullOrWhiteSpace(request.TrackingNumber))
			{
				var trackingNumber = request.TrackingNumber.Trim();
				query = query.Where(si => si.TrackingNumber != null && si.TrackingNumber.Contains(trackingNumber));
			}

			var totalCount = await query.CountAsync();

			var items = await query
			 .ApplySorting(request.SortBy ?? nameof(ShippingInfoListItem.ShippedDate), request.IsDescending)
				.Skip((request.PageNumber - 1) * request.PageSize)
				.Take(request.PageSize)
				.ToListAsync();

			return (items, totalCount);
		}

		public async Task<ShippingInfo?> GetByOrderIdAsync(Guid orderId)
	   => await _context.Orders
			.Where(o => o.Id == orderId)
			.Select(o => o.ForwardShipping)
			.FirstOrDefaultAsync();

		public async Task<List<ShippingInfo>> GetSyncCandidatesForGhnAsync()
		{
			var forwardShippingCandidates = _context.Orders
				.Where(o => o.ForwardShipping != null
					&& o.ForwardShipping.CarrierName == CarrierName.GHN
					&& !string.IsNullOrWhiteSpace(o.ForwardShipping.TrackingNumber)
					&& o.ForwardShipping.Status != ShippingStatus.Cancelled
					&& o.ForwardShipping.Status != ShippingStatus.Delivered
					&& o.ForwardShipping.Status != ShippingStatus.Returned)
				.Select(o => o.ForwardShipping!);

			var returnShippingCandidates = _context.OrderReturnRequests
				.Where(r => r.ReturnShipping != null
					&& r.ReturnShipping.CarrierName == CarrierName.GHN
					&& !string.IsNullOrWhiteSpace(r.ReturnShipping.TrackingNumber)
					&& r.ReturnShipping.Status != ShippingStatus.Cancelled
					&& r.ReturnShipping.Status != ShippingStatus.Delivered
					&& r.ReturnShipping.Status != ShippingStatus.Returned)
				.Select(r => r.ReturnShipping!);

			return await forwardShippingCandidates
				.Concat(returnShippingCandidates)
				.ToListAsync();
		}

		public async Task<List<ShippingInfo>> GetSyncCandidatesForGhnByUserIdAsync(Guid userId)
		{
			var forwardShippingCandidates = _context.Orders
				.Where(o => o.CustomerId == userId
					&& o.ForwardShipping != null
					&& o.ForwardShipping.CarrierName == CarrierName.GHN
					&& !string.IsNullOrWhiteSpace(o.ForwardShipping.TrackingNumber)
					&& o.ForwardShipping.Status != ShippingStatus.Cancelled
					&& o.ForwardShipping.Status != ShippingStatus.Delivered
					&& o.ForwardShipping.Status != ShippingStatus.Returned)
				.Select(o => o.ForwardShipping!);

			var returnShippingCandidates = _context.OrderReturnRequests
				.Where(r => r.CustomerId == userId
					&& r.ReturnShipping != null
					&& r.ReturnShipping.CarrierName == CarrierName.GHN
					&& !string.IsNullOrWhiteSpace(r.ReturnShipping.TrackingNumber)
					&& r.ReturnShipping.Status != ShippingStatus.Cancelled
					&& r.ReturnShipping.Status != ShippingStatus.Delivered
					&& r.ReturnShipping.Status != ShippingStatus.Returned)
				.Select(r => r.ReturnShipping!);

			return await forwardShippingCandidates
				.Concat(returnShippingCandidates)
				.ToListAsync();
		}
	}
}
