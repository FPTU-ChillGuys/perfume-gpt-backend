using PerfumeGPT.Application.DTOs.Requests.Inventory.Batches;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.Batches;
using PerfumeGPT.Application.Exceptions;
using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Application.Interfaces.Services;
using PerfumeGPT.Domain.Entities;

namespace PerfumeGPT.Application.Services
{
	public class BatchService : IBatchService
	{
		#region Dependencies
		private readonly IUnitOfWork _unitOfWork;

		public BatchService(IUnitOfWork unitOfWork)
		{
			_unitOfWork = unitOfWork;
		}
		#endregion

		public async Task CreateBatchesAsync(Guid variantId, Guid importDetailId, List<CreateBatchRequest> batchRequests)
		{
			if (batchRequests == null || batchRequests.Count == 0)
				throw AppException.BadRequest("At least one batch is required.");

			foreach (var batchRequest in batchRequests)
			{
				var batch = Batch.CreateForImport(
					variantId,
					importDetailId,
					batchRequest.BatchCode,
					batchRequest.ManufactureDate,
					batchRequest.ExpiryDate,
					batchRequest.Quantity);

				await _unitOfWork.Batches.AddAsync(batch);
			}
			await _unitOfWork.Stocks.UpdateStockAsync(variantId);
		}

		public async Task<bool> ValidateBatchAvailabilityAsync(Guid variantId, int requiredQuantity)
		{
			if (requiredQuantity <= 0)
			{
				throw AppException.BadRequest("Required quantity must be greater than 0.");
			}

			var batches = await _unitOfWork.Batches.GetAvailableBatchesByVariantIdAsync(variantId);

			var totalAvailable = batches.Sum(b => b.AvailableInBatch);

			return totalAvailable >= requiredQuantity;
		}

		public async Task<bool> DeductBatchesByVariantIdAsync(Guid variantId, int quantity)
		{
			if (quantity <= 0)
			{
				throw AppException.BadRequest("Quantity must be greater than 0.");
			}

			var result = await _unitOfWork.Batches.DeductBatchesByVariantIdAsync(variantId, quantity);
			if (result)
			{
				await _unitOfWork.Stocks.UpdateStockAsync(variantId);
			}

			return result;
		}

		public async Task<BaseResponse<PagedResult<BatchDetailResponse>>> GetBatchesAsync(GetBatchesRequest request)
		{
			var (batches, totalCount) = await _unitOfWork.Batches.GetBatchesAsync(request);

			var pagedResult = new PagedResult<BatchDetailResponse>(
				batches,
				request.PageNumber,
				request.PageSize,
				totalCount
			);

			return BaseResponse<PagedResult<BatchDetailResponse>>.Ok(pagedResult);
		}

		public async Task<BaseResponse<List<BatchLookupResponse>>> GetBatchLookupAsync()
		{
			var batchLookup = await _unitOfWork.Batches.GetBatchLookupAsync();
			return BaseResponse<List<BatchLookupResponse>>.Ok(batchLookup);
		}

		public async Task<BaseResponse<BatchDetailResponse>> GetBatchByIdAsync(Guid batchId)
		{
			var response = await _unitOfWork.Batches.GetBatchByIdAsync(batchId);
			return response == null ? throw AppException.NotFound("Batch not found") : BaseResponse<BatchDetailResponse>.Ok(response);
		}

		public async Task IncreaseBatchQuantityAsync(Guid batchId, int quantity)
		{
			if (quantity <= 0)
			{
				throw AppException.BadRequest("Quantity must be greater than 0.");
			}

			var result = await _unitOfWork.Batches.IncreaseBatchQuantityAsync(batchId, quantity);
			if (!result)
			{
				throw AppException.BadRequest("Failed to increase batch quantity.");
			}

			var variantId = await _unitOfWork.Batches.GetVariantIdByBatchIdAsync(batchId);
			if (variantId.HasValue)
			{
				await _unitOfWork.Stocks.UpdateStockAsync(variantId.Value);
			}
		}

		public async Task DecreaseBatchQuantityAsync(Guid batchId, int quantity)
		{
			if (quantity <= 0)
			{
				throw AppException.BadRequest("Quantity must be greater than 0.");
			}

			var result = await _unitOfWork.Batches.DecreaseBatchQuantityAsync(batchId, quantity);
			if (!result)
			{
				throw AppException.BadRequest("Failed to decrease batch quantity.");
			}

			var variantId = await _unitOfWork.Batches.GetVariantIdByBatchIdAsync(batchId);
			if (variantId.HasValue)
			{
				await _unitOfWork.Stocks.UpdateStockAsync(variantId.Value);
			}
		}
	}
}
