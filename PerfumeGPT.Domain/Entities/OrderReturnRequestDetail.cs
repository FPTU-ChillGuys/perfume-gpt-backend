using PerfumeGPT.Domain.Commons;
using PerfumeGPT.Domain.Exceptions;

namespace PerfumeGPT.Domain.Entities
{
	public class OrderReturnRequestDetail : BaseEntity<Guid>
	{
		protected OrderReturnRequestDetail() { }

		public Guid ReturnRequestId { get; private set; }
		public Guid OrderDetailId { get; private set; }
		public int RequestedQuantity { get; private set; }

		// Navigation properties
		public virtual OrderReturnRequest ReturnRequest { get; set; } = null!;
		public virtual OrderDetail OrderDetail { get; set; } = null!;

		// Factory methods
		public static OrderReturnRequestDetail Create(ReturnRequestDetailPayload payload)
		{
			if (payload.OrderDetailId == Guid.Empty)
				throw DomainException.BadRequest("Order detail ID is required.");

			if (payload.RequestedQuantity <= 0)
				throw DomainException.BadRequest("Returned quantity must be greater than 0.");

			if (payload.OrderedQuantity <= 0)
				throw DomainException.BadRequest("Ordered quantity must be greater than 0.");

			if (payload.RequestedQuantity > payload.OrderedQuantity)
				throw DomainException.BadRequest("Returned quantity cannot exceed ordered quantity.");

			return new OrderReturnRequestDetail
			{
				OrderDetailId = payload.OrderDetailId,
				RequestedQuantity = payload.RequestedQuantity
			};
		}

		public record ReturnRequestDetailPayload
		{
			public required Guid OrderDetailId { get; init; }
			public required int RequestedQuantity { get; init; }
			public required int OrderedQuantity { get; init; }
		}
	}
}
