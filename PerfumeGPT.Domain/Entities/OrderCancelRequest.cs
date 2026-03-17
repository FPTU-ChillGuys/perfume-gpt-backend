using PerfumeGPT.Domain.Commons;
using PerfumeGPT.Domain.Commons.Audits;
using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Domain.Entities
{
	public class OrderCancelRequest : BaseEntity<Guid>, IHasTimestamps
	{
		public Guid OrderId { get; set; }
		public Guid RequestedById { get; set; }
		public Guid? ProcessedById { get; set; }

		public string Reason { get; set; } = null!;
		public string? StaffNote { get; set; }
		public CancelRequestStatus Status { get; set; }


		public bool IsRefundRequired { get; set; }
		public decimal? RefundAmount { get; set; }
		public bool IsRefunded { get; set; }
		public string? VnpTransactionNo { get; set; }

		// Navigation properties
		public virtual Order Order { get; set; } = null!;
		public virtual User RequestedBy { get; set; } = null!;
		public virtual User? ProcessedBy { get; set; }

		// IHasTimestamps implementation
		public DateTime CreatedAt { get; set; }
		public DateTime? UpdatedAt { get; set; }
	}
}
