using PerfumeGPT.Domain.Commons;
using PerfumeGPT.Domain.Commons.Audits;
using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Domain.Entities
{
	public class ProductVariant : BaseEntity<Guid>, IHasTimestamps, ISoftDelete
	{
		public Guid ProductId { get; set; }
		public string Barcode { get; set; } = null!;
		public string Sku { get; set; } = null!;
		public int VolumeMl { get; set; }
		public int ConcentrationId { get; set; }
		public VariantType Type { get; set; }
		public decimal BasePrice { get; set; }
		public VariantStatus Status { get; set; }

		// Navigation
		public virtual Product Product { get; set; } = null!;
		public virtual Concentration Concentration { get; set; } = null!;
		public virtual ICollection<ImportDetail> ImportDetails { get; set; } = [];
		public virtual ICollection<StockAdjustmentDetail> StockAdjustmentDetails { get; set; } = [];
		public virtual ICollection<Batch> Batches { get; set; } = [];
		public virtual ICollection<StockReservation> StockReservations { get; set; } = [];
		public virtual Stock Stock { get; set; } = null!;
		public virtual ICollection<CartItem> CartItems { get; set; } = [];
		public virtual ICollection<OrderDetail> OrderDetails { get; set; } = [];
		public virtual ICollection<Media> Media { get; set; } = [];
		public virtual ICollection<Review> Reviews { get; set; } = [];

		// ISoftDelete implementation
		public bool IsDeleted { get; set; }
		public DateTime? DeletedAt { get; set; }

		// IHasTimestamps implementation
		public DateTime? UpdatedAt { get; set; }
		public DateTime CreatedAt { get; set; }
	}
}
