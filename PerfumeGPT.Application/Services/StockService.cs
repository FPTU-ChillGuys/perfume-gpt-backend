using PerfumeGPT.Application.DTOs.Requests.Inventory;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.Inventory;
using PerfumeGPT.Application.Exceptions;
using PerfumeGPT.Application.Interfaces.Repositories;
using PerfumeGPT.Application.Interfaces.Services;
using PerfumeGPT.Domain.Entities;

namespace PerfumeGPT.Application.Services
{
	public class StockService : IStockService
	{
		#region Dependencies
		private readonly IStockRepository _stockRepository;
		private readonly IBatchRepository _batchRepository;

		public StockService(IStockRepository stockRepository, IBatchRepository batchRepository)
		{
			_stockRepository = stockRepository;
			_batchRepository = batchRepository;
		}
		#endregion Dependencies

		public async Task<bool> HasSufficientStockAsync(Guid variantId, int requiredQuantity)
			=> await _stockRepository.HasSufficientStockAsync(variantId, requiredQuantity);

		public async Task<BaseResponse<PagedResult<StockResponse>>> GetInventoryAsync(GetPagedInventoryRequest request)
		{
			var (stockResponses, totalCount) = await _stockRepository.GetPagedInventoryAsync(request);

			var pagedResult = new PagedResult<StockResponse>(
				stockResponses,
				request.PageNumber,
				request.PageSize,
				totalCount
			);

			return BaseResponse<PagedResult<StockResponse>>.Ok(pagedResult);
		}

		public async Task<BaseResponse<StockResponse>> GetStockByVariantIdAsync(Guid variantId)
		{
			var response = await _stockRepository.GetStockWithDetailsByVariantIdAsync(variantId)
				?? throw AppException.NotFound($"Stock not found for variant {variantId}");

			return BaseResponse<StockResponse>.Ok(response);
		}

		public async Task<BaseResponse<InventorySummaryResponse>> GetInventorySummaryAsync()
		{
			var now = DateTime.UtcNow;
			var expiringSoonDate = now.AddDays(30);

			var (totalVariants, totalStockQuantity, lowStockVariantsCount) = await _stockRepository.GetInventorySummaryDataAsync();

			var allBatches = await _batchRepository.GetAllAsync(asNoTracking: true);

			var summary = new InventorySummaryResponse
			{
				TotalVariants = totalVariants,
				TotalStockQuantity = totalStockQuantity,
				LowStockVariantsCount = lowStockVariantsCount,
				TotalBatches = allBatches.Count(),
				ExpiredBatchesCount = allBatches.Count(b => b.ExpiryDate < now),
				ExpiringSoonCount = allBatches.Count(b => b.ExpiryDate >= now && b.ExpiryDate <= expiringSoonDate)
			};

			return BaseResponse<InventorySummaryResponse>.Ok(summary);
		}

		public async Task InitStockAsync(Guid variantId, int initialQuantity, int lowThreshold)
		{
			var exists = await _stockRepository.FirstOrDefaultAsync(s => s.VariantId == variantId);
			if (exists != null)
			{
				throw AppException.Conflict($"Stock for variant {variantId} already exists.");
			}

			var newStock = new Stock(variantId, initialQuantity, lowThreshold);
			await _stockRepository.AddAsync(newStock);
		}
	}
}
