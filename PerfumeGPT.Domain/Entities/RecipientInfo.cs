using PerfumeGPT.Domain.Commons;

namespace PerfumeGPT.Domain.Entities
{
	public class RecipientInfo : BaseEntity<Guid>
	{
		public Guid OrderId { get; set; }
		public string RecipientName { get; set; } = null!;
		public string RecipientPhoneNumber { get; set; } = null!;

		// Calculate Shipping fee based on Address
		public int DistrictId { get; set; }
		public string DistrictName { get; set; } = null!;
		public string WardCode { get; set; } = null!;
		public string WardName { get; set; } = null!;
		public string ProvinceName { get; set; } = null!;

		// Recipient Full Address
		public string FullAddress { get; set; } = null!;

		// Navigation
		public virtual Order Order { get; set; } = null!;
	}
}
