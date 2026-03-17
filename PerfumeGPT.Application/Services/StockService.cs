using PerfumeGPT.Application.DTOs.Requests.Inventory;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.Inventory;
using PerfumeGPT.Application.Interfaces.Repositories;
using PerfumeGPT.Application.Interfaces.Services;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.Services
{
	public class StockService : IStockService
	{
		private readonly IStockRepository _stockRepository;
		private readonly IBatchRepository _batchRepository;

		public StockService(IStockRepository stockRepository, IBatchRepository batchRepository)
		{
			_stockRepository = stockRepository;
			_batchRepository = batchRepository;
		}

		public async Task<bool> HasSufficientStockAsync(Guid variantId, int requiredQuantity)
		{
			return await _stockRepository.HasSufficientStockAsync(variantId, requiredQuantity);
		}

		public async Task<bool> IncreaseStockAsync(Guid variantId, int quantity)
		{
			if (quantity <= 0)
			{
				return false;
			}

			var stock = await _stockRepository.FirstOrDefaultAsync(s => s.VariantId == variantId);

			if (stock == null)
			{
				return false;
			}

			stock.TotalQuantity += quantity;

			if (stock.TotalQuantity > stock.LowStockThreshold)
			{
				stock.Status = StockStatus.Normal;
			}
			else
			{
				stock.Status = StockStatus.LowStock;
			}

			_stockRepository.Update(stock);

			return true;
		}

		public async Task<bool> DecreaseStockAsync(Guid variantId, int quantity)
		{
			if (quantity <= 0)
			{
				return false;
			}

			var stock = await _stockRepository.FirstOrDefaultAsync(s => s.VariantId == variantId);

			if (stock == null)
			{
				return false;
			}

			stock.TotalQuantity -= quantity;

			if (stock.TotalQuantity < 0)
			{
				stock.Status = StockStatus.OutOfStock;
				stock.TotalQuantity = 0;
			}
			else if (stock.TotalQuantity <= stock.LowStockThreshold)
			{
				stock.Status = StockStatus.LowStock;
			}
			else
			{
				stock.Status = StockStatus.Normal;
			}

			_stockRepository.Update(stock);

			return true;
		}

		public async Task<BaseResponse<PagedResult<StockResponse>>> GetInventoryAsync(GetPagedInventoryRequest request)
		{
			try
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
			catch (Exception ex)
			{
				return BaseResponse<PagedResult<StockResponse>>.Fail(
					$"Error retrieving inventory: {ex.Message}",
					ResponseErrorType.InternalError
				);
			}
		}

		public async Task<BaseResponse<StockResponse>> GetStockByVariantIdAsync(Guid variantId)
		{
			try
			{
				var response = await _stockRepository.GetStockWithDetailsByVariantIdAsync(variantId);

				if (response == null)
				{
					return BaseResponse<StockResponse>.Fail(
						"Stock not found for this variant",
						ResponseErrorType.NotFound
					);
				}

				return BaseResponse<StockResponse>.Ok(response);
			}
			catch (Exception ex)
			{
				return BaseResponse<StockResponse>.Fail(
					$"Error retrieving stock: {ex.Message}",
					ResponseErrorType.InternalError
				);
			}
		}

		public async Task<BaseResponse<InventorySummaryResponse>> GetInventorySummaryAsync()
		{
			try
			{
				var now = DateTime.UtcNow;
				var expiringSoonDate = now.AddDays(30);

				var (totalVariants, totalStockQuantity, lowStockVariantsCount) = await _stockRepository.GetInventorySummaryDataAsync();

				var allBatches = await _batchRepository.GetAllAsync(
					asNoTracking: true
				);

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
			catch (Exception ex)
			{
				return BaseResponse<InventorySummaryResponse>.Fail(
					$"Error retrieving inventory summary: {ex.Message}",
					ResponseErrorType.InternalError
				);
			}
		}

		public async Task<bool> InitStock(Guid variantId, int initialQuantity, int lowThreshold)
		{
			var stock = await _stockRepository.FirstOrDefaultAsync(s => s.VariantId == variantId);
			if (stock != null) return false;

			await _stockRepository.AddAsync(new Stock
			{
				VariantId = variantId,
				TotalQuantity = initialQuantity,
				LowStockThreshold = lowThreshold,
				Status = StockStatus.OutOfStock
			});

			return true;
		}
	}
}
