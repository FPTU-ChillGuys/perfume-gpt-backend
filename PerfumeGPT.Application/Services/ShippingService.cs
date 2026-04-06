using Microsoft.Extensions.Logging;
using PerfumeGPT.Application.DTOs.Requests.Shippings;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.Shippings;
using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Application.Interfaces.Services;
using PerfumeGPT.Application.Interfaces.ThirdParties;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.Services
{
	public class ShippingService : IShippingService
	{
		private readonly IUnitOfWork _unitOfWork;
		private readonly IOrderWorkflowService _orderWorkflowService;
		private readonly IGHNService _ghnService;
		private readonly ILogger<ShippingService> _logger;

		public ShippingService(IUnitOfWork unitOfWork, IGHNService ghnService, ILogger<ShippingService> logger, IOrderWorkflowService orderWorkflowService)
		{
			_unitOfWork = unitOfWork;
			_ghnService = ghnService;
			_logger = logger;
			_orderWorkflowService = orderWorkflowService;
		}

		public async Task<BaseResponse<PagedResult<ShippingInfoListItem>>> GetPagedShippingInfosByUserIdAsync(Guid userId, GetPagedShippingsRequest request)
		{
			if (userId == Guid.Empty)
				return BaseResponse<PagedResult<ShippingInfoListItem>>.Fail("User ID is required.", ResponseErrorType.BadRequest);

			var (items, totalCount) = await _unitOfWork.ShippingInfos.GetPagedByUserIdAsync(userId, request);

			var pagedResult = new PagedResult<ShippingInfoListItem>(
				items,
				request.PageNumber,
				request.PageSize,
				totalCount);

			return BaseResponse<PagedResult<ShippingInfoListItem>>.Ok(pagedResult, "Shipping infos retrieved successfully.");
		}

		public async Task<BaseResponse<string>> SyncShippingStatusByUserIdAsync(Guid userId)
		{
			if (userId == Guid.Empty)
				return BaseResponse<string>.Fail("User ID is required.", ResponseErrorType.BadRequest);

			var candidates = await _unitOfWork.ShippingInfos.GetSyncCandidatesForGhnByUserIdAsync(userId);
			if (candidates.Count == 0)
				return BaseResponse<string>.Ok("Shipping status is up to date. No pending orders found.");

			var updatedCount = 0;

			foreach (var shippingInfo in candidates)
			{
				var isUpdated = await SyncSingleShippingInfoAsync(shippingInfo);

				if (isUpdated)
				{
					updatedCount++;
				}

				await Task.Delay(200);
			}

			if (updatedCount > 0)
			{
				await _unitOfWork.SaveChangesAsync();
			}

			return BaseResponse<string>.Ok($"Shipping status sync completed. Updated {updatedCount} record(s).");
		}

		public async Task<bool> SyncSingleShippingInfoAsync(ShippingInfo shippingInfo)
		{
			Guid? orderId = null;
			Guid? returnRequestId = null;

			try
			{
				var latestDetail = await _ghnService.GetOrderDetailAsync(shippingInfo.TrackingNumber!);
				if (latestDetail == null || string.IsNullOrWhiteSpace(latestDetail.Status))
					return false;

				var targetStatus = MapGhnStatusToDomainStatus(latestDetail.Status);
				if (!targetStatus.HasValue)
					return false;

				if (TryApplyShippingStatus(shippingInfo, targetStatus.Value))
				{
					var order = await _unitOfWork.Orders.FirstOrDefaultAsync(o => o.ForwardShippingId == shippingInfo.Id);
					if (order != null)
					{
						orderId = order.Id;
						await _orderWorkflowService.ProcessShippingStatusChangeAsync(order, targetStatus.Value);
					}
					else
					{
						var returnRequest = await _unitOfWork.OrderReturnRequests.FirstOrDefaultAsync(r => r.ReturnShippingId == shippingInfo.Id);
						if (returnRequest == null)
						{
							return false;
						}

						returnRequestId = returnRequest.Id;
					}

					_unitOfWork.ShippingInfos.Update(shippingInfo);

					return true;
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to sync GHN tracking {TrackingNumber} for Order {OrderId} or ReturnRequest {ReturnRequestId}", shippingInfo.TrackingNumber, orderId, returnRequestId);
			}

			return false;
		}

		public ShippingStatus? MapGhnStatusToDomainStatus(string ghnStatus)
		{
			var normalized = ghnStatus.Trim().ToLowerInvariant();

			return normalized switch
			{
				"ready_to_pick" => ShippingStatus.ReadyToPick,
				"picking" or "picked" or "storing" or "sorting" or "transporting" or "delivering" or "money_collect_picking" or "money_collect_delivering" => ShippingStatus.Delivering,
				"delivered" => ShippingStatus.Delivered,
				"cancel" => ShippingStatus.Cancelled,
				"returning" or "return" or "return_transporting" or "return_sorting" or "waiting_to_return" or "delivery_fail" or "return_fail" => ShippingStatus.Returning,
				"returned" => ShippingStatus.Returned,
				_ => null
			};
		}

		public bool TryApplyShippingStatus(ShippingInfo shippingInfo, ShippingStatus targetStatus)
		{
			if (shippingInfo.Status == targetStatus)
			{
				return false;
			}

			switch (targetStatus)
			{
				case ShippingStatus.Delivering:
					if (shippingInfo.Status == ShippingStatus.ReadyToPick)
					{
						shippingInfo.MarkAsDelivering();
						return true;
					}
					break;
				case ShippingStatus.Delivered:
					if (shippingInfo.Status == ShippingStatus.ReadyToPick)
					{
						shippingInfo.MarkAsDelivering();
						shippingInfo.MarkAsDelivered();
						return true;
					}

					if (shippingInfo.Status == ShippingStatus.Delivering)
					{
						shippingInfo.MarkAsDelivered();
						return true;
					}
					break;
				case ShippingStatus.Cancelled:
					if (shippingInfo.Status != ShippingStatus.Delivered)
					{
						shippingInfo.Cancel();
						return true;
					}
					break;
				case ShippingStatus.Returning:
					if (shippingInfo.Status == ShippingStatus.Delivering)
					{
						shippingInfo.MarkAsReturning();
						return true;
					}
					break;
				case ShippingStatus.Returned:
					if (shippingInfo.Status == ShippingStatus.Returning ||
						shippingInfo.Status == ShippingStatus.Delivering ||
						shippingInfo.Status == ShippingStatus.ReadyToPick)
					{
						if (shippingInfo.Status != ShippingStatus.Returning)
						{
							shippingInfo.MarkAsReturning();
						}
						shippingInfo.MarkAsReturned();
						return true;
					}
					break;
			}

			return false;
		}
	}
}
