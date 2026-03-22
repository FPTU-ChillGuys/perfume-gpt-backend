using PerfumeGPT.Domain.Commons;
using PerfumeGPT.Domain.Enums;
using PerfumeGPT.Domain.Exceptions;

namespace PerfumeGPT.Domain.Entities
{
	public class Brand : BaseEntity<int>
	{
		public string Name { get; set; } = null!;

		// Navigation
		public virtual ICollection<Product> Products { get; set; } = [];

		public static string NormalizeName(string name)
		{
			var normalizedName = name?.Trim() ?? string.Empty;
			if (string.IsNullOrWhiteSpace(normalizedName))
			{
				throw new DomainException("Brand name is required.", DomainErrorType.BadRequest);
			}

			return normalizedName;
		}

		public void Rename(string name)
		{
			Name = NormalizeName(name);
		}

		public void EnsureCanBeDeleted()
		{
			if (Products.Count != 0)
			{
				throw new DomainException("Cannot delete brand with associated products", DomainErrorType.BadRequest);
			}
		}
	}
}
