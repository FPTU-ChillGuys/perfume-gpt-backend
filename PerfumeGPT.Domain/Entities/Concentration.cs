using PerfumeGPT.Domain.Commons;
using PerfumeGPT.Domain.Exceptions;

namespace PerfumeGPT.Domain.Entities
{
	public class Concentration : BaseEntity<int>
	{
		private Concentration() { }

		public string Name { get; private set; } = null!;

		// Navigation
		public virtual ICollection<ProductVariant> Variants { get; set; } = [];

		// Factory methods
		public static Concentration Create(string name)
		{
			return new Concentration
			{
				Name = NormalizeName(name)
			};
		}

		public void Rename(string name)
		{
			Name = NormalizeName(name);
		}

		// Business logic methods
		public static string NormalizeName(string name)
		{
			var normalized = name?.Trim() ?? string.Empty;
			if (string.IsNullOrWhiteSpace(normalized))
				throw DomainException.BadRequest("Concentration name is required.");
			return normalized;
		}
	}
}
