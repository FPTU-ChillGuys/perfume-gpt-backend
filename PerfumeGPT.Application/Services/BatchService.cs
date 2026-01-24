using FluentValidation;
using PerfumeGPT.Application.DTOs.Requests.Imports;
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
}

}

