using PerfumeGPT.Domain.Commons;
using PerfumeGPT.Domain.Commons.Audits;
using Microsoft.Data.SqlTypes;


namespace PerfumeGPT.Domain.Entities
{
	public class Product : BaseEntity<Guid>, IHasTimestamps, ISoftDelete
	{
		public string Name { get; set; } = null!;
		public int BrandId { get; set; }
		public int CategoryId { get; set; }
		public string? Description { get; set; }

		// Navigation
		public virtual Brand Brand { get; set; } = null!;
		public virtual Category Category { get; set; } = null!;
		public virtual ICollection<ProductVariant> Variants { get; set; } = [];
		public virtual ICollection<Media> Media { get; set; } = [];
		public virtual ICollection<ProductAttribute> ProductAttributes { get; set; } = [];

		// ISoftDelete implementation
		public bool IsDeleted { get; set; }
		public DateTime? DeletedAt { get; set; }

		// IHasTimestamps implementation
		public DateTime? UpdatedAt { get; set; }
		public DateTime CreatedAt { get; set; }

		//Embedding vector for search functionality
		public SqlVector<float>? Embedding { get; set; }
	}
}
