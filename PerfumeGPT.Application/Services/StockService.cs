using PerfumeGPT.Application.DTOs.Requests.Inventory;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.Inventory;
using PerfumeGPT.Application.Exceptions;
using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Application.Interfaces.Services;
using PerfumeGPT.Application.Services.Helpers;
using PerfumeGPT.Domain.Entities;

namespace PerfumeGPT.Application.Services
{
	public class StockService : IStockService
	{
		#region Dependencies
		private readonly IUnitOfWork _unitOfWork;

		public StockService(IUnitOfWork unitOfWork)
		{
			_unitOfWork = unitOfWork;
		}
		#endregion Dependencies



		public async Task<BaseResponse<string>> UpdateStockAsync(Guid stockId, UpdateStockRequest request)
		{
			var stock = await _unitOfWork.Stocks.GetByIdAsync(stockId)
			 ?? throw AppException.NotFound($"Không tìm thấy tồn kho với ID {stockId}");
			if (stock.LowStockThreshold == request.LowStockThreshold)
			{
				return BaseResponse<string>.Ok("Ngưỡng tồn kho thấp đã được cập nhật.");
			}
			stock.UpdateLowStockThreshold(request.LowStockThreshold);
			_unitOfWork.Stocks.Update(stock);
			await _unitOfWork.SaveChangesAsync();
			return BaseResponse<string>.Ok("Cập nhật tồn kho thành công.");
		}

		public async Task<bool> HasSufficientStockAsync(Guid variantId, int requiredQuantity, int? minBufferDays = null, List<Guid>? exemptedBatchIds = null)
		{
			return await _unitOfWork.Stocks.HasSufficientStockAsync(variantId, requiredQuantity, minBufferDays, exemptedBatchIds);
		}

		public async Task<BaseResponse<PagedResult<StockResponse>>> GetInventoryAsync(GetPagedInventoryRequest request)
		{
			var sellable = await SellableStockContextLoader.LoadAsync(_unitOfWork);
			var (stockResponses, totalCount) = await _unitOfWork.Stocks.GetPagedInventoryAsync(request, sellable);

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
			var sellable = await SellableStockContextLoader.LoadAsync(_unitOfWork, [variantId]);
			var response = await _unitOfWork.Stocks.GetStockWithDetailsByVariantIdAsync(variantId, sellable)
			 ?? throw AppException.NotFound($"Không tìm thấy tồn kho cho biến thể {variantId}");

			return BaseResponse<StockResponse>.Ok(response);
		}

		public async Task<BaseResponse<InventorySummaryResponse>> GetInventorySummaryAsync()
		{
			var now = DateTime.UtcNow;
			var expiringSoonDate = now.AddDays(30);

			var sellable = await SellableStockContextLoader.LoadAsync(_unitOfWork);
			var (totalVariants, totalStockQuantity, lowStockVariantsCount) = await _unitOfWork.Stocks.GetInventorySummaryDataAsync(sellable);

			var totalBatches = await _unitOfWork.Batches.CountAsync();
			var expiredBatchesCount = await _unitOfWork.Batches.CountAsync(b => b.ExpiryDate < now);
			var expiringSoonCount = await _unitOfWork.Batches.CountAsync(b => b.ExpiryDate >= now && b.ExpiryDate <= expiringSoonDate);

			var summary = new InventorySummaryResponse
			{
				TotalVariants = totalVariants,
				TotalStockQuantity = totalStockQuantity,
				LowStockVariantsCount = lowStockVariantsCount,
				TotalBatches = totalBatches,
				ExpiredBatchesCount = expiredBatchesCount,
				ExpiringSoonCount = expiringSoonCount
			};

			return BaseResponse<InventorySummaryResponse>.Ok(summary);
		}

		public async Task InitStockAsync(Guid variantId, int initialQuantity, int lowThreshold)
		{
			var exists = await _unitOfWork.Stocks.FirstOrDefaultAsync(s => s.VariantId == variantId);
			if (exists != null)
			{
				throw AppException.Conflict($"Tồn kho cho biến thể {variantId} đã tồn tại.");
			}

			var newStock = new Stock(variantId, initialQuantity, lowThreshold);
			await _unitOfWork.Stocks.AddAsync(newStock);
		}
	}
}
