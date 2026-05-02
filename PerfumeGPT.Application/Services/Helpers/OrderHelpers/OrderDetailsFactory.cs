using PerfumeGPT.Application.DTOs.Responses.CartItems;
using PerfumeGPT.Application.Exceptions;
using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Application.Interfaces.Services.OrderHelpers;
using PerfumeGPT.Domain.Entities;
using System.Text.Json;

namespace PerfumeGPT.Application.Services.Helpers.OrderHelpers
{
	public class OrderDetailsFactory : IOrderDetailsFactory
	{
		private readonly IUnitOfWork _unitOfWork;

		public OrderDetailsFactory(IUnitOfWork unitOfWork) { _unitOfWork = unitOfWork; }

		public async Task CreateOrderDetailsAsync(Order order, List<CartCheckoutItemDto> items)
		{
			if (order == null) throw AppException.BadRequest("Đơn hàng là bắt buộc.");
			if (items == null || items.Count == 0) throw AppException.BadRequest("Danh sách sản phẩm của đơn hàng là bắt buộc.");

			// Chặn lỗi nhanh: Xác thực tổng tồn kho trước khi đi tiếp
			var stockItems = items.GroupBy(i => i.VariantId).Select(g => (VariantId: g.Key, Quantity: g.Sum(x => x.Quantity))).ToList();
			var stockValidation = await ValidateStockAvailabilityAsync(stockItems);
			if (!stockValidation) throw AppException.BadRequest("Xác thực tồn kho thất bại.");

			var variantIds = items.Select(i => i.VariantId).Distinct().ToList();
			var variants = await _unitOfWork.Variants.GetVariantsWithDetailsByIdsAsync(variantIds);
			var variantDictionary = variants.ToDictionary(v => v.Id);

			var orderDetailsToAdd = new List<OrderDetail>();

			foreach (var item in items)
			{
				if (!variantDictionary.TryGetValue(item.VariantId, out var variant))
					throw AppException.NotFound($"Biến thể sản phẩm {item.VariantId} không tồn tại.");

				var totalVoucherDiscount = item.ApportionedVoucherDiscount;
				var totalPromoDiscount = item.Discount - totalVoucherDiscount;

				// Lập Snapshot không chứa Batch (Vì 1 dòng có thể xé thành nhiều Batch)
				string snapshotJson = CreateSnapshotJson(variant, item.Quantity, item.SubTotal, totalPromoDiscount);

				var detail = OrderDetail.Create(item.VariantId, item.AppliedPromotionItemId, item.Quantity, variant.BasePrice, snapshotJson, totalVoucherDiscount, totalPromoDiscount);

				// 💡 Lưu tạm BatchId (Nếu có) vào biến Tàng hình để lát nữa StockReservationService bốc ra dùng
				detail.TransientBatchId = item.BatchId;

				orderDetailsToAdd.Add(detail);
			}

			order.AddOrderDetails(orderDetailsToAdd);
		}

		private static string CreateSnapshotJson(ProductVariant variant, int quantity, decimal lineTotal, decimal promoDiscount)
		{
			var snapshotData = new
			{
				variant.Sku,
				variant.Barcode,
				ProductName = variant.Product?.Name ?? "Unknown Product",
				variant.VolumeMl,
				VariantType = variant.Type.ToString(),
				Concentration = variant.Concentration?.Name,
				FinalUnitPrice = quantity > 0 ? (lineTotal - promoDiscount) / quantity : 0m
			};
			return JsonSerializer.Serialize(snapshotData);
		}

		private async Task<bool> ValidateStockAvailabilityAsync(List<(Guid VariantId, int Quantity)> items)
		{
			if (items == null || items.Count == 0) return true;

			// BƯỚC 2: Gom nhóm các Item trùng VariantId (Đề phòng giỏ hàng gửi lên 2 dòng có cùng Variant)
			var groupedItems = items
				.GroupBy(i => i.VariantId)
				.Select(g => new { VariantId = g.Key, RequiredQty = g.Sum(x => x.Quantity) })
				.ToList();

			var variantIds = groupedItems.Select(x => x.VariantId).Distinct().ToList();

			// BƯỚC 3: BULK READ - Kéo tất cả dữ liệu liên quan lên RAM chỉ bằng 3 câu Query (Có AsNoTracking để tối ưu RAM)

			// 3.1. Kéo Stock
			var stocks = await _unitOfWork.Stocks.GetAllAsync(
				s => variantIds.Contains(s.VariantId),
				asNoTracking: true);
			var stockDict = stocks.ToDictionary(s => s.VariantId);

			// 3.2. Kéo Batch (Chỉ lấy các lô chưa hết hạn)
			var now = DateTime.UtcNow;
			var batches = await _unitOfWork.Batches.GetAllAsync(
				b => variantIds.Contains(b.VariantId) && b.ExpiryDate > now,
				asNoTracking: true);

			// Tính tổng số lượng khả dụng của tất cả các Lô gộp lại cho từng Variant
			var batchDict = batches
				.GroupBy(b => b.VariantId)
				.ToDictionary(
					g => g.Key,
					g => g.Sum(b => Math.Max(0, b.RemainingQuantity - b.ReservedQuantity)));

			// 3.3. Kéo Variant (Chỉ để lấy Sku phục vụ cho việc in ra câu báo lỗi)
			var variants = await _unitOfWork.Variants.GetAllAsync(
				v => variantIds.Contains(v.Id),
				asNoTracking: true);
			var variantDict = variants.ToDictionary(v => v.Id);

			// BƯỚC 4: THỰC HIỆN ĐỐI CHIẾU TRÊN RAM (IN-MEMORY VALIDATION)
			foreach (var item in groupedItems)
			{
				var variantName = variantDict.TryGetValue(item.VariantId, out var v) ? $"Variant {v.Sku}" : "Unknown product";

				// 4.1. Đối chiếu tổng tồn kho (Stock)
				if (!stockDict.TryGetValue(item.VariantId, out var stock) || stock.AvailableQuantity < item.RequiredQty)
				{
					throw AppException.BadRequest($"Không đủ hàng tồn kho cho {variantName}.");
				}

				// 4.2. Đối chiếu Tồn kho theo lô (Batch)
				if (!batchDict.TryGetValue(item.VariantId, out var totalBatchAvailable) || totalBatchAvailable < item.RequiredQty)
				{
					throw AppException.BadRequest($"Không đủ hàng tồn kho trong lô cho {variantName}.");
				}
			}

			return true;
		}
	}
}
