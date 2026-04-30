using PerfumeGPT.Application.DTOs.Requests.Inventory.Batches;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.Batches;
using PerfumeGPT.Application.Exceptions;
using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Application.Interfaces.Services;
using PerfumeGPT.Domain.Entities;
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

		public async Task<BaseResponse<PagedResult<BatchDetailResponse>>> GetBatchesAsync(GetBatchesRequest request)
		{
			var currentPolicy = await _unitOfWork.StorePolicies.GetCurrentPolicyAsync();

			int threshold = currentPolicy?.BatchExpiringSoonThresholdInDays ?? 30;

			var (batches, totalCount) = await _unitOfWork.Batches.GetBatchesAsync(request, threshold);

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
	}
}
