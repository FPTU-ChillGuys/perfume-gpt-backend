using PerfumeGPT.Domain.Commons;
using PerfumeGPT.Domain.Exceptions;

namespace PerfumeGPT.Domain.Entities
{
	public class OlfactoryFamily : BaseEntity<int>
	{
		protected OlfactoryFamily() { }

		public string Name { get; private set; } = null!;

		// navigation properties
		public virtual ICollection<ProductFamilyMap> ProductFamilyMaps { get; set; } = [];
		public virtual ICollection<CustomerFamilyPreference> CustomerFamilyPreferences { get; set; } = [];

		// Factory methods
		public static OlfactoryFamily Create(string name)
		{
			return new OlfactoryFamily
			{
				Name = NormalizeName(name)
			};
		}

		public void Rename(string name)
		{
			Name = NormalizeName(name);
		}

		private static string NormalizeName(string name)
		{
			if (string.IsNullOrWhiteSpace(name))
				throw DomainException.BadRequest("OlfactoryFamily name is required.");

			return name.Trim();
		}
	}
}
