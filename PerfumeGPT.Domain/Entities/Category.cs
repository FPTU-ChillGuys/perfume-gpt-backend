using PerfumeGPT.Domain.Commons;
using PerfumeGPT.Domain.Exceptions;

namespace PerfumeGPT.Domain.Entities
{
	public class Category : BaseEntity<int>
	{
		private Category() { }

		public string Name { get; private set; } = null!;

		// Navigation property
		public virtual ICollection<Product> Products { get; set; } = [];

		// Factory methods
		public static Category Create(string name)
		{
			return new Category
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
				throw DomainException.BadRequest("Tên danh mục không được để trống.");
			return normalized;
		}
	}
}
