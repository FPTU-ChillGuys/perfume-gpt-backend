using PerfumeGPT.Application.DTOs.Requests.Inventory.Batches;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.Batches;
using PerfumeGPT.Application.Exceptions;
using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Application.Interfaces.Services;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Domain.Enums;
using MapsterMapper;

namespace PerfumeGPT.Application.Services
{
	public class BatchService : IBatchService
	{
		private readonly IUnitOfWork _unitOfWork;
		private readonly IMapper _mapper;

		public BatchService(IUnitOfWork unitOfWork, IMapper mapper)
		{
			_unitOfWork = unitOfWork;
			_mapper = mapper;
		}

		public async Task CreateBatchesAsync(Guid variantId, Guid importDetailId, List<CreateBatchRequest> batchRequests)
		{
			if (batchRequests == null || batchRequests.Count == 0)
				throw AppException.BadRequest("Bắt buộc có ít nhất một lô.");

			foreach (var batchRequest in batchRequests)
			{
				var createBatchDto = _mapper.Map<Batch.CreateForImportDto>(batchRequest) with
				{
					VariantId = variantId,
					ImportDetailId = importDetailId
				};

				var batch = Batch.CreateForImport(createBatchDto);

				await _unitOfWork.Batches.AddAsync(batch);
			}
			await _unitOfWork.Stocks.UpdateStockAsync(variantId);
		}

		public async Task<bool> ValidateBatchAvailabilityAsync(Guid variantId, int requiredQuantity)
		{
			if (requiredQuantity <= 0)
			{
				throw AppException.BadRequest("Số lượng yêu cầu phải lớn hơn 0.");
			}

			var batches = await _unitOfWork.Batches.GetAvailableBatchesByVariantIdAsync(variantId);

			var totalAvailable = batches.Sum(b => b.AvailableInBatch);

			return totalAvailable >= requiredQuantity;
		}

		//public async Task<bool> DeductBatchesByVariantIdAsync(Guid variantId, int quantity, Guid referenceId)
		//{
		//	if (quantity <= 0)
		//	{
		//		throw AppException.BadRequest("Quantity must be greater than 0.");
		//	}

		//	if (referenceId == Guid.Empty)
		//	{
		//		throw AppException.BadRequest("Reference ID is required.");
		//	}

		//	var result = await _unitOfWork.Batches.DeductBatchesByVariantIdAsync(variantId, quantity, referenceId);
		//	if (result)
		//	{
		//		await _unitOfWork.Stocks.UpdateStockAsync(variantId);
		//	}

		//	return result;
		//}

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
			return response == null ? throw AppException.NotFound("Không tìm thấy lô") : BaseResponse<BatchDetailResponse>.Ok(response);
		}

		public async Task IncreaseBatchQuantityAsync(Guid batchId, int quantity)
		{
			if (quantity <= 0)
			{
				throw AppException.BadRequest("Số lượng phải lớn hơn 0.");
			}

			var batch = await _unitOfWork.Batches.GetByIdAsync(batchId)
			   ?? throw AppException.NotFound($"Không tìm thấy lô {batchId}");

			batch.IncreaseQuantity(
				 quantity,
				 StockTransactionType.Adjustment,
				 batchId,
				 null,
				 $"Tăng thủ công số lượng cho lô {batchId}.");
			_unitOfWork.Batches.Update(batch);

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
				throw AppException.BadRequest("Số lượng phải lớn hơn 0.");
			}

			var batch = await _unitOfWork.Batches.GetByIdAsync(batchId)
			   ?? throw AppException.NotFound($"Không tìm thấy lô {batchId}");

			batch.DecreaseQuantity(
				 quantity,
				 StockTransactionType.Adjustment,
				 batchId,
				 null,
				 $"Giảm thủ công số lượng cho lô {batchId}.");
			_unitOfWork.Batches.Update(batch);

			var variantId = await _unitOfWork.Batches.GetVariantIdByBatchIdAsync(batchId);
			if (variantId.HasValue)
			{
				await _unitOfWork.Stocks.UpdateStockAsync(variantId.Value);
			}
		}
	}
}
