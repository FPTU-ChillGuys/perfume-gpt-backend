using FluentValidation;
using PerfumeGPT.Application.DTOs.Requests.Imports;
using PerfumeGPT.Application.DTOs.Requests.Inventory;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.Inventory;
using PerfumeGPT.Application.Interfaces.Repositories;
using PerfumeGPT.Application.Interfaces.Services;
using PerfumeGPT.Domain.Entities;

namespace PerfumeGPT.Application.Services
{
	public class BatchService : IBatchService
	{
		private readonly IBatchRepository _batchRepository;
		private readonly IValidator<CreateBatchRequest> _createBatchValidator;

		public BatchService(
			IBatchRepository batchRepository,
			IValidator<CreateBatchRequest> createBatchValidator)
		{
			_batchRepository = batchRepository;
			_createBatchValidator = createBatchValidator;
		}

		public async Task<List<Batch>> CreateBatchesAsync(Guid variantId, Guid importDetailId, List<CreateBatchRequest> batchRequests)
		{
			var createdBatches = new List<Batch>();

			foreach (var batchRequest in batchRequests)
			{
				// Use FluentValidation instead of manual validation
				var validationResult = await _createBatchValidator.ValidateAsync(batchRequest);
				if (!validationResult.IsValid)
				{
					var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
					throw new ValidationException($"Batch validation failed: {errors}");
				}

				var batch = new Batch
				{
					VariantId = variantId,
					ImportDetailId = importDetailId,
					BatchCode = batchRequest.BatchCode,
					ManufactureDate = batchRequest.ManufactureDate,
					ExpiryDate = batchRequest.ExpiryDate,
					ImportQuantity = batchRequest.Quantity,
					RemainingQuantity = batchRequest.Quantity
				};

				await _batchRepository.AddAsync(batch);
				createdBatches.Add(batch);
			}

			return createdBatches;
		}

		public bool ValidateBatches(List<CreateBatchRequest> batchRequests, int expectedTotalQuantity)
		{
			if (batchRequests == null || batchRequests.Count == 0)
			{
				return false;
			}

			var totalQuantity = batchRequests.Sum(b => b.Quantity);
			return totalQuantity == expectedTotalQuantity;
		}

		public async Task<bool> DeductBatchAsync(Guid batchId, int quantity)
		{
			// Validation
			if (quantity <= 0)
			{
				return false;
			}

			var batch = await _batchRepository.GetByIdAsync(batchId);
			if (batch == null)
			{
				return false;
			}

			// Check if batch has enough quantity
			if (batch.RemainingQuantity < quantity)
			{
				return false;
			}

			// Check if batch is expired
			if (batch.ExpiryDate < DateTime.UtcNow)
			{
				return false;
			}

			// Deduct quantity
			batch.RemainingQuantity -= quantity;
			_batchRepository.Update(batch);

			// Don't call SaveChanges - let the orchestrator/transaction handle it
			return true;
		}

		public async Task<bool> ValidateBatchAvailabilityAsync(Guid variantId, int requiredQuantity)
		{
			// Get all non-expired batches for the variant
			var batches = await _batchRepository.GetAvailableBatchesByVariantAsync(variantId);

			var totalAvailable = batches
				.Where(b => b.ExpiryDate >= DateTime.UtcNow)
				.Sum(b => b.RemainingQuantity);

			return totalAvailable >= requiredQuantity;
		}

		public async Task<bool> DeductBatchesByVariantAsync(Guid variantId, int quantity)
		{
			// Validation
			if (quantity <= 0)
			{
				return false;
			}

			// Get all available batches ordered by expiry date (FIFO - oldest first)
			var batches = await _batchRepository.GetAvailableBatchesByVariantAsync(variantId);
			var availableBatches = batches
				.Where(b => b.ExpiryDate >= DateTime.UtcNow && b.RemainingQuantity > 0)
				.OrderBy(b => b.ExpiryDate)
				.ToList();

			var remainingToDeduct = quantity;

			foreach (var batch in availableBatches)
			{
				if (remainingToDeduct <= 0)
					break;

				var deductFromThisBatch = Math.Min(batch.RemainingQuantity, remainingToDeduct);
				batch.RemainingQuantity -= deductFromThisBatch;
				remainingToDeduct -= deductFromThisBatch;

				_batchRepository.Update(batch);
			}

			// If we couldn't deduct the full quantity, return false
			if (remainingToDeduct > 0)
			{
				return false;
			}

			// Don't call SaveChanges - let the orchestrator/transaction handle it
			return true;
		}

		public async Task<BaseResponse<PagedResult<BatchResponse>>> GetBatchesAsync(GetBatchesRequest request)
		{
			try
			{
				var now = DateTime.UtcNow;

				// Use repository method that encapsulates all query logic
				var (batches, totalCount) = await _batchRepository.GetBatchesWithFiltersAsync(request);

				// Map to response DTOs
				var batchResponses = batches.Select(b => new BatchResponse
				{
					Id = b.Id,
					VariantId = b.VariantId,
					VariantSku = b.ProductVariant.Sku,
					ProductName = b.ProductVariant.Product.Name ?? "",
					VolumeMl = b.ProductVariant.VolumeMl,
					ConcentrationName = b.ProductVariant.Concentration.Name ?? "",
					BatchCode = b.BatchCode,
					ManufactureDate = b.ManufactureDate,
					ExpiryDate = b.ExpiryDate,
					ImportQuantity = b.ImportQuantity,
					RemainingQuantity = b.RemainingQuantity,
					IsExpired = b.ExpiryDate < now,
					DaysUntilExpiry = (int)(b.ExpiryDate - now).TotalDays,
					CreatedAt = b.CreatedAt
				}).ToList();

				var pagedResult = new PagedResult<BatchResponse>(
					batchResponses,
					request.PageNumber,
					request.PageSize,
					totalCount
				);

				return BaseResponse<PagedResult<BatchResponse>>.Ok(pagedResult);
			}
			catch (Exception ex)
			{
				return BaseResponse<PagedResult<BatchResponse>>.Fail(
					$"Error retrieving batches: {ex.Message}",
					ResponseErrorType.InternalError
				);
			}
		}

		public async Task<BaseResponse<List<BatchResponse>>> GetBatchesByVariantIdAsync(Guid variantId)
		{
			try
			{
				var now = DateTime.UtcNow;

				// Use repository method that encapsulates query logic with includes
				var batches = await _batchRepository.GetBatchesByVariantWithIncludesAsync(variantId);

				// Map to response DTOs
				var batchResponses = batches.Select(b => new BatchResponse
				{
					Id = b.Id,
					VariantId = b.VariantId,
					VariantSku = b.ProductVariant.Sku,
					ProductName = b.ProductVariant.Product.Name ?? "",
					VolumeMl = b.ProductVariant.VolumeMl,
					ConcentrationName = b.ProductVariant.Concentration.Name ?? "",
					BatchCode = b.BatchCode,
					ManufactureDate = b.ManufactureDate,
					ExpiryDate = b.ExpiryDate,
					ImportQuantity = b.ImportQuantity,
					RemainingQuantity = b.RemainingQuantity,
					IsExpired = b.ExpiryDate < now,
					DaysUntilExpiry = (int)(b.ExpiryDate - now).TotalDays,
					CreatedAt = b.CreatedAt
				}).ToList();

				return BaseResponse<List<BatchResponse>>.Ok(batchResponses);
			}
			catch (Exception ex)
			{
				return BaseResponse<List<BatchResponse>>.Fail(
					$"Error retrieving batches for variant: {ex.Message}",
					ResponseErrorType.InternalError
				);
			}
		}

		public async Task<BaseResponse<BatchResponse>> GetBatchByIdAsync(Guid batchId)
		{
			try
			{
				var now = DateTime.UtcNow;

				// Use repository method that encapsulates query logic with includes
				var batch = await _batchRepository.GetBatchByIdWithIncludesAsync(batchId);

				if (batch == null)
				{
					return BaseResponse<BatchResponse>.Fail(
						"Batch not found",
						ResponseErrorType.NotFound
					);
				}

				// Map to response DTO
				var response = new BatchResponse
				{
					Id = batch.Id,
					VariantId = batch.VariantId,
					VariantSku = batch.ProductVariant.Sku,
					ProductName = batch.ProductVariant.Product.Name,
					VolumeMl = batch.ProductVariant.VolumeMl,
					ConcentrationName = batch.ProductVariant.Concentration.Name,
					BatchCode = batch.BatchCode,
					ManufactureDate = batch.ManufactureDate,
					ExpiryDate = batch.ExpiryDate,
					ImportQuantity = batch.ImportQuantity,
					RemainingQuantity = batch.RemainingQuantity,
					IsExpired = batch.ExpiryDate < now,
					DaysUntilExpiry = (int)(batch.ExpiryDate - now).TotalDays,
					CreatedAt = batch.CreatedAt
				};

				return BaseResponse<BatchResponse>.Ok(response);
			}
			catch (Exception ex)
			{
				return BaseResponse<BatchResponse>.Fail(
					$"Error retrieving batch: {ex.Message}",
					ResponseErrorType.InternalError
				);
			}
		}

		public async Task IncreaseBatchQuantityAsync(Guid batchId, int quantity)
		{
			var batch = await _batchRepository.GetByIdAsync(batchId);
			if (batch == null)
			{
				throw new InvalidOperationException($"Batch {batchId} not found.");
			}

			batch.RemainingQuantity += quantity;
			_batchRepository.Update(batch);
		}

		public async Task DecreaseBatchQuantityAsync(Guid batchId, int quantity)
		{
			var batch = await _batchRepository.GetByIdAsync(batchId) ?? throw new InvalidOperationException($"Batch {batchId} not found.");
			if (batch.RemainingQuantity < quantity)
			{
				throw new InvalidOperationException($"Insufficient quantity in batch {batchId}. Available: {batch.RemainingQuantity}, Requested: {quantity}");
			}

			batch.RemainingQuantity -= quantity;
			_batchRepository.Update(batch);
		}
	}
}
