using PerfumeGPT.Domain.Commons;
using PerfumeGPT.Domain.Enums;
using PerfumeGPT.Domain.Exceptions;
using System.ComponentModel.DataAnnotations;

namespace PerfumeGPT.Domain.Entities
{
	public class Stock : BaseEntity<Guid>
	{
		protected Stock() { }
		public Guid VariantId { get; private set; }
		public int TotalQuantity { get; private set; }
		public int ReservedQuantity { get; private set; }
		public int AvailableQuantity => TotalQuantity - ReservedQuantity;
		public int LowStockThreshold { get; private set; }
		public StockStatus Status { get; private set; }

		[Timestamp]
		public byte[] RowVersion { get; private set; } = null!;

		// Navigation properties 
		public virtual ProductVariant ProductVariant { get; private set; } = null!;
		public virtual ICollection<Notification> Notifications { get; private set; } = [];

		// Factory methods
		public Stock(Guid variantId, int initialQuantity, int lowStockThreshold)
		{
			if (initialQuantity < 0)
               throw DomainException.BadRequest("Số lượng ban đầu không được âm.");
			if (lowStockThreshold < 0)
              throw DomainException.BadRequest("Ngưỡng cảnh báo không được âm.");

			VariantId = variantId;
			TotalQuantity = initialQuantity;
			LowStockThreshold = lowStockThreshold;
			ReservedQuantity = 0;

			UpdateStatus();
		}

		// Business logic methods
		public void Increase(int quantity)
		{
			if (quantity <= 0)
               throw DomainException.BadRequest("Số lượng tăng phải lớn hơn 0.");

			TotalQuantity += quantity;
			UpdateStatus();
		}

		public void Decrease(int quantity)
		{
			if (quantity <= 0)
               throw DomainException.BadRequest("Số lượng giảm phải lớn hơn 0.");

			if (AvailableQuantity < quantity)
             throw DomainException.BadRequest($"Tồn kho không đủ. Khả dụng: {AvailableQuantity}, yêu cầu: {quantity}");

			TotalQuantity -= quantity;
			UpdateStatus();
		}

		public void Reserve(int quantity)
		{
			if (quantity <= 0)
                throw DomainException.BadRequest("Số lượng giữ chỗ phải lớn hơn 0.");

			if (AvailableQuantity < quantity)
               throw DomainException.Conflict("Không đủ tồn kho khả dụng để giữ chỗ.");

			ReservedQuantity += quantity;
		}

		public void ReleaseReservation(int quantity)
		{
			if (quantity <= 0 || ReservedQuantity < quantity)
              throw DomainException.BadRequest("Số lượng giải phóng không hợp lệ.");

			ReservedQuantity -= quantity;
		}

		public void SyncQuantity(int exactQuantity)
		{
			if (exactQuantity < 0)
                throw DomainException.BadRequest("Số lượng đồng bộ không được âm.");

			TotalQuantity = exactQuantity;
			UpdateStatus();
		}

		private void UpdateStatus()
		{
			if (TotalQuantity <= 0)
			{
				Status = StockStatus.OutOfStock;
				TotalQuantity = 0;
			}
			else if (TotalQuantity <= LowStockThreshold)
			{
				Status = StockStatus.LowStock;
			}
			else
			{
				Status = StockStatus.Normal;
			}
		}
	}
}
