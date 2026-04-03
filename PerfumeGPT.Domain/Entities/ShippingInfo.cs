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
				throw DomainException.BadRequest("Shipping fee cannot be negative.");

			return new ShippingInfo
			{
				Type = type,
				CarrierName = carrierName,
				ShippingFee = shippingFee,
				Status = ShippingStatus.Pending,
				EstimatedDeliveryDate = EstimatedDeliveryDate
			};
		}

		// Business logic methods
		public void MarkAsDelivered(DateTime? deliveredDateUtc = null)
		{
			if (Status != ShippingStatus.Delivering)
				throw DomainException.BadRequest("Cannot mark as delivered if not in Delivering status.");

			Status = ShippingStatus.Delivered;
			ShippedDate = deliveredDateUtc ?? DateTime.UtcNow;
		}

		public void Cancel()
		{
			if (Status == ShippingStatus.Delivered)
				throw DomainException.BadRequest("Cannot cancel a shipment that has already been delivered.");

			Status = ShippingStatus.Cancelled;
		}

		public void MarkAsDelivering()
		{
			if (Status == ShippingStatus.Delivered)
				throw DomainException.BadRequest("Cannot set shipment to delivering after it is delivered.");

			Status = ShippingStatus.Delivering;
		}

		public void MarkAsReturning()
		{
			if (Status != ShippingStatus.Delivering && Status != ShippingStatus.Pending)
				throw DomainException.BadRequest("Cannot mark as returning unless the shipment failed during delivery or pickup.");

			Status = ShippingStatus.Returning;
		}

		public void MarkAsReturned()
		{
			if (Status != ShippingStatus.Returning && Status != ShippingStatus.Delivering && Status != ShippingStatus.Pending)
				throw DomainException.BadRequest("Cannot mark as returned from the current status.");

			Status = ShippingStatus.Returned;
		}

		public void SetTrackingNumber(string trackingNumber)
		{
			if (string.IsNullOrWhiteSpace(trackingNumber))
				throw DomainException.BadRequest("Tracking number is required.");

			TrackingNumber = trackingNumber.Trim();
		}
	}
}
