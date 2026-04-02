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
			var query = _context.ShippingInfos
				.Where(si => si.Order.CustomerId == userId)
				.AsQueryable();

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

			if (!string.IsNullOrWhiteSpace(request.TrackingNumber))
			{
				var trackingNumber = request.TrackingNumber.Trim();
				query = query.Where(si => si.TrackingNumber != null && si.TrackingNumber.Contains(trackingNumber));
			}

			var totalCount = await query.CountAsync();

			var items = await query
				.ApplySorting(request.SortBy ?? nameof(ShippingInfo.ShippedDate), request.IsDescending)
				.Skip((request.PageNumber - 1) * request.PageSize)
				.Take(request.PageSize)
				.Select(si => new ShippingInfoListItem
				{
					Id = si.Id,
					OrderId = si.OrderId,
					CarrierName = si.CarrierName,
					TrackingNumber = si.TrackingNumber,
					ShippingFee = si.ShippingFee,
					Status = si.Status,
					LeadTime = si.LeadTime,
					ShippedDate = si.ShippedDate
				})
				.ToListAsync();

			return (items, totalCount);
		}

		public async Task<ShippingInfo?> GetByOrderIdAsync(Guid orderId)
		=> await _context.ShippingInfos.FirstOrDefaultAsync(si => si.OrderId == orderId);

		public async Task<List<ShippingInfo>> GetSyncCandidatesForGhnAsync()
		=> await _context.ShippingInfos
				.Include(si => si.Order)
				.Where(si => si.CarrierName == CarrierName.GHN
					&& !string.IsNullOrWhiteSpace(si.TrackingNumber)
					&& si.Status != ShippingStatus.Cancelled
					&& si.Status != ShippingStatus.Delivered
					&& si.Status != ShippingStatus.Returned)
				.ToListAsync();

		public async Task<List<ShippingInfo>> GetSyncCandidatesForGhnByUserIdAsync(Guid userId)
		=> await _context.ShippingInfos
				.Include(si => si.Order)
				.Where(si => si.Order.CustomerId == userId
					&& si.CarrierName == CarrierName.GHN
					&& !string.IsNullOrWhiteSpace(si.TrackingNumber)
					&& si.Status != ShippingStatus.Cancelled
					&& si.Status != ShippingStatus.Delivered
					&& si.Status != ShippingStatus.Returned)
				.ToListAsync();
	}
}
