using PerfumeGPT.Domain.Commons;
using PerfumeGPT.Domain.Exceptions;

namespace PerfumeGPT.Domain.Entities
{
	public class ScentNote : BaseEntity<int>
	{
		protected ScentNote() { }

		public string Name { get; private set; } = null!;

		// Navigation properties
		public virtual ICollection<ProductNoteMap> ProductScentNoteMaps { get; set; } = [];
		public virtual ICollection<CustomerNotePreference> CustomerScentNotePreferences { get; set; } = [];

		// Factory methods
		public static ScentNote Create(string name)
		{
			return new ScentNote
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
                throw DomainException.BadRequest("Tên nốt hương là bắt buộc.");

			return name.Trim();
		}
	}
}
