using FluentValidation;
using MapsterMapper;
using PerfumeGPT.Application.DTOs.Requests.Batches;
using PerfumeGPT.Application.DTOs.Requests.Inventory;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.Batches;
using PerfumeGPT.Application.Exceptions;
using PerfumeGPT.Application.Interfaces.Repositories;
using PerfumeGPT.Application.Interfaces.Services;
using PerfumeGPT.Domain.Entities;

namespace PerfumeGPT.Application.Services
{
	public class BatchService : IBatchService
	{
		#region Dependencies
		private readonly IBatchRepository _batchRepository;
		private readonly IStockRepository _stockRepository;
		private readonly IValidator<CreateBatchRequest> _createBatchValidator;
		private readonly IMapper _mapper;

		public BatchService(
			IBatchRepository batchRepository,
			IStockRepository stockRepository,
			IValidator<CreateBatchRequest> createBatchValidator,
			IMapper mapper)
		{
			_batchRepository = batchRepository;
			_stockRepository = stockRepository;
			_createBatchValidator = createBatchValidator;
			_mapper = mapper;
		}
		#endregion

		public async Task<List<Batch>> CreateBatchesAsync(Guid variantId, Guid importDetailId, List<CreateBatchRequest> batchRequests)
		{
			if (batchRequests == null || batchRequests.Count == 0)
			{
				throw AppException.BadRequest("At least one batch is required.");
			}

			var createdBatches = new List<Batch>();

			foreach (var batchRequest in batchRequests)
			{
				var validationResult = await _createBatchValidator.ValidateAsync(batchRequest);
				if (!validationResult.IsValid)
				{
					throw AppException.BadRequest(
						"Batch validation failed",
						validationResult.Errors.Select(e => e.ErrorMessage).ToList());
				}

				var mapped = _mapper.Map<Batch>(batchRequest);
				var batch = Batch.CreateForImport(
					variantId,
					importDetailId,
					mapped.BatchCode,
					mapped.ManufactureDate,
					mapped.ExpiryDate,
					mapped.ImportQuantity);

				await _batchRepository.AddAsync(batch);
				createdBatches.Add(batch);
			}

			await _batchRepository.SaveChangesAsync();
			await _stockRepository.UpdateStockAsync(variantId);

			return createdBatches;
		}

		public bool IsTotalQuantityValid(List<CreateBatchRequest> batchRequests, int expectedTotalQuantity)
		{
			if (batchRequests == null || batchRequests.Count == 0)
			{
				return false;
			}

			if (expectedTotalQuantity <= 0)
			{
				return false;
			}

			var totalQuantity = batchRequests.Sum(b => b.Quantity);
			return totalQuantity == expectedTotalQuantity;
		}

		public async Task<bool> ValidateBatchAvailabilityAsync(Guid variantId, int requiredQuantity)
		{
			if (requiredQuantity <= 0)
			{
				throw AppException.BadRequest("Required quantity must be greater than 0.");
			}

			var batches = await _batchRepository.GetAvailableBatchesByVariantIdAsync(variantId);

			var totalAvailable = batches.Sum(b => b.AvailableInBatch);

			return totalAvailable >= requiredQuantity;
		}

		public async Task<bool> DeductBatchesByVariantIdAsync(Guid variantId, int quantity)
		{
			if (quantity <= 0)
			{
				throw AppException.BadRequest("Quantity must be greater than 0.");
			}

			var result = await _batchRepository.DeductBatchesByVariantIdAsync(variantId, quantity);
			if (result)
			{
				await _stockRepository.UpdateStockAsync(variantId);
			}
			return result;
		}

		public async Task<BaseResponse<PagedResult<BatchDetailResponse>>> GetBatchesAsync(GetBatchesRequest request)
		{
			var (batches, totalCount) = await _batchRepository.GetBatchesAsync(request);

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
			var batchLookup = await _batchRepository.GetBatchLookupAsync();
			return BaseResponse<List<BatchLookupResponse>>.Ok(batchLookup);
		}

		public async Task<BaseResponse<List<BatchDetailResponse>>> GetBatchesByVariantIdAsync(Guid variantId)
		{
			var batchDetailResponses = await _batchRepository.GetBatchesByVariantIdAsync(variantId);
			return BaseResponse<List<BatchDetailResponse>>.Ok(batchDetailResponses);
		}

		public async Task<BaseResponse<BatchDetailResponse>> GetBatchByIdAsync(Guid batchId)
		{
			var response = await _batchRepository.GetBatchByIdAsync(batchId);
			return response == null ? throw AppException.NotFound("Batch not found") : BaseResponse<BatchDetailResponse>.Ok(response);
		}

		public async Task<bool> IncreaseBatchQuantityAsync(Guid batchId, int quantity)
		{
			if (quantity <= 0)
			{
				throw AppException.BadRequest("Quantity must be greater than 0.");
			}

			var result = await _batchRepository.IncreaseBatchQuantityAsync(batchId, quantity);
			if (result)
			{
				var variantId = await _batchRepository.GetVariantIdByBatchIdAsync(batchId);
				if (variantId.HasValue)
				{
					await _stockRepository.UpdateStockAsync(variantId.Value);
				}
			}
			else
			{
				throw AppException.BadRequest("Failed to increase batch quantity.");
			}

			return result;
		}

		public async Task<bool> DecreaseBatchQuantityAsync(Guid batchId, int quantity)
		{
			if (quantity <= 0)
			{
				throw AppException.BadRequest("Quantity must be greater than 0.");
			}

			var result = await _batchRepository.DecreaseBatchQuantityAsync(batchId, quantity);
			if (result)
			{
				var variantId = await _batchRepository.GetVariantIdByBatchIdAsync(batchId);
				if (variantId.HasValue)
				{
					await _stockRepository.UpdateStockAsync(variantId.Value);
				}
			}
			else
			{
				throw AppException.BadRequest("Failed to decrease batch quantity.");
			}

			return result;
		}
	}
}
