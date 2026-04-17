using PerfumeGPT.Domain.Commons;
using PerfumeGPT.Domain.Commons.Audits;
using PerfumeGPT.Domain.Enums;
using PerfumeGPT.Domain.Events.Inventories;
using PerfumeGPT.Domain.Exceptions;
using System.ComponentModel.DataAnnotations;

namespace PerfumeGPT.Domain.Entities
{
	public class Batch : BaseEntity<Guid>, IHasCreatedAt
	{
		private Batch() { }

		public Guid VariantId { get; private set; }
		public Guid ImportDetailId { get; private set; }
		public string BatchCode { get; private set; } = null!;
		public DateTime ManufactureDate { get; private set; }
		public DateTime ExpiryDate { get; private set; }
		public int ImportQuantity { get; private set; }
		public int RemainingQuantity { get; private set; }
		public int ReservedQuantity { get; private set; }
		public int AvailableInBatch => RemainingQuantity - ReservedQuantity;

		[Timestamp]
		public byte[] RowVersion { get; set; } = null!;

		// Navigation properties
		public virtual ProductVariant ProductVariant { get; set; } = null!;
		public virtual ImportDetail ImportDetail { get; set; } = null!;
		public virtual ICollection<OrderDetail> OrderDetails { get; set; } = [];
		public virtual ICollection<StockAdjustmentDetail> StockAdjustmentDetails { get; set; } = [];
		public virtual ICollection<StockReservation> StockReservations { get; set; } = [];
		public virtual ICollection<PromotionItem> Promotions { get; set; } = [];

		// IHasCreatedAt implementation
		public DateTime CreatedAt { get; set; }

		// Factory methods
		public static Batch CreateForImport(CreateForImportDto dto)
		{
			if (string.IsNullOrWhiteSpace(dto.BatchCode))
				throw DomainException.BadRequest("Mã lô là bắt buộc.");

			if (dto.Quantity <= 0)
				throw DomainException.BadRequest("Số lượng lô phải lớn hơn 0.");

			if (dto.ExpiryDate <= dto.ManufactureDate)
				throw DomainException.BadRequest("Ngày hết hạn phải sau ngày sản xuất.");

			if (dto.ExpiryDate <= DateTime.UtcNow)
				throw DomainException.BadRequest("Ngày hết hạn phải ở tương lai.");

			var batch = new Batch
			{
				Id = Guid.NewGuid(),
				VariantId = dto.VariantId,
				ImportDetailId = dto.ImportDetailId,
				BatchCode = dto.BatchCode.Trim(),
				ManufactureDate = dto.ManufactureDate,
				ExpiryDate = dto.ExpiryDate,
				ImportQuantity = dto.Quantity,
				RemainingQuantity = dto.Quantity,
				ReservedQuantity = 0
			};

			batch.AddDomainEvent(new PhysicalStockChangedDomainEvent(
				batch.VariantId,
				batch.Id,
				batch.ImportQuantity,
				batch.RemainingQuantity,
				StockTransactionType.Import,
				batch.ImportDetailId,
				$"Lô hàng nhập mới với mã lô {batch.BatchCode}",
				null
			));

			return batch;
		}

		// Business logic methods
		// 1. Giữ nguyên các hàm check Logic cũ
		public bool CanIncreaseQuantity(int quantity)
			=> quantity > 0 && RemainingQuantity + quantity <= ImportQuantity;

		public bool CanDecreaseQuantity(int quantity)
			=> quantity > 0 && RemainingQuantity >= quantity;

		// 2. Viết lại hàm Tăng kho (Ép buộc phải có Context để Ghi Log)
		public void IncreaseQuantity(int quantity, StockTransactionType type, Guid referenceId, Guid? actorId, string? reason = null)
		{
			if (!CanIncreaseQuantity(quantity))
				throw DomainException.BadRequest($"Không thể tăng số lượng vượt quá số lượng nhập {ImportQuantity}.");

			RemainingQuantity += quantity;

			//  Bắn sự kiện Ghi Log ngay tại đây!
			AddDomainEvent(new PhysicalStockChangedDomainEvent(
				this.VariantId,
				this.Id,
				quantity, // Số dương
				this.RemainingQuantity,
				type,
				referenceId,
				reason,
				actorId
			));
		}

		// 3. Viết lại hàm Giảm kho (Ép buộc phải có Context để Ghi Log)
		public void DecreaseQuantity(int quantity, StockTransactionType type, Guid referenceId, Guid? actorId, string? reason = null)
		{
			if (!CanDecreaseQuantity(quantity))
				throw DomainException.BadRequest("Không thể giảm số lượng lô xuống dưới 0.");

			RemainingQuantity -= quantity;

			//  Bắn sự kiện Ghi Log ngay tại đây!
			AddDomainEvent(new PhysicalStockChangedDomainEvent(
				this.VariantId,
				this.Id,
				-quantity, // Số âm vì là xuất kho
				this.RemainingQuantity,
				type,
				referenceId,
				reason,
				actorId
			));
		}

		public void Reserve(int quantity)
		{
			if (quantity <= 0)
				throw DomainException.BadRequest("Số lượng giữ chỗ phải lớn hơn 0.");
			if (AvailableInBatch < quantity)
				throw DomainException.BadRequest("Số lượng khả dụng không đủ để giữ chỗ.");
			ReservedQuantity += quantity;
		}

		public void Release(int quantity)
		{
			if (quantity <= 0)
				throw DomainException.BadRequest("Số lượng giải phóng phải lớn hơn 0.");
			if (ReservedQuantity < quantity)
				throw DomainException.BadRequest("Không thể giải phóng vượt quá số lượng đã giữ chỗ.");
			ReservedQuantity -= quantity;
		}

		// Records
		public sealed record CreateForImportDto
		{
			public Guid VariantId { get; init; }
			public Guid ImportDetailId { get; init; }
			public required string BatchCode { get; init; }
			public DateTime ManufactureDate { get; init; }
			public DateTime ExpiryDate { get; init; }
			public int Quantity { get; init; }
		}
	}
}
