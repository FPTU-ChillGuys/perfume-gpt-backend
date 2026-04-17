using FluentValidation;
using PerfumeGPT.Application.DTOs.Requests.StockAdjustments;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.StockAdjustments;
using PerfumeGPT.Application.Exceptions;
using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Application.Interfaces.Services;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Domain.Enums;
using static PerfumeGPT.Domain.Entities.StockAdjustmentDetail;

namespace PerfumeGPT.Application.Services
{
	public class StockAdjustmentService : IStockAdjustmentService
	{
		#region Dependencies
		private readonly IUnitOfWork _unitOfWork;
		private readonly IBatchService _batchService;
		private readonly INotificationService _notificationService;

		public StockAdjustmentService(
			IUnitOfWork unitOfWork,
		 IBatchService batchService,
			INotificationService notificationService)
		{
			_unitOfWork = unitOfWork;
			_batchService = batchService;
			_notificationService = notificationService;
		}
		#endregion Dependencies

		public async Task<BaseResponse<string>> CreateStockAdjustmentAsync(CreateStockAdjustmentRequest request, Guid userId)
		{
			var duplicateVariants = request.AdjustmentDetails
				.GroupBy(d => d.VariantId)
				.Where(g => g.Count() > 1)
				.Select(g => g.Key)
				.ToList();

			if (duplicateVariants.Count != 0)
			{
				var duplicateIds = string.Join(", ", duplicateVariants);
				throw AppException.BadRequest($"Phát hiện ID biến thể trùng: {duplicateIds}. Mỗi biến thể chỉ được xuất hiện một lần.");
			}

			foreach (var detail in request.AdjustmentDetails)
			{
				var variant = await _unitOfWork.Variants.GetByIdAsync(detail.VariantId) ?? throw AppException.NotFound($"Không tìm thấy biến thể có ID {detail.VariantId}.");
				var batch = await _unitOfWork.Batches.GetByIdAsync(detail.BatchId) ?? throw AppException.NotFound($"Không tìm thấy lô có ID {detail.BatchId}.");

				if (batch.VariantId != detail.VariantId)
					throw AppException.BadRequest($"Lô {detail.BatchId} không thuộc biến thể {detail.VariantId}.");
			}

			var totalAdjustmentQty = request.AdjustmentDetails.Sum(d => Math.Abs(d.AdjustmentQuantity));
			bool isAutoApprove = totalAdjustmentQty <= 5; // Magic number 5 - config later
			var response = await _unitOfWork.ExecuteInTransactionAsync(async () =>
			  {
				  var stockAdjustment = StockAdjustment.Create(
						   userId,
						   request.AdjustmentDate,
						   request.Reason,
						   request.Note);

				  foreach (var detailRequest in request.AdjustmentDetails)
				  {
					  var detailPayload = new StockAdjustmentDetailPayload
					  {
						  ProductVariantId = detailRequest.VariantId,
						  BatchId = detailRequest.BatchId,
						  AdjustmentQuantity = detailRequest.AdjustmentQuantity,
						  Note = detailRequest.Note
					  };

					  if (isAutoApprove)
					  {
						  stockAdjustment.AddApprovedDetail(detailPayload, detailRequest.AdjustmentQuantity);

						  if (detailRequest.AdjustmentQuantity > 0)
						  {
							  await _batchService.IncreaseBatchQuantityAsync(detailRequest.BatchId, detailRequest.AdjustmentQuantity);
						  }
						  else if (detailRequest.AdjustmentQuantity < 0)
						  {
							  await _batchService.DecreaseBatchQuantityAsync(detailRequest.BatchId, Math.Abs(detailRequest.AdjustmentQuantity));
						  }
					  }
					  else
					  {
						  stockAdjustment.AddDetail(detailPayload);
					  }
				  }

				  if (isAutoApprove)
				  {
					  stockAdjustment.UpdateStatus(StockAdjustmentStatus.InProgress);
					  stockAdjustment.Complete(userId);
				  }

				  await _unitOfWork.StockAdjustments.AddAsync(stockAdjustment);

				  var message = isAutoApprove
					? "Tạo phiếu điều chỉnh kho và tự động duyệt thành công."
					  : "Đã tạo phiếu điều chỉnh kho và đang chờ quản lý xác minh.";

				  return BaseResponse<string>.Ok(stockAdjustment.Id.ToString(), message);
			  });

			if (!isAutoApprove && Guid.TryParse(response.Payload, out var stockAdjustmentId))
			{
				await _notificationService.SendToRoleAsync(
					UserRole.admin,
					"Yêu cầu điều chỉnh kho mới",
					$"Có phiếu điều chỉnh kho #{stockAdjustmentId} cần duyệt.",
					NotificationType.Warning,
					referenceId: stockAdjustmentId,
					referenceType: NotifiReferecneType.Adjustment);
			}

			return response;
		}

		public async Task<BaseResponse<string>> VerifyStockAdjustmentAsync(Guid adjustmentId, VerifyStockAdjustmentRequest request, Guid verifiedByUserId)
		{
			var stockAdjustment = await _unitOfWork.StockAdjustments.GetByIdWithDetailsAsync(adjustmentId) ?? throw AppException.NotFound("Không tìm thấy phiếu điều chỉnh kho.");

			stockAdjustment.EnsureVerifiable();

			if (request.AdjustmentDetails.Count != stockAdjustment.AdjustmentDetails.Count)
			{
				throw AppException.BadRequest("Số lượng chi tiết điều chỉnh dùng để xác minh không khớp.");
			}

			// Check for duplicate import detail IDs in request
			var duplicateDetailIds = request.AdjustmentDetails
				.GroupBy(d => d.DetailId)
				.Where(g => g.Count() > 1)
				.Select(g => g.Key)
				.ToList();

			if (duplicateDetailIds.Count != 0)
			{
				var duplicateIds = string.Join(", ", duplicateDetailIds);
				throw AppException.BadRequest($"Phát hiện ID chi tiết điều chỉnh trùng trong request: {duplicateIds}. Mỗi chi tiết chỉ được xuất hiện một lần.");
			}

			// Validate all adjustment details exist and match request
			foreach (var verifyDetail in request.AdjustmentDetails)
			{
				var adjustmentDetail = stockAdjustment.AdjustmentDetails.FirstOrDefault(d => d.Id == verifyDetail.DetailId)
					?? throw AppException.NotFound($"Không tìm thấy chi tiết điều chỉnh có ID {verifyDetail.DetailId}.");
			}

			// Execute within transaction
			var response = await _unitOfWork.ExecuteInTransactionAsync(async () =>
			  {
				  foreach (var verifyDetail in request.AdjustmentDetails)
				  {
					  var adjustmentDetail = stockAdjustment.AdjustmentDetails.First(d => d.Id == verifyDetail.DetailId);

					  // Update adjustment detail
					  adjustmentDetail.Approve(verifyDetail.ApprovedQuantity, verifyDetail.Note);
					  _unitOfWork.StockAdjustmentDetails.Update(adjustmentDetail);

					  // Apply stock adjustment
					  if (verifyDetail.ApprovedQuantity > 0)
					  {
						  // Update batch quantity - this will also recalculate and update stock quantity automatically
						  await _batchService.IncreaseBatchQuantityAsync(adjustmentDetail.BatchId, verifyDetail.ApprovedQuantity);
					  }
					  else if (verifyDetail.ApprovedQuantity < 0)
					  {
						  // Update batch quantity - this will also recalculate and update stock quantity automatically
						  await _batchService.DecreaseBatchQuantityAsync(adjustmentDetail.BatchId, Math.Abs(verifyDetail.ApprovedQuantity));
					  }
				  }

				  stockAdjustment.Complete(verifiedByUserId);
				  _unitOfWork.StockAdjustments.Update(stockAdjustment);

				  return BaseResponse<string>.Ok(stockAdjustment.Id.ToString(), "Xác minh phiếu điều chỉnh kho thành công.");
			  });

			await _notificationService.SendToUserAsync(
				stockAdjustment.CreatedById,
				"Yêu cầu điều chỉnh kho đã được chấp thuận",
				$"Yêu cầu điều chỉnh kho #{stockAdjustment.Id} của bạn đã được Admin chấp thuận.",
				NotificationType.Success,
				referenceId: stockAdjustment.Id,
				referenceType: NotifiReferecneType.Adjustment);

			return response;
		}

		public async Task<BaseResponse<StockAdjustmentResponse>> GetStockAdjustmentByIdAsync(Guid id)
		{
			var response = await _unitOfWork.StockAdjustments.GetByIdToViewAsync(id)
				  ?? throw AppException.NotFound("Không tìm thấy phiếu điều chỉnh kho.");

			return BaseResponse<StockAdjustmentResponse>.Ok(response, "Lấy thông tin phiếu điều chỉnh kho thành công.");
		}

		public async Task<BaseResponse<PagedResult<StockAdjustmentListItem>>> GetPagedStockAdjustmentsAsync(GetPagedStockAdjustmentsRequest request)
		{
			var (items, totalCount) = await _unitOfWork.StockAdjustments.GetPagedAsync(request);

			var pagedResult = new PagedResult<StockAdjustmentListItem>(
				items,
				request.PageNumber,
				request.PageSize,
				totalCount
			);

			return BaseResponse<PagedResult<StockAdjustmentListItem>>.Ok(pagedResult, "Lấy danh sách phiếu điều chỉnh kho thành công.");
		}

		public async Task<BaseResponse<string>> UpdateAdjustmentStatusAsync(Guid id, UpdateStockAdjustmentStatusRequest request)
		{
			var stockAdjustment = await _unitOfWork.StockAdjustments.GetByIdAsync(id) ?? throw AppException.NotFound("Không tìm thấy phiếu điều chỉnh kho.");

			if (request.Status == StockAdjustmentStatus.Cancelled)
			{
				stockAdjustment.Cancel(request.Note);
			}
			else
			{
				stockAdjustment.UpdateStatus(request.Status);
			}

			_unitOfWork.StockAdjustments.Update(stockAdjustment);
			var saved = await _unitOfWork.SaveChangesAsync();
			if (!saved) throw AppException.Internal("Cập nhật trạng thái phiếu điều chỉnh kho thất bại.");

			if (request.Status == StockAdjustmentStatus.Cancelled)
			{
				await _notificationService.SendToUserAsync(
					stockAdjustment.CreatedById,
					"Yêu cầu điều chỉnh kho đã bị từ chối",
					$"Yêu cầu điều chỉnh kho #{stockAdjustment.Id} của bạn đã bị từ chối.",
					NotificationType.Warning,
					referenceId: stockAdjustment.Id,
					referenceType: NotifiReferecneType.Adjustment);
			}

			return BaseResponse<string>.Ok(id.ToString(), "Cập nhật trạng thái phiếu điều chỉnh kho thành công.");
		}

		public async Task<BaseResponse<bool>> DeleteStockAdjustmentAsync(Guid id)
		{
			var stockAdjustment = await _unitOfWork.StockAdjustments.GetByIdWithDetailsAsync(id)
				  ?? throw AppException.NotFound("Không tìm thấy phiếu điều chỉnh kho.");
			stockAdjustment.EnsureIsPending();

			return await _unitOfWork.ExecuteInTransactionAsync(async () =>
			{
				foreach (var detail in stockAdjustment.AdjustmentDetails)
				{
					_unitOfWork.StockAdjustmentDetails.Remove(detail);
				}

				_unitOfWork.StockAdjustments.Remove(stockAdjustment);

				return BaseResponse<bool>.Ok(true, "Xóa phiếu điều chỉnh kho thành công.");
			});
		}
	}
}
