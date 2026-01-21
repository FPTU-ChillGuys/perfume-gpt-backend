using PerfumeGPT.Domain.Commons;

namespace PerfumeGPT.Domain.Entities
{
	public class RecipientInfo : BaseEntity<Guid>
	{
		public Guid OrderId { get; set; }
		public string? FullName { get; set; }
		public string? Phone { get; set; }

		// Calculate Shipping fee based on Address
		public int DistrictId { get; set; }
		public string WardCode { get; set; } = null!;

		// Recipient Full Address
		public string FullAddress { get; set; } = null!;

		// Navigation
		public virtual Order Order { get; set; } = null!;
	}
}
