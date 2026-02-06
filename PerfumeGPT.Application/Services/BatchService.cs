using FluentValidation;
using MapsterMapper;
using PerfumeGPT.Application.DTOs.Requests.Imports;
using PerfumeGPT.Application.DTOs.Requests.Inventory;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.Batches;
using PerfumeGPT.Application.Interfaces.Repositories;
using PerfumeGPT.Application.Interfaces.Services;
using PerfumeGPT.Domain.Entities;

namespace PerfumeGPT.Application.Services
{
	public class BatchService : IBatchService
	{
		#region Dependencies

		private readonly IBatchRepository _batchRepository;
		private readonly IValidator<CreateBatchRequest> _createBatchValidator;
		private readonly IMapper _mapper;

		public BatchService(
			IBatchRepository batchRepository,
			IValidator<CreateBatchRequest> createBatchValidator,
			IMapper mapper)
		{
			_batchRepository = batchRepository;
			_createBatchValidator = createBatchValidator;
			_mapper = mapper;
		}

		#endregion

		public async Task<List<Batch>> CreateBatchesAsync(Guid variantId, Guid importDetailId, List<CreateBatchRequest> batchRequests)
		{
			var createdBatches = new List<Batch>();

			foreach (var batchRequest in batchRequests)
			{
				var validationResult = await _createBatchValidator.ValidateAsync(batchRequest);
				if (!validationResult.IsValid)
				{
					var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
					throw new ValidationException($"Batch validation failed: {errors}");
				}

				var batch = _mapper.Map<Batch>(batchRequest);
				batch.VariantId = variantId;
				batch.ImportDetailId = importDetailId;

				await _batchRepository.AddAsync(batch);
				createdBatches.Add(batch);
			}

			return createdBatches;
		}

		public bool IsTotalQuantityValid(List<CreateBatchRequest> batchRequests, int expectedTotalQuantity)
		{
			if (batchRequests == null || batchRequests.Count == 0)
			{
				return false;
			}

			var totalQuantity = batchRequests.Sum(b => b.Quantity);
			return totalQuantity == expectedTotalQuantity;
		}

		public async Task<bool> ValidateBatchAvailabilityAsync(Guid variantId, int requiredQuantity)
		{
			var batches = await _batchRepository.GetAvailableBatchesByVariantIdAsync(variantId);

			var totalAvailable = batches.Sum(b => b.AvailableInBatch);

			return totalAvailable >= requiredQuantity;
		}

		public async Task<bool> DeductBatchesByVariantIdAsync(Guid variantId, int quantity)
		{
			return await _batchRepository.DeductBatchesByVariantIdAsync(variantId, quantity);
		}

		public async Task<BaseResponse<PagedResult<BatchDetailResponse>>> GetBatchesAsync(GetBatchesRequest request)
		{
			try
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
			catch (Exception ex)
			{
				return BaseResponse<PagedResult<BatchDetailResponse>>.Fail(
					$"Error retrieving batches: {ex.Message}",
					ResponseErrorType.InternalError
				);
			}
		}

		public async Task<BaseResponse<List<BatchDetailResponse>>> GetBatchesByVariantIdAsync(Guid variantId)
		{
			try
			{
				var BatchDetailResponses = await _batchRepository.GetBatchesByVariantIdAsync(variantId);

				return BaseResponse<List<BatchDetailResponse>>.Ok(BatchDetailResponses);
			}
			catch (Exception ex)
			{
				return BaseResponse<List<BatchDetailResponse>>.Fail(
					$"Error retrieving batches for variant: {ex.Message}",
					ResponseErrorType.InternalError
				);
			}
		}

		public async Task<BaseResponse<BatchDetailResponse>> GetBatchByIdAsync(Guid batchId)
		{
			try
			{
				var response = await _batchRepository.GetBatchByIdAsync(batchId);

				if (response == null)
				{
					return BaseResponse<BatchDetailResponse>.Fail(
						"Batch not found",
						ResponseErrorType.NotFound
					);
				}

				return BaseResponse<BatchDetailResponse>.Ok(response);
			}
			catch (Exception ex)
			{
				return BaseResponse<BatchDetailResponse>.Fail(
					$"Error retrieving batch: {ex.Message}",
					ResponseErrorType.InternalError
				);
			}
		}

		public async Task<bool> IncreaseBatchQuantityAsync(Guid batchId, int quantity)
		{
			return await _batchRepository.IncreaseBatchQuantityAsync(batchId, quantity);
		}

		public async Task<bool> DecreaseBatchQuantityAsync(Guid batchId, int quantity)
		{
			return await _batchRepository.DecreaseBatchQuantityAsync(batchId, quantity);
		}
	}
}
