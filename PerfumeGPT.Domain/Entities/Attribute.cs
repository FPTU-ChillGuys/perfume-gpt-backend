using PerfumeGPT.Domain.Commons;
using PerfumeGPT.Domain.Commons.Helpers;
using PerfumeGPT.Domain.Exceptions;

namespace PerfumeGPT.Domain.Entities
{
	public class Attribute : BaseEntity<int>
	{
		private Attribute() { }

		public string InternalCode { get; private set; } = null!;
		public string Name { get; private set; } = null!;
		public string? Description { get; private set; }
		public bool IsVariantLevel { get; private set; }

		public virtual ICollection<ProductAttribute> ProductAttributes { get; set; } = [];
		public virtual ICollection<AttributeValue> AttributeValues { get; set; } = [];

		public static Attribute Create(string? internalCode, string name, string? description, bool isVariantLevel)
		{
			if (string.IsNullOrWhiteSpace(name))
				throw DomainException.BadRequest("Attribute name is required.");

			string finalCode = string.IsNullOrWhiteSpace(internalCode)
			? GenerateInternalCode(name)
			: internalCode.Trim().ToUpperInvariant();

			return new Attribute
			{
				InternalCode = finalCode,
				Name = name.Trim(),
				Description = description?.Trim(),
				IsVariantLevel = isVariantLevel
			};
		}

		public void Update(string? name, string? description, bool? isVariantLevel)
		{
			if (name != null)
			{
				if (string.IsNullOrWhiteSpace(name))
					throw DomainException.BadRequest("Attribute name cannot be empty.");
				Name = name.Trim();
			}

			if (description != null) Description = description.Trim();
			if (isVariantLevel.HasValue) IsVariantLevel = isVariantLevel.Value;
		}

		public static void EnsureCanBeDeleted(bool isInUse)
		{
			if (isInUse)
				throw DomainException.BadRequest("Attribute is in use and cannot be deleted.");
		}

		private static string GenerateInternalCode(string name) => name.ToUrlsFriendly().ToUpperInvariant();
	}
}
