using PerfumeGPT.Domain.Commons;
using PerfumeGPT.Domain.Exceptions;

namespace PerfumeGPT.Domain.Entities
{
	public class OrderReturnRequestDetail : BaseEntity<Guid>
	{
		protected OrderReturnRequestDetail() { }

		public Guid ReturnRequestId { get; private set; }
		public Guid OrderDetailId { get; private set; }
		public int ReturnedQuantity { get; private set; }

		public bool? IsRestocked { get; private set; }
		public string? InspectionNote { get; private set; }

		// Navigation properties
		public virtual OrderReturnRequest ReturnRequest { get; private set; } = null!;
		public virtual OrderDetail OrderDetail { get; private set; } = null!;

		// Factory method
		internal static OrderReturnRequestDetail Create(Guid orderDetailId, int returnedQuantity)
		{
			if (orderDetailId == Guid.Empty)
				throw DomainException.BadRequest("Order detail ID is required.");

			if (returnedQuantity <= 0)
				throw DomainException.BadRequest("Returned quantity must be greater than 0.");

			return new OrderReturnRequestDetail
			{
				OrderDetailId = orderDetailId,
				ReturnedQuantity = returnedQuantity,
				IsRestocked = null // Chưa kiểm tra
			};
		}

		// Business logic method
		internal void RecordInspection(bool isRestocked, string? inspectionNote)
		{
			IsRestocked = isRestocked;
			InspectionNote = string.IsNullOrWhiteSpace(inspectionNote) ? null : inspectionNote.Trim();
		}
	}
}
