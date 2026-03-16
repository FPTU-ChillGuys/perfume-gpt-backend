using PerfumeGPT.Domain.Commons;

namespace PerfumeGPT.Domain.Entities
{
	public class OlfactoryFamily : BaseEntity<int>
	{
		public string Name { get; set; } = null!;

		// navigation properties
		public virtual ICollection<ProductFamilyMap> ProductFamilyMaps { get; set; } = [];
		public virtual ICollection<CustomerFamilyPreference> CustomerFamilyPreferences { get; set; } = [];
	}
}
