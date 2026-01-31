using FluentValidation;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using PerfumeGPT.Application.DTOs.Requests.StockAdjustments;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.StockAdjustments;
using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Application.Interfaces.Services;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.Services
{
	public class StockAdjustmentService : IStockAdjustmentService
	{
		private readonly IUnitOfWork _unitOfWork;
		private readonly IStockService _stockService;
		private readonly IBatchService _batchService;
		private readonly IValidator<CreateStockAdjustmentRequest> _createValidator;
		private readonly IValidator<VerifyStockAdjustmentRequest> _verifyValidator;
		private readonly IValidator<UpdateStockAdjustmentStatusRequest> _updateStatusValidator;
		private readonly IMapper _mapper;

		public StockAdjustmentService(
			IUnitOfWork unitOfWork,
			IStockService stockService,
			IBatchService batchService,
			IMapper mapper,
			IValidator<CreateStockAdjustmentRequest> createValidator,
			IValidator<VerifyStockAdjustmentRequest> verifyValidator,
			IValidator<UpdateStockAdjustmentStatusRequest> updateStatusValidator)
		{
			_unitOfWork = unitOfWork;
			_stockService = stockService;
			_batchService = batchService;
			_mapper = mapper;
			_createValidator = createValidator;
			_verifyValidator = verifyValidator;
			_updateStatusValidator = updateStatusValidator;
		}

		public async Task<BaseResponse<string>> CreateStockAdjustmentAsync(CreateStockAdjustmentRequest request, Guid userId)
		{
			try
			{
				// Validate request using FluentValidation
				var validationResult = await _createValidator.ValidateAsync(request);
				if (!validationResult.IsValid)
				{
					var errors = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
					return BaseResponse<string>.Fail(errors, ResponseErrorType.BadRequest);
				}

				// VALIDATION: Check for duplicate variant IDs
				var duplicateVariants = request.AdjustmentDetails
					.GroupBy(d => d.VariantId)
					.Where(g => g.Count() > 1)
					.Select(g => g.Key)
					.ToList();

				if (duplicateVariants.Count != 0)
				{
					var duplicateIds = string.Join(", ", duplicateVariants);
					return BaseResponse<string>.Fail($"Duplicate variant IDs found: {duplicateIds}. Each variant can only appear once per adjustment.", ResponseErrorType.BadRequest);
				}

				// Validate variants and batches exist
				foreach (var detail in request.AdjustmentDetails)
				{
					var variant = await _unitOfWork.Variants.GetByIdAsync(detail.VariantId);
					if (variant == null)
					{
						return BaseResponse<string>.Fail($"Variant with ID {detail.VariantId} not found.", ResponseErrorType.NotFound);
					}

				if (detail.BatchId == Guid.Empty)
				{
					return BaseResponse<string>.Fail("Batch ID is required for stock adjustment.", ResponseErrorType.BadRequest);
				}

				var batch = await _unitOfWork.Batches.GetByIdAsync(detail.BatchId);
				if (batch == null)
						{
					return BaseResponse<string>.Fail($"Batch with ID {detail.BatchId} not found.", ResponseErrorType.NotFound);
				}

				if (batch.VariantId != detail.VariantId)
				{
					return BaseResponse<string>.Fail($"Batch {detail.BatchId} does not belong to variant {detail.VariantId}.", ResponseErrorType.BadRequest);
				}
			}

			// Execute within transaction
				return await _unitOfWork.ExecuteInTransactionAsync(async () =>
				{
					var stockAdjustment = new StockAdjustment
					{
						CreatedById = userId,
						AdjustmentDate = request.AdjustmentDate,
						Reason = request.Reason,
						Note = request.Note,
						Status = StockAdjustmentStatus.Pending
					};

					await _unitOfWork.StockAdjustments.AddAsync(stockAdjustment);

					foreach (var detailRequest in request.AdjustmentDetails)
					{
						var adjustmentDetail = new StockAdjustmentDetail
						{
							StockAdjustmentId = stockAdjustment.Id,
							ProductVariantId = detailRequest.VariantId,
							BatchId = detailRequest.BatchId,
							AdjustmentQuantity = detailRequest.AdjustmentQuantity,
							ApprovedQuantity = 0,
							Note = detailRequest.Note
						};

						await _unitOfWork.StockAdjustmentDetails.AddAsync(adjustmentDetail);
					}

					return BaseResponse<string>.Ok(stockAdjustment.Id.ToString(), "Stock adjustment created successfully.");
				});
			}
			catch (Exception ex)
			{
				return BaseResponse<string>.Fail($"Error creating stock adjustment: {ex.Message}", ResponseErrorType.InternalError);
			}
		}

		public async Task<BaseResponse<string>> VerifyStockAdjustmentAsync(Guid adjustmentId, VerifyStockAdjustmentRequest request, Guid verifiedByUserId)
		{
			try
			{
				// Validate request using FluentValidation
				var validationResult = await _verifyValidator.ValidateAsync(request);
				if (!validationResult.IsValid)
				{
					var errors = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
					return BaseResponse<string>.Fail(errors, ResponseErrorType.BadRequest);
				}

				// Validation - Entity existence and business rules
				var stockAdjustment = await _unitOfWork.StockAdjustments.GetByConditionAsync(
					predicate: sa => sa.Id == adjustmentId,
					include: query => query.Include(sa => sa.AdjustmentDetails));

				if (stockAdjustment == null)
				{
					return BaseResponse<string>.Fail("Stock adjustment not found.", ResponseErrorType.NotFound);
				}

				if (stockAdjustment.Status != StockAdjustmentStatus.InProgress)
				{
					return BaseResponse<string>.Fail("Only in progress stock adjustments can be verified.", ResponseErrorType.BadRequest);
				}

				if (request.AdjustmentDetails.Count != stockAdjustment.AdjustmentDetails.Count)
				{
					return BaseResponse<string>.Fail("Mismatch in number of adjustment details for verification.", ResponseErrorType.BadRequest);
				}

				// Validate all adjustment details exist and match request
				foreach (var verifyDetail in request.AdjustmentDetails)
				{
					var adjustmentDetail = stockAdjustment.AdjustmentDetails.FirstOrDefault(d => d.Id == verifyDetail.DetailId);
					if (adjustmentDetail == null)
					{
						return BaseResponse<string>.Fail($"Adjustment detail with ID {verifyDetail.DetailId} not found.", ResponseErrorType.NotFound);
					}
				}

				// Execute within transaction
				return await _unitOfWork.ExecuteInTransactionAsync(async () =>
				{
					foreach (var verifyDetail in request.AdjustmentDetails)
					{
						var adjustmentDetail = stockAdjustment.AdjustmentDetails.First(d => d.Id == verifyDetail.DetailId);

						// Update adjustment detail
						adjustmentDetail.ApprovedQuantity = verifyDetail.ApprovedQuantity;
						adjustmentDetail.Note = verifyDetail.Note;
						_unitOfWork.StockAdjustmentDetails.Update(adjustmentDetail);

						// Apply stock adjustment
						if (verifyDetail.ApprovedQuantity > 0)
						{
							// Increase stock quantity
							var stockIncreased = await _stockService.IncreaseStockAsync(
								adjustmentDetail.ProductVariantId,
								verifyDetail.ApprovedQuantity);

							if (!stockIncreased)
							{
								throw new InvalidOperationException($"Failed to increase stock for variant {adjustmentDetail.ProductVariantId}");
							}

							// Update batch quantity (BatchId is required)
							await _batchService.IncreaseBatchQuantityAsync(adjustmentDetail.BatchId, verifyDetail.ApprovedQuantity);
						}
						else if (verifyDetail.ApprovedQuantity < 0)
						{
							// Decrease stock quantity
							var stockDecreased = await _stockService.DecreaseStockAsync(
								adjustmentDetail.ProductVariantId,
								Math.Abs(verifyDetail.ApprovedQuantity));

							if (!stockDecreased)
							{
								throw new InvalidOperationException($"Failed to decrease stock for variant {adjustmentDetail.ProductVariantId}");
							}

							// Update batch quantity (BatchId is required)
							await _batchService.DecreaseBatchQuantityAsync(adjustmentDetail.BatchId, Math.Abs(verifyDetail.ApprovedQuantity));
						}
					}

					// Update stock adjustment status to Completed and set verifier
					stockAdjustment.Status = StockAdjustmentStatus.Completed;
					stockAdjustment.VerifiedById = verifiedByUserId;
					_unitOfWork.StockAdjustments.Update(stockAdjustment);

					return BaseResponse<string>.Ok(stockAdjustment.Id.ToString(), "Stock adjustment verified successfully.");
				});
			}
			catch (Exception ex)
			{
				return BaseResponse<string>.Fail($"Error verifying stock adjustment: {ex.Message}", ResponseErrorType.InternalError);
			}
		}

		public async Task<BaseResponse<StockAdjustmentResponse>> GetStockAdjustmentByIdAsync(Guid id)
		{
			try
			{
				var stockAdjustment = await _unitOfWork.StockAdjustments.GetByIdWithDetailsAsync(id);

				if (stockAdjustment == null)
				{
					return BaseResponse<StockAdjustmentResponse>.Fail("Stock adjustment not found.", ResponseErrorType.NotFound);
				}

				var response = _mapper.Map<StockAdjustmentResponse>(stockAdjustment);

				return BaseResponse<StockAdjustmentResponse>.Ok(response, "Stock adjustment retrieved successfully.");
			}
			catch (Exception ex)
			{
				return BaseResponse<StockAdjustmentResponse>.Fail($"Error retrieving stock adjustment: {ex.Message}", ResponseErrorType.InternalError);
			}
		}

		public async Task<BaseResponse<PagedResult<StockAdjustmentListItem>>> GetPagedStockAdjustmentsAsync(GetPagedStockAdjustmentsRequest request)
		{
			try
			{
				var (items, totalCount) = await _unitOfWork.StockAdjustments.GetPagedWithDetailsAsync(request);

				var response = _mapper.Map<List<StockAdjustmentListItem>>(items);

				var pagedResult = new PagedResult<StockAdjustmentListItem>(
					response,
					request.PageNumber,
					request.PageSize,
					totalCount
				);

				return BaseResponse<PagedResult<StockAdjustmentListItem>>.Ok(pagedResult, "Stock adjustments retrieved successfully.");
			}
			catch (Exception ex)
			{
				return BaseResponse<PagedResult<StockAdjustmentListItem>>.Fail($"Error retrieving stock adjustments: {ex.Message}", ResponseErrorType.InternalError);
			}
		}

		public async Task<BaseResponse<string>> UpdateAdjustmentStatusAsync(Guid id, UpdateStockAdjustmentStatusRequest request)
		{
			try
			{
				// Validate request using FluentValidation
				var validationResult = await _updateStatusValidator.ValidateAsync(request);
				if (!validationResult.IsValid)
				{
					var errors = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
					return BaseResponse<string>.Fail(errors, ResponseErrorType.BadRequest);
				}

				var stockAdjustment = await _unitOfWork.StockAdjustments.GetByIdAsync(id);

				if (stockAdjustment == null)
				{
					return BaseResponse<string>.Fail("Stock adjustment not found.", ResponseErrorType.NotFound);
				}

				stockAdjustment.Status = request.Status;
				_unitOfWork.StockAdjustments.Update(stockAdjustment);
				await _unitOfWork.SaveChangesAsync();

				return BaseResponse<string>.Ok(id.ToString(), "Stock adjustment status updated successfully.");
			}
			catch (Exception ex)
			{
				return BaseResponse<string>.Fail($"Error updating stock adjustment status: {ex.Message}", ResponseErrorType.InternalError);
			}
		}

		public async Task<BaseResponse<bool>> DeleteStockAdjustmentAsync(Guid id)
		{
			try
			{
				var stockAdjustment = await _unitOfWork.StockAdjustments.GetByIdWithDetailsForDeleteAsync(id);

				if (stockAdjustment == null)
				{
					return BaseResponse<bool>.Fail("Stock adjustment not found.", ResponseErrorType.NotFound);
				}

				if (stockAdjustment.Status == StockAdjustmentStatus.Completed)
				{
					return BaseResponse<bool>.Fail("Completed stock adjustments cannot be deleted.", ResponseErrorType.BadRequest);
				}

				return await _unitOfWork.ExecuteInTransactionAsync(async () =>
				{
					// Delete adjustment details
					foreach (var detail in stockAdjustment.AdjustmentDetails)
					{
						_unitOfWork.StockAdjustmentDetails.Remove(detail);
					}

					// Delete adjustment
					_unitOfWork.StockAdjustments.Remove(stockAdjustment);

					return BaseResponse<bool>.Ok(true, "Stock adjustment deleted successfully.");
				});
			}
			catch (Exception ex)
			{
				return BaseResponse<bool>.Fail($"Error deleting stock adjustment: {ex.Message}", ResponseErrorType.InternalError);
			}
		}
	}
}
