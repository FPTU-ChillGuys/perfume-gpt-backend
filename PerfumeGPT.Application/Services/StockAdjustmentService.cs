using FluentValidation;
using PerfumeGPT.Application.DTOs.Requests.StockAdjustments;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.StockAdjustments;
using PerfumeGPT.Application.Exceptions;
using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Application.Interfaces.Services;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.Services
{
	public class StockAdjustmentService : IStockAdjustmentService
	{
		#region Dependencies
		private readonly IUnitOfWork _unitOfWork;
		private readonly IBatchService _batchService;

		public StockAdjustmentService(
			IUnitOfWork unitOfWork,
			IBatchService batchService)
		{
			_unitOfWork = unitOfWork;
			_batchService = batchService;
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
				throw AppException.BadRequest($"Duplicate variant IDs found: {duplicateIds}. Each variant can only appear once.");
			}

			foreach (var detail in request.AdjustmentDetails)
			{
				var variant = await _unitOfWork.Variants.GetByIdAsync(detail.VariantId) ?? throw AppException.NotFound($"Variant with ID {detail.VariantId} not found.");
				var batch = await _unitOfWork.Batches.GetByIdAsync(detail.BatchId) ?? throw AppException.NotFound($"Batch with ID {detail.BatchId} not found.");

				if (batch.VariantId != detail.VariantId)
					throw AppException.BadRequest($"Batch {detail.BatchId} does not belong to variant {detail.VariantId}.");
			}

			var totalAdjustmentQty = request.AdjustmentDetails.Sum(d => Math.Abs(d.AdjustmentQuantity));
			bool isAutoApprove = totalAdjustmentQty <= 5; // Magic number 5 - config later
			return await _unitOfWork.ExecuteInTransactionAsync(async () =>
			{
				var stockAdjustment = StockAdjustment.Create(
						 userId,
						 request.AdjustmentDate,
						 request.Reason,
						 request.Note);

				foreach (var detailRequest in request.AdjustmentDetails)
				{
					if (isAutoApprove)
					{
						stockAdjustment.AddApprovedDetail(
							detailRequest.VariantId,
							detailRequest.BatchId,
							detailRequest.AdjustmentQuantity,
							detailRequest.AdjustmentQuantity,
							detailRequest.Note);

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
						stockAdjustment.AddDetail(
							detailRequest.VariantId,
							detailRequest.BatchId,
							detailRequest.AdjustmentQuantity,
							detailRequest.Note);
					}
				}

				if (isAutoApprove)
				{
					stockAdjustment.UpdateStatus(StockAdjustmentStatus.InProgress);
					stockAdjustment.Complete(userId);
				}

				await _unitOfWork.StockAdjustments.AddAsync(stockAdjustment);

				var message = isAutoApprove
					? "Stock adjustment created and auto-approved successfully."
					: "Stock adjustment created and is pending for manager verification.";

				return BaseResponse<string>.Ok(stockAdjustment.Id.ToString(), message);
			});
		}

		public async Task<BaseResponse<string>> VerifyStockAdjustmentAsync(Guid adjustmentId, VerifyStockAdjustmentRequest request, Guid verifiedByUserId)
		{
			var stockAdjustment = await _unitOfWork.StockAdjustments.GetByIdWithDetailsAsync(adjustmentId) ?? throw AppException.NotFound("Stock adjustment not found.");

			stockAdjustment.EnsureVerifiable();

			if (request.AdjustmentDetails.Count != stockAdjustment.AdjustmentDetails.Count)
			{
				throw AppException.BadRequest("Mismatch in number of adjustment details for verification.");
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
				throw AppException.BadRequest($"Duplicate adjust detail IDs found in request: {duplicateIds}. Each import detail can only appear once.");
			}

			// Validate all adjustment details exist and match request
			foreach (var verifyDetail in request.AdjustmentDetails)
			{
				var adjustmentDetail = stockAdjustment.AdjustmentDetails.FirstOrDefault(d => d.Id == verifyDetail.DetailId)
					?? throw AppException.NotFound($"Adjustment detail with ID {verifyDetail.DetailId} not found.");
			}

			// Execute within transaction
			return await _unitOfWork.ExecuteInTransactionAsync(async () =>
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

				return BaseResponse<string>.Ok(stockAdjustment.Id.ToString(), "Stock adjustment verified successfully.");
			});
		}

		public async Task<BaseResponse<StockAdjustmentResponse>> GetStockAdjustmentByIdAsync(Guid id)
		{
			var response = await _unitOfWork.StockAdjustments.GetByIdToViewAsync(id)
				   ?? throw AppException.NotFound("Stock adjustment not found.");

			return BaseResponse<StockAdjustmentResponse>.Ok(response, "Stock adjustment retrieved successfully.");
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

			return BaseResponse<PagedResult<StockAdjustmentListItem>>.Ok(pagedResult, "Stock adjustments retrieved successfully.");
		}

		public async Task<BaseResponse<string>> UpdateAdjustmentStatusAsync(Guid id, UpdateStockAdjustmentStatusRequest request)
		{
			var stockAdjustment = await _unitOfWork.StockAdjustments.GetByIdAsync(id) ?? throw AppException.NotFound("Stock adjustment not found.");

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
			if (!saved) throw AppException.Internal("Failed to update stock adjustment status.");

			return BaseResponse<string>.Ok(id.ToString(), "Stock adjustment status updated successfully.");
		}

		public async Task<BaseResponse<bool>> DeleteStockAdjustmentAsync(Guid id)
		{
			var stockAdjustment = await _unitOfWork.StockAdjustments.GetByIdWithDetailsAsync(id)
					?? throw AppException.NotFound("Stock adjustment not found.");
			stockAdjustment.EnsureIsPending();

			return await _unitOfWork.ExecuteInTransactionAsync(async () =>
			{
				foreach (var detail in stockAdjustment.AdjustmentDetails)
				{
					_unitOfWork.StockAdjustmentDetails.Remove(detail);
				}

				_unitOfWork.StockAdjustments.Remove(stockAdjustment);

				return BaseResponse<bool>.Ok(true, "Stock adjustment deleted successfully.");
			});
		}
	}
}
