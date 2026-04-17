using PerfumeGPT.Domain.Commons;
using PerfumeGPT.Domain.Enums;
using PerfumeGPT.Domain.Exceptions;

namespace PerfumeGPT.Domain.Entities
{
	public class ShippingInfo : BaseEntity<Guid>
	{
		protected ShippingInfo() { }

		public CarrierName CarrierName { get; private set; }
		public string? TrackingNumber { get; private set; }
		public ShippingType Type { get; private set; }
		public decimal ShippingFee { get; private set; }
		public ShippingStatus Status { get; private set; }
		public DateTime? EstimatedDeliveryDate { get; private set; }
		public DateTime? ShippedDate { get; private set; }

		// Factory method
		public static ShippingInfo Create(CarrierName carrierName, ShippingType type, decimal shippingFee = 0, DateTime? EstimatedDeliveryDate = null)
		{
			if (shippingFee < 0)
               throw DomainException.BadRequest("Phí vận chuyển không được âm.");

			return new ShippingInfo
			{
				Type = type,
				CarrierName = carrierName,
				ShippingFee = shippingFee,
				Status = ShippingStatus.UnAssigned,
				EstimatedDeliveryDate = EstimatedDeliveryDate
			};
		}

		// Business logic methods
		public void MarkAsDelivered(DateTime? deliveredDateUtc = null)
		{
			if (Status != ShippingStatus.Delivering)
              throw DomainException.BadRequest("Không thể đánh dấu đã giao khi không ở trạng thái đang giao.");

			Status = ShippingStatus.Delivered;
			ShippedDate = deliveredDateUtc ?? DateTime.UtcNow;
		}

		public void Cancel()
		{
			if (Status == ShippingStatus.Delivered)
              throw DomainException.BadRequest("Không thể hủy chuyến giao đã giao thành công.");

			Status = ShippingStatus.Cancelled;
		}

		public void MarkAsDelivering()
		{
			if (Status == ShippingStatus.Delivered)
               throw DomainException.BadRequest("Không thể chuyển sang trạng thái đang giao sau khi đã giao thành công.");

			Status = ShippingStatus.Delivering;
		}

		public void MarkAsReturning()
		{
            if (Status != ShippingStatus.Delivering && Status != ShippingStatus.ReadyToPick && Status != ShippingStatus.Delivered)
             throw DomainException.BadRequest("Chỉ có thể đánh dấu đang hoàn trả khi giao hàng hoặc lấy hàng thất bại.");

			Status = ShippingStatus.Returning;
		}

		public void MarkAsReturned()
		{
			if (Status != ShippingStatus.Returning && Status != ShippingStatus.Delivering && Status != ShippingStatus.ReadyToPick)
               throw DomainException.BadRequest("Không thể đánh dấu đã hoàn trả từ trạng thái hiện tại.");

			Status = ShippingStatus.Returned;
		}

		public void SetTrackingNumber(string trackingNumber)
		{
			if (string.IsNullOrWhiteSpace(trackingNumber))
               throw DomainException.BadRequest("Mã vận đơn là bắt buộc.");

			TrackingNumber = trackingNumber.Trim();
			Status = ShippingStatus.ReadyToPick;
		}
	}
}
