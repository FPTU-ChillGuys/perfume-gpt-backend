using PerfumeGPT.Domain.Commons;

namespace PerfumeGPT.Domain.Entities
{
	public class ScentNote : BaseEntity<int>
	{
		public string Name { get; set; } = null!;

		// Navigation properties
		public virtual ICollection<ProductNoteMap> ProductScentNoteMaps { get; set; } = [];
		public virtual ICollection<CustomerNotePreference> CustomerScentNotePreferences { get; set; } = [];
	}
}
