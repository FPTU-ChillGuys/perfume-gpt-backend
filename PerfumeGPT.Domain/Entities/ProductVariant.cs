using PerfumeGPT.Domain.Commons;
using PerfumeGPT.Domain.Commons.Audits;

namespace PerfumeGPT.Domain.Entities
{
	public class ProductVariant : BaseEntity<Guid>, IHasTimestamps, ISoftDelete
	{
		public Guid ProductId { get; set; }
		public string? ImageUrl { get; set; }
		public string Sku { get; set; } = null!;
		public int VolumeMl { get; set; } // (30ml / 50ml / 100ml / etc.)
		public int ConcentrationId { get; set; } // (Eau de Parfum / Eau de Toilette / etc.)
		public string? Type { get; set; } // (fullbox / tester / mini)
		public decimal BasePrice { get; set; }
		public string? Status { get; set; } // (available / out_of_stock / discontinued)

		// Navigation
		public virtual Product Product { get; set; } = null!;
		public virtual Concentration Concentration { get; set; } = null!;
		public virtual ICollection<ImportDetail> ImportDetails { get; set; } = [];
		public virtual ICollection<Batch> Batches { get; set; } = [];
		public virtual Stock Stock { get; set; } = null!;
		public virtual ICollection<CartItem> CartItems { get; set; } = [];
		public virtual ICollection<OrderDetail> OrderDetails { get; set; } = [];

		// ISoftDelete implementation
		public bool IsDeleted { get; set; }
		public DateTime? DeletedAt { get; set; }

		// IHasTimestamps implementation
		public DateTime? UpdatedAt { get; set; }
		public DateTime CreatedAt { get; set; }
	}
}
