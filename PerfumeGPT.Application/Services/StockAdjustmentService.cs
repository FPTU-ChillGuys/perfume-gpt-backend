using FluentValidation;
using Microsoft.Extensions.Logging;
using PerfumeGPT.Application.DTOs.Requests.StockAdjustments;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.StockAdjustments;
using PerfumeGPT.Application.Exceptions;
using PerfumeGPT.Application.Extensions;
using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Application.Interfaces.Services;
using PerfumeGPT.Application.Interfaces.ThirdParties;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Domain.Enums;
using static PerfumeGPT.Domain.Entities.StockAdjustmentDetail;

namespace PerfumeGPT.Application.Services
{
	public class StockAdjustmentService : IStockAdjustmentService
	{
		#region Dependencies
		private readonly IUnitOfWork _unitOfWork;
		private readonly IBackgroundJobService _backgroundJobService;
		private readonly ILogger<StockAdjustmentService> _logger;

		public StockAdjustmentService(
			IUnitOfWork unitOfWork,
			IBackgroundJobService backgroundJobService,
			ILogger<StockAdjustmentService> logger)
		{
			_unitOfWork = unitOfWork;
			_backgroundJobService = backgroundJobService;
			_logger = logger;
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

			// ==============================================================
			// BƯỚC 1: BULK READ - GOM IDs VÀ KÉO LÊN RAM ĐỂ XÁC THỰC
			// ==============================================================
			var variantIds = request.AdjustmentDetails.Select(d => d.VariantId).Distinct().ToList();
			var batchIds = request.AdjustmentDetails.Select(d => d.BatchId).Distinct().ToList();

			var variants = await _unitOfWork.Variants.GetAllAsync(v => variantIds.Contains(v.Id));
			var variantDict = variants.ToDictionary(v => v.Id);

			var batches = await _unitOfWork.Batches.GetAllAsync(b => batchIds.Contains(b.Id));
			var batchDict = batches.ToDictionary(b => b.Id);

			// Xác thực trên RAM (Không tốn câu Query nào thêm)
			foreach (var detail in request.AdjustmentDetails)
			{
				if (!variantDict.ContainsKey(detail.VariantId))
					throw AppException.NotFound($"Không tìm thấy biến thể có ID {detail.VariantId}.");

				if (!batchDict.TryGetValue(detail.BatchId, out var batch))
					throw AppException.NotFound($"Không tìm thấy lô có ID {detail.BatchId}.");

				if (batch.VariantId != detail.VariantId)
					throw AppException.BadRequest($"Lô {detail.BatchId} không thuộc biến thể {detail.VariantId}.");
			}

			var totalAdjustmentQty = request.AdjustmentDetails.Sum(d => Math.Abs(d.AdjustmentQuantity));
			var storePolicy = await _unitOfWork.StorePolicies.GetCurrentPolicyAsync();
			bool isAutoApprove = totalAdjustmentQty <= storePolicy?.StockAdjustmentAutoApprovalThreshold;

			// ==============================================================
			// BƯỚC 2: BULK READ STOCK (CHỈ KHI CẦN AUTO-APPROVE)
			// ==============================================================
			Dictionary<Guid, Stock> stockDict = [];
			if (isAutoApprove)
			{
				var stocks = await _unitOfWork.Stocks.GetAllAsync(s => variantIds.Contains(s.VariantId));
				stockDict = stocks.ToDictionary(s => s.VariantId);
			}
			var response = await _unitOfWork.ExecuteInTransactionAsync(async () =>
			{
				var stockAdjustment = StockAdjustment.Create(
					userId,
					request.AdjustmentDate,
					request.Reason,
					request.Note);

				// ==============================================================
				// BƯỚC 3: XỬ LÝ LOGIC TRÊN RAM (IN-MEMORY PROCESSING)
				// ==============================================================
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

						var batch = batchDict[detailRequest.BatchId];
						if (!stockDict.TryGetValue(detailRequest.VariantId, out var stock))
							throw AppException.NotFound($"Không tìm thấy tồn kho cho biến thể {detailRequest.VariantId}");

						// Cập nhật số lượng vật lý trên RAM
						if (detailRequest.AdjustmentQuantity > 0)
						{
							batch.IncreaseQuantity(detailRequest.AdjustmentQuantity, StockTransactionType.Adjustment, stockAdjustment.Id, userId, detailRequest.Note);
							stock.Increase(detailRequest.AdjustmentQuantity);
						}
						else if (detailRequest.AdjustmentQuantity < 0)
						{
							var absQty = Math.Abs(detailRequest.AdjustmentQuantity);
							batch.DecreaseQuantity(absQty, StockTransactionType.Adjustment, stockAdjustment.Id, userId, detailRequest.Note);
							stock.Decrease(absQty);
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

					_unitOfWork.Batches.UpdateRange([.. batchDict.Values]);
					_unitOfWork.Stocks.UpdateRange([.. stockDict.Values]);
				}

				await _unitOfWork.StockAdjustments.AddAsync(stockAdjustment);

				var message = isAutoApprove
					? "Tạo phiếu điều chỉnh kho và tự động duyệt thành công."
					: "Đã tạo phiếu điều chỉnh kho và đang chờ quản lý xác minh.";

				return BaseResponse<string>.Ok(stockAdjustment.Id.ToString(), message);
			});

			if (!isAutoApprove && Guid.TryParse(response.Payload, out var stockAdjustmentId))
			{
				_backgroundJobService.EnqueueRoleNotification(
					_logger,
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
			var stockAdjustment = await _unitOfWork.StockAdjustments.GetByIdWithDetailsAsync(adjustmentId)
				?? throw AppException.NotFound("Không tìm thấy phiếu điều chỉnh kho.");

			stockAdjustment.EnsureVerifiable();

			if (request.AdjustmentDetails.Count != stockAdjustment.AdjustmentDetails.Count)
				throw AppException.BadRequest("Số lượng chi tiết điều chỉnh dùng để xác minh không khớp.");

			var duplicateDetailIds = request.AdjustmentDetails
				.GroupBy(d => d.DetailId)
				.Where(g => g.Count() > 1)
				.Select(g => g.Key)
				.ToList();

			if (duplicateDetailIds.Count != 0)
				throw AppException.BadRequest($"Phát hiện ID chi tiết trùng: {string.Join(", ", duplicateDetailIds)}.");

			// ==============================================================
			// BƯỚC 1: BULK READ - GOM TẤT CẢ LÊN RAM BẰNG 1 CÂU QUERY
			// ==============================================================
			var requestDict = request.AdjustmentDetails.ToDictionary(r => r.DetailId);
			var batchIds = stockAdjustment.AdjustmentDetails.Select(d => d.BatchId).Distinct().ToList();
			var variantIds = stockAdjustment.AdjustmentDetails.Select(d => d.ProductVariantId).Distinct().ToList();

			var batches = await _unitOfWork.Batches.GetAllAsync(b => batchIds.Contains(b.Id));
			var batchDict = batches.ToDictionary(b => b.Id);

			var stocks = await _unitOfWork.Stocks.GetAllAsync(s => variantIds.Contains(s.VariantId));
			var stockDict = stocks.ToDictionary(s => s.VariantId);

			// ==============================================================
			// BƯỚC 2: IN-MEMORY PROCESSING - TÍNH TOÁN TRÊN RAM
			// ==============================================================
			var response = await _unitOfWork.ExecuteInTransactionAsync(async () =>
			{
				foreach (var adjustmentDetail in stockAdjustment.AdjustmentDetails)
				{
					if (!requestDict.TryGetValue(adjustmentDetail.Id, out var verifyDetail))
						throw AppException.NotFound($"Không tìm thấy chi tiết điều chỉnh có ID {adjustmentDetail.Id}.");

					adjustmentDetail.Approve(verifyDetail.ApprovedQuantity, verifyDetail.Note);
					_unitOfWork.StockAdjustmentDetails.Update(adjustmentDetail); // Tracking update

					// KHÔNG GỌI SANG BATCH SERVICE NỮA! Lấy dữ liệu trên RAM ra xử lý:
					if (!batchDict.TryGetValue(adjustmentDetail.BatchId, out var batch))
						throw AppException.NotFound($"Không tìm thấy lô {adjustmentDetail.BatchId}");

					if (!stockDict.TryGetValue(adjustmentDetail.ProductVariantId, out var stock))
						throw AppException.NotFound($"Không tìm thấy tồn kho cho biến thể {adjustmentDetail.ProductVariantId}");

					// Tự thay đổi trên RAM
					if (verifyDetail.ApprovedQuantity > 0)
					{
						batch.IncreaseQuantity(verifyDetail.ApprovedQuantity, StockTransactionType.Adjustment, adjustmentId, verifiedByUserId, verifyDetail.Note);
						stock.Increase(verifyDetail.ApprovedQuantity);
					}
					else if (verifyDetail.ApprovedQuantity < 0)
					{
						var absQty = Math.Abs(verifyDetail.ApprovedQuantity);
						batch.DecreaseQuantity(absQty, StockTransactionType.Adjustment, adjustmentId, verifiedByUserId, verifyDetail.Note);
						stock.Decrease(absQty);
					}
				}

				stockAdjustment.Complete(verifiedByUserId);
				_unitOfWork.StockAdjustments.Update(stockAdjustment);

				// ==============================================================
				// BƯỚC 3: BULK WRITE - ĐẨY XUỐNG DB 1 LẦN
				// ==============================================================
				_unitOfWork.Batches.UpdateRange([.. batches]);
				_unitOfWork.Stocks.UpdateRange([.. stocks]);

				return BaseResponse<string>.Ok(stockAdjustment.Id.ToString(), "Xác minh phiếu điều chỉnh kho thành công.");
			});

			_backgroundJobService.EnqueueStaffNotificationWithFcm(
				_logger,
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
				_backgroundJobService.EnqueueStaffNotificationWithFcm(
					_logger,
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
				_unitOfWork.StockAdjustmentDetails.RemoveRange(stockAdjustment.AdjustmentDetails);

				_unitOfWork.StockAdjustments.Remove(stockAdjustment);

				return BaseResponse<bool>.Ok(true, "Xóa phiếu điều chỉnh kho thành công.");
			});
		}
	}
}
