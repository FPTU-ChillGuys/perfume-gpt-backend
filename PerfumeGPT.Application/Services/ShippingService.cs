using Microsoft.Extensions.Logging;
using PerfumeGPT.Application.DTOs.Requests.Shippings;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.GHNs;
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
		private readonly IReturnWorkflowService _returnWorkflowService;
		private readonly IGHNService _ghnService;
		private readonly ILogger<ShippingService> _logger;

		public ShippingService(IUnitOfWork unitOfWork, IGHNService ghnService, ILogger<ShippingService> logger, IOrderWorkflowService orderWorkflowService, IReturnWorkflowService returnWorkflowService)
		{
			_unitOfWork = unitOfWork;
			_ghnService = ghnService;
			_logger = logger;
			_orderWorkflowService = orderWorkflowService;
			_returnWorkflowService = returnWorkflowService;
		}

		public async Task<BaseResponse<PagedResult<ShippingInfoListItem>>> GetPagedShippingInfosByUserIdAsync(Guid userId, GetPagedShippingsRequest request)
		{
			if (userId == Guid.Empty)
				return BaseResponse<PagedResult<ShippingInfoListItem>>.Fail("Bắt buộc cung cấp User ID.", ResponseErrorType.BadRequest);

			var (items, totalCount) = await _unitOfWork.ShippingInfos.GetPagedByUserIdAsync(userId, request);

			var pagedResult = new PagedResult<ShippingInfoListItem>(
				items,
				request.PageNumber,
				request.PageSize,
				totalCount);

			return BaseResponse<PagedResult<ShippingInfoListItem>>.Ok(pagedResult, "Lấy danh sách thông tin vận chuyển thành công.");
		}

		public async Task<BaseResponse<string>> SyncShippingStatusByUserIdAsync(Guid userId)
		{
			if (userId == Guid.Empty)
				return BaseResponse<string>.Fail("Bắt buộc cung cấp User ID.", ResponseErrorType.BadRequest);

			var candidates = await _unitOfWork.ShippingInfos.GetSyncCandidatesWithOrdersForGhnByUserIdAsync(userId);
			if (candidates.Count == 0)
				return BaseResponse<string>.Ok("Trạng thái vận chuyển đã được cập nhật. Không có đơn hàng chờ xử lý.");

			var updatedCount = 0;

			foreach (var candidate in candidates)
			{
				var isUpdated = await SyncSingleShippingInfoAsync(candidate.Shipping, candidate.ForwardOrder, candidate.ReturnRequest);

				if (isUpdated)
				{
					updatedCount++;
				}

				await Task.Delay(200);
			}

			return BaseResponse<string>.Ok($"Đồng bộ trạng thái vận chuyển hoàn tất. Đã cập nhật {updatedCount} bản ghi.");
		}

		public async Task<bool> SyncSingleShippingInfoAsync(ShippingInfo shippingInfo, Order? forwardOrder = null, OrderReturnRequest? returnRequest = null)
		{
			try
			{
				var latestDetail = await _ghnService.GetOrderDetailAsync(shippingInfo.TrackingNumber!);
				if (latestDetail == null || string.IsNullOrWhiteSpace(latestDetail.Status)) return false;

				var targetStatus = MapGhnStatusToDomainStatus(latestDetail.Status);
				if (!targetStatus.HasValue) return false;

				var statusUpdatedAtUtc = ResolveLatestStatusUpdatedAtUtc(latestDetail);
				return await ApplyShippingStatusAsync(shippingInfo, targetStatus.Value, forwardOrder, returnRequest, statusUpdatedAtUtc);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to sync GHN tracking {TrackingNumber}", shippingInfo.TrackingNumber);
			}
			return false;
		}

		private async Task<bool> ApplyShippingStatusAsync(ShippingInfo shippingInfo, ShippingStatus targetStatus, Order? forwardOrder, OrderReturnRequest? returnRequest, DateTime? statusUpdatedAtUtc)
		{
			try
			{
				if (!TryApplyShippingStatus(shippingInfo, targetStatus)) return false;

				if (forwardOrder != null)
				{
					// Xử lý đơn hàng đi (Forward)
					await _orderWorkflowService.ProcessForwardShippingStatusAsync(forwardOrder, targetStatus, statusUpdatedAtUtc);
					_unitOfWork.Orders.Update(forwardOrder);
				}
				else if (returnRequest != null)
				{
					// Xử lý đơn hàng về (Return)
					var returnOrder = returnRequest.Order;
					await _returnWorkflowService.ProcessReturnShippingStatusAsync(returnOrder, returnRequest, targetStatus);
					_unitOfWork.Orders.Update(returnOrder);
					_unitOfWork.OrderReturnRequests.Update(returnRequest);
				}
				else
				{
					return false;
				}

				_unitOfWork.ShippingInfos.Update(shippingInfo);
				await _unitOfWork.SaveChangesAsync();
				return true;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to apply shipping status for GHN tracking {TrackingNumber}", shippingInfo.TrackingNumber);
				return false;
			}
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
				"damage" => ShippingStatus.Damaged,
				"lost" => ShippingStatus.Lost,
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
				case ShippingStatus.Damaged:
				case ShippingStatus.Lost:
					if (shippingInfo.Status != ShippingStatus.Delivered)
					{
						shippingInfo.MarkAsCarrierIncident(targetStatus);
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

		public async Task<BaseResponse<string>> SyncShippingStatusByTrackingNumberAsync(string trackingNumber)
		{
			var normalizedTrackingNumber = trackingNumber.Trim();
			var candidate = await _unitOfWork.ShippingInfos.GetSyncCandidateWithOrdersByTrackingNumberAsync(normalizedTrackingNumber)
				?? throw new Exception($"Không tìm thấy thông tin vận chuyển GHN với mã theo dõi {normalizedTrackingNumber}");

			await SyncSingleShippingInfoAsync(candidate.Shipping, candidate.ForwardOrder, candidate.ReturnRequest);
			return BaseResponse<string>.Ok($"Đã đồng bộ trạng thái vận chuyển cho mã theo dõi {trackingNumber} thành công.");
		}

		private static DateTime? ResolveLatestStatusUpdatedAtUtc(ShippingOrderDetailDto latestDetail)
		{
			if (latestDetail.Log == null || latestDetail.Log.Count == 0 || string.IsNullOrWhiteSpace(latestDetail.Status))
			{
				return latestDetail.OrderDate;
			}

			var normalizedStatus = latestDetail.Status.Trim();
			return latestDetail.Log
				.Where(log => string.Equals(log.Status, normalizedStatus, StringComparison.OrdinalIgnoreCase) && log.UpdatedDate.HasValue)
				.OrderByDescending(log => log.UpdatedDate)
				.Select(log => log.UpdatedDate)
				.FirstOrDefault()
				?? latestDetail.OrderDate;
		}
	}
}
